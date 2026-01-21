using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrbitOS.Api.Services;

namespace OrbitOS.Api.Controllers;

#region DTOs

public class ChatRequest
{
    public required string Message { get; set; }
    public List<ChatMessageDto>? History { get; set; }
    public string? Context { get; set; }
}

public class ChatMessageDto
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

public class ChatResponse
{
    public required string Message { get; set; }
    public List<ToolCallDto>? ToolCalls { get; set; }
    public ChatErrorDto? Error { get; set; }
}

public class ToolCallDto
{
    public required string Tool { get; set; }
    public required string Action { get; set; }
    public object? Data { get; set; }
}

public class ChatErrorDto
{
    public required string Code { get; set; }
    public required string Message { get; set; }
}

#endregion

[ApiController]
[Route("api/organizations/{organizationId}/ai")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
public class AiController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IDashboardInsightsService _dashboardInsightsService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiChatService aiChatService,
        IDashboardInsightsService dashboardInsightsService,
        ILogger<AiController> logger)
    {
        _aiChatService = aiChatService;
        _dashboardInsightsService = dashboardInsightsService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message to the AI assistant
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat(
        Guid organizationId,
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI chat request for organization {OrganizationId}: {Message}",
            organizationId, request.Message.Length > 100 ? request.Message[..100] + "..." : request.Message);

        var aiRequest = new AiChatRequest
        {
            Message = request.Message,
            Context = request.Context,
            History = request.History?.Select(h => new AiChatMessage
            {
                Role = h.Role,
                Content = h.Content
            }).ToList()
        };

        var result = await _aiChatService.ChatAsync(organizationId, aiRequest, cancellationToken);

        var response = new ChatResponse
        {
            Message = result.Message,
            ToolCalls = result.ToolCalls?.Select(tc => new ToolCallDto
            {
                Tool = tc.Tool,
                Action = tc.Action,
                Data = tc.Data.HasValue ? tc.Data.Value : null
            }).ToList(),
            Error = result.Error != null ? new ChatErrorDto
            {
                Code = result.Error.Code,
                Message = result.Error.Message
            } : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Get AI assistant capabilities for this organization
    /// </summary>
    [HttpGet("capabilities")]
    public ActionResult<object> GetCapabilities(Guid organizationId)
    {
        return Ok(new
        {
            capabilities = new[]
            {
                // Core
                new { name = "chat", description = "General conversation and questions about your organization" },
                new { name = "analyze_coverage", description = "Analyze organizational coverage and identify gaps" },
                // People & Roles
                new { name = "create_person", description = "Add new people to your organization" },
                new { name = "update_person", description = "Update existing people's information" },
                new { name = "assign_role", description = "Assign roles to people" },
                new { name = "add_capability", description = "Add function capabilities to people" },
                new { name = "create_role", description = "Create new organizational roles" },
                new { name = "bulk_create_roles", description = "Create multiple roles at once" },
                // Functions
                new { name = "create_function", description = "Create business functions/capabilities" },
                new { name = "bulk_create_functions", description = "Create multiple functions at once" },
                new { name = "suggest_functions", description = "Get AI suggestions for functions based on context" },
                // Processes
                new { name = "create_process", description = "Create business processes" },
                new { name = "bulk_create_processes", description = "Create multiple processes at once" },
                // Goals
                new { name = "create_goal", description = "Create objectives, key results, or initiatives" },
                new { name = "bulk_create_goals", description = "Create multiple goals at once" },
                // Business Model Canvas
                new { name = "create_partner", description = "Create key partners" },
                new { name = "bulk_create_partners", description = "Create multiple partners at once" },
                new { name = "create_channel", description = "Create sales/marketing channels" },
                new { name = "bulk_create_channels", description = "Create multiple channels at once" },
                new { name = "create_value_proposition", description = "Create value propositions" },
                new { name = "bulk_create_value_propositions", description = "Create multiple value propositions" },
                new { name = "create_customer_relationship", description = "Create customer relationship types" },
                new { name = "bulk_create_customer_relationships", description = "Create multiple customer relationships" },
                new { name = "create_revenue_stream", description = "Create revenue streams" },
                new { name = "bulk_create_revenue_streams", description = "Create multiple revenue streams" }
            },
            quickActions = new[]
            {
                new { label = "Analyze health", prompt = "Analyze the organizational coverage and identify any gaps or single points of failure" },
                new { label = "List people", prompt = "Who are the people in my organization and what are their roles?" },
                new { label = "Suggest functions", prompt = "Suggest the top 10 financial functions every company should have" },
                new { label = "Fill BMC", prompt = "Help me fill in my Business Model Canvas - what partners, channels, and value propositions should I consider?" },
                new { label = "Create OKRs", prompt = "Help me create a set of OKRs (Objectives and Key Results) for this quarter" },
                new { label = "Define processes", prompt = "What are the core business processes a company like ours should have?" }
            }
        });
    }

    /// <summary>
    /// Get AI-powered dashboard insights including stats, focus items, and next actions
    /// </summary>
    [HttpGet("dashboard-insights")]
    public async Task<ActionResult<DashboardInsightsResponse>> GetDashboardInsights(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Dashboard insights request for organization {OrganizationId}", organizationId);

        var insights = await _dashboardInsightsService.GetInsightsAsync(organizationId, cancellationToken);

        return Ok(insights);
    }
}
