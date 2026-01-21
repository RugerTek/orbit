using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Services;

#region DTOs

public class DashboardInsightsResponse
{
    public required DashboardStats Stats { get; set; }
    public required List<FocusItem> FocusItems { get; set; }
    public required List<NextAction> NextActions { get; set; }
    public string? Error { get; set; }
}

public class DashboardStats
{
    public int PeopleCount { get; set; }
    public string PeopleChange { get; set; } = "";
    public int RolesCount { get; set; }
    public string RolesChange { get; set; } = "";
    public int FunctionsCount { get; set; }
    public string FunctionsChange { get; set; } = "";
    public int ProcessesCount { get; set; }
    public string ProcessesChange { get; set; } = "";
}

public class FocusItem
{
    public required string Title { get; set; }
    public required string Status { get; set; }
    public required string Summary { get; set; }
    public required string Href { get; set; }
}

public class NextAction
{
    public required string Action { get; set; }
    public string? Href { get; set; }
}

#endregion

public interface IDashboardInsightsService
{
    Task<DashboardInsightsResponse> GetInsightsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

public class DashboardInsightsService : IDashboardInsightsService
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<DashboardInsightsService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private const string ANTHROPIC_API_URL = "https://api.anthropic.com/v1/messages";

    public DashboardInsightsService(
        OrbitOSDbContext dbContext,
        ILogger<DashboardInsightsService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Anthropic");
        _apiKey = configuration["ANTHROPIC_API_KEY"]
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }

    public async Task<DashboardInsightsResponse> GetInsightsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await GatherStatsAsync(organizationId, cancellationToken);
            var analysisData = await GatherAnalysisDataAsync(organizationId, cancellationToken);
            var focusItems = GenerateFocusItems(analysisData);
            var nextActions = GenerateNextActions(analysisData);

            // Try AI enhancement if available
            if (!string.IsNullOrEmpty(_apiKey))
            {
                try
                {
                    var aiInsights = await GetAiEnhancedInsightsAsync(analysisData, cancellationToken);
                    if (aiInsights != null)
                    {
                        focusItems = aiInsights.FocusItems;
                        nextActions = aiInsights.NextActions;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI enhancement failed, using rule-based insights");
                }
            }

            return new DashboardInsightsResponse { Stats = stats, FocusItems = focusItems, NextActions = nextActions };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard insights");
            return new DashboardInsightsResponse
            {
                Stats = new DashboardStats(),
                FocusItems = new List<FocusItem>(),
                NextActions = new List<NextAction>(),
                Error = "Failed to generate insights"
            };
        }
    }

    private async Task<DashboardStats> GatherStatsAsync(Guid organizationId, CancellationToken ct)
    {
        var peopleCount = await _dbContext.Resources.Include(r => r.ResourceSubtype)
            .CountAsync(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person, ct);
        var rolesCount = await _dbContext.Roles.CountAsync(r => r.OrganizationId == organizationId, ct);
        var functionsCount = await _dbContext.Functions.CountAsync(f => f.OrganizationId == organizationId, ct);
        var processesCount = await _dbContext.Processes.CountAsync(p => p.OrganizationId == organizationId, ct);

        var emptyRolesCount = await _dbContext.Roles.Where(r => r.OrganizationId == organizationId)
            .Where(r => !_dbContext.RoleAssignments.Any(ra => ra.RoleId == r.Id)).CountAsync(ct);
        var spofRolesCount = await _dbContext.Roles.Where(r => r.OrganizationId == organizationId)
            .Where(r => _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id) == 1).CountAsync(ct);
        var uncoveredFunctionsCount = await _dbContext.Functions.Where(f => f.OrganizationId == organizationId)
            .Where(f => !f.FunctionCapabilities.Any()).CountAsync(ct);
        var processesNeedingReview = await _dbContext.Processes.Where(p => p.OrganizationId == organizationId)
            .Where(p => p.OwnerId == null || !p.Activities.Any()).CountAsync(ct);

        return new DashboardStats
        {
            PeopleCount = peopleCount,
            PeopleChange = peopleCount > 0 ? "Active" : "Add people to get started",
            RolesCount = rolesCount,
            RolesChange = emptyRolesCount > 0 ? $"{emptyRolesCount} unassigned" : (spofRolesCount > 0 ? $"{spofRolesCount} SPOF" : "All covered"),
            FunctionsCount = functionsCount,
            FunctionsChange = uncoveredFunctionsCount > 0 ? $"{uncoveredFunctionsCount} uncovered" : "All mapped",
            ProcessesCount = processesCount,
            ProcessesChange = processesNeedingReview > 0 ? $"{processesNeedingReview} need review" : "All documented"
        };
    }

    private async Task<AnalysisData> GatherAnalysisDataAsync(Guid organizationId, CancellationToken ct)
    {
        var data = new AnalysisData();
        data.EmptyRoles = await _dbContext.Roles.Where(r => r.OrganizationId == organizationId)
            .Where(r => !_dbContext.RoleAssignments.Any(ra => ra.RoleId == r.Id))
            .Select(r => new RoleInfo { Id = r.Id, Name = r.Name, Department = r.Department }).ToListAsync(ct);

        data.SpofRoles = await _dbContext.Roles.Where(r => r.OrganizationId == organizationId)
            .Where(r => _dbContext.RoleAssignments.Count(ra => ra.RoleId == r.Id) == 1)
            .Select(r => new RoleInfo { Id = r.Id, Name = r.Name, Department = r.Department,
                AssigneeName = _dbContext.RoleAssignments.Where(ra => ra.RoleId == r.Id).Select(ra => ra.Resource.Name).FirstOrDefault()
            }).ToListAsync(ct);

        data.UncoveredFunctions = await _dbContext.Functions.Where(f => f.OrganizationId == organizationId)
            .Where(f => !f.FunctionCapabilities.Any())
            .Select(f => new FunctionInfo { Id = f.Id, Name = f.Name, Category = f.Category }).ToListAsync(ct);

        data.ProcessesNeedingAttention = await _dbContext.Processes.Where(p => p.OrganizationId == organizationId)
            .Where(p => p.OwnerId == null || !p.Activities.Any())
            .Select(p => new ProcessInfo { Id = p.Id, Name = p.Name, HasOwner = p.OwnerId != null, ActivityCount = p.Activities.Count }).ToListAsync(ct);

        data.TotalPeople = await _dbContext.Resources.Include(r => r.ResourceSubtype)
            .CountAsync(r => r.OrganizationId == organizationId && r.ResourceSubtype.ResourceType == ResourceType.Person, ct);
        data.TotalRoles = await _dbContext.Roles.CountAsync(r => r.OrganizationId == organizationId, ct);
        data.TotalFunctions = await _dbContext.Functions.CountAsync(f => f.OrganizationId == organizationId, ct);
        data.TotalProcesses = await _dbContext.Processes.CountAsync(p => p.OrganizationId == organizationId, ct);
        return data;
    }

    private List<FocusItem> GenerateFocusItems(AnalysisData data)
    {
        var items = new List<FocusItem>();
        if (data.SpofRoles.Any())
        {
            var top = data.SpofRoles.First();
            items.Add(new FocusItem { Title = $"{top.Name} Coverage", Status = "Bottleneck",
                Summary = $"Only {top.AssigneeName} covers this role. Add backup to reduce risk.", Href = "/app/roles" });
        }
        if (data.EmptyRoles.Any())
            items.Add(new FocusItem { Title = "Role Assignments", Status = "Review",
                Summary = $"{data.EmptyRoles.Count} role(s) have no one assigned.", Href = "/app/roles" });
        if (data.UncoveredFunctions.Any())
            items.Add(new FocusItem { Title = "Function Coverage", Status = "Review",
                Summary = $"{data.UncoveredFunctions.Count} function(s) have no trained personnel.", Href = "/app/functions" });
        if (data.ProcessesNeedingAttention.Any())
        {
            var p = data.ProcessesNeedingAttention.First();
            items.Add(new FocusItem { Title = p.Name, Status = !p.HasOwner ? "Bottleneck" : "Review",
                Summary = !p.HasOwner ? "Process has no owner assigned." : "Process needs activities defined.", Href = $"/app/processes/{p.Id}" });
        }
        if (!items.Any())
        {
            items.Add(data.TotalPeople > 0 && data.TotalRoles > 0
                ? new FocusItem { Title = "Organization Health", Status = "Stable", Summary = "All roles covered. No critical gaps.", Href = "/app/people" }
                : new FocusItem { Title = "Getting Started", Status = "Review", Summary = "Add people and roles to begin.", Href = "/app/people" });
        }
        return items.Take(3).ToList();
    }

    private List<NextAction> GenerateNextActions(AnalysisData data)
    {
        var actions = new List<NextAction>();
        foreach (var role in data.EmptyRoles.Take(2))
            actions.Add(new NextAction { Action = $"Assign someone to the {role.Name} role.", Href = "/app/roles" });
        foreach (var role in data.SpofRoles.Take(2))
            if (actions.Count < 5) actions.Add(new NextAction { Action = $"Add backup for {role.Name} (only {role.AssigneeName}).", Href = "/app/roles" });
        foreach (var func in data.UncoveredFunctions.Take(2))
            if (actions.Count < 5) actions.Add(new NextAction { Action = $"Train someone on {func.Name}.", Href = "/app/functions" });
        foreach (var p in data.ProcessesNeedingAttention.Where(x => !x.HasOwner).Take(2))
            if (actions.Count < 5) actions.Add(new NextAction { Action = $"Assign owner to {p.Name} process.", Href = $"/app/processes/{p.Id}" });
        if (!actions.Any())
        {
            if (data.TotalProcesses == 0) actions.Add(new NextAction { Action = "Document your first business process.", Href = "/app/processes" });
            if (data.TotalFunctions == 0) actions.Add(new NextAction { Action = "Define business functions.", Href = "/app/functions" });
            if (data.TotalRoles == 0) actions.Add(new NextAction { Action = "Create roles for your organization.", Href = "/app/roles" });
            if (data.TotalPeople == 0) actions.Add(new NextAction { Action = "Add people to your organization.", Href = "/app/people" });
        }
        return actions.Take(5).ToList();
    }

    private async Task<AiInsightsResult?> GetAiEnhancedInsightsAsync(AnalysisData data, CancellationToken ct)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Org data: {data.TotalPeople} people, {data.TotalRoles} roles ({data.EmptyRoles.Count} empty, {data.SpofRoles.Count} SPOF), {data.TotalFunctions} functions ({data.UncoveredFunctions.Count} uncovered), {data.TotalProcesses} processes.");
        if (data.SpofRoles.Any()) prompt.AppendLine($"SPOFs: {string.Join(", ", data.SpofRoles.Take(3).Select(r => $"{r.Name}={r.AssigneeName}"))}");
        if (data.EmptyRoles.Any()) prompt.AppendLine($"Empty roles: {string.Join(", ", data.EmptyRoles.Take(3).Select(r => r.Name))}");
        prompt.AppendLine("Generate 3 focus items and 5 next actions as JSON.");

        var requestBody = new
        {
            model = "claude-sonnet-4-20250514", max_tokens = 800,
            system = "Return JSON: {\"focusItems\":[{\"title\":\"str\",\"status\":\"Bottleneck|Review|Stable\",\"summary\":\"str\",\"href\":\"str\"}],\"nextActions\":[{\"action\":\"str\",\"href\":\"str\"}]}. Max 3 focus, 5 actions. hrefs: /app/people, /app/roles, /app/functions, /app/processes",
            messages = new[] { new { role = "user", content = prompt.ToString() } }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        using var req = new HttpRequestMessage(HttpMethod.Post, ANTHROPIC_API_URL);
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await _httpClient.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;

        var respJson = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<JsonElement>(respJson);
        var text = parsed.GetProperty("content")[0].GetProperty("text").GetString();
        if (string.IsNullOrEmpty(text)) return null;

        return JsonSerializer.Deserialize<AiInsightsResult>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private class AnalysisData
    {
        public List<RoleInfo> EmptyRoles { get; set; } = new();
        public List<RoleInfo> SpofRoles { get; set; } = new();
        public List<FunctionInfo> UncoveredFunctions { get; set; } = new();
        public List<ProcessInfo> ProcessesNeedingAttention { get; set; } = new();
        public int TotalPeople, TotalRoles, TotalFunctions, TotalProcesses;
    }
    private class RoleInfo { public Guid Id; public string Name = "", Department = "", AssigneeName = ""; }
    private class FunctionInfo { public Guid Id; public string Name = "", Category = ""; }
    private class ProcessInfo { public Guid Id; public string Name = ""; public bool HasOwner; public int ActivityCount; }
    private class AiInsightsResult { public List<FocusItem> FocusItems { get; set; } = new(); public List<NextAction> NextActions { get; set; } = new(); }
}
