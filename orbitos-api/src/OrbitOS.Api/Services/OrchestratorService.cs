using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Services;

/// <summary>
/// Orchestrator service for routing AI queries and managing Agent-to-Agent communication
/// </summary>
public interface IOrchestratorService
{
    /// <summary>
    /// Routes a query to appropriate agents and synthesizes the response
    /// </summary>
    Task<OrchestratorResponse> OrchestrateAsync(
        Guid organizationId,
        OrchestratorRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Directly consults a specific specialist agent
    /// </summary>
    Task<SpecialistResponse> ConsultSpecialistAsync(
        Guid organizationId,
        string specialistKey,
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the effective system prompt for an agent (base + custom + context)
    /// </summary>
    Task<string> BuildAgentPromptAsync(
        AiAgent agent,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

public class OrchestratorRequest
{
    public required string Message { get; set; }
    public Guid? ConversationId { get; set; }
    public List<ChatMessage>? History { get; set; }
}

public class ChatMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

public class OrchestratorResponse
{
    public required string Message { get; set; }
    public List<string> AgentsConsulted { get; set; } = new();
    public List<AgentContribution> Contributions { get; set; } = new();
    public string? Error { get; set; }
}

public class AgentContribution
{
    public required string AgentName { get; set; }
    public required string SpecialistKey { get; set; }
    public required string Response { get; set; }
    public int TokensUsed { get; set; }
}

public class SpecialistResponse
{
    public required string Message { get; set; }
    public List<string> ContextUsed { get; set; } = new();
    public int TokensUsed { get; set; }
    public string? Error { get; set; }
}

public class OrchestratorService : IOrchestratorService
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly IMultiProviderAiService _aiService;
    private readonly IScopedContextLoader _contextLoader;
    private readonly IBuiltInAgentService _builtInAgentService;
    private readonly ILogger<OrchestratorService> _logger;

    // Keywords that indicate which specialist should be consulted
    private static readonly Dictionary<string, string[]> _specialistKeywords = new()
    {
        ["people"] = new[] { "people", "team", "person", "staff", "employee", "hire", "capacity", "overload", "role", "assign", "who", "org chart", "report", "manager", "headcount" },
        ["process"] = new[] { "process", "workflow", "activity", "bottleneck", "automation", "handoff", "procedure", "step", "flow" },
        ["strategy"] = new[] { "strategy", "goal", "okr", "objective", "canvas", "business model", "partner", "channel", "revenue", "value proposition", "customer" },
        ["finance"] = new[] { "finance", "budget", "cost", "spending", "roi", "revenue", "profit", "expense", "financial" }
    };

    public OrchestratorService(
        OrbitOSDbContext dbContext,
        IMultiProviderAiService aiService,
        IScopedContextLoader contextLoader,
        IBuiltInAgentService builtInAgentService,
        ILogger<OrchestratorService> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _contextLoader = contextLoader;
        _builtInAgentService = builtInAgentService;
        _logger = logger;
    }

    public async Task<OrchestratorResponse> OrchestrateAsync(
        Guid organizationId,
        OrchestratorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure Built-in agents exist
            await _builtInAgentService.SeedBuiltInAgentsAsync(organizationId, cancellationToken);

            // Determine which specialists to consult based on query
            var relevantSpecialists = DetermineRelevantSpecialists(request.Message);

            if (relevantSpecialists.Count == 0)
            {
                // No specific specialists detected, consult all
                relevantSpecialists = new List<string> { "people", "process", "strategy" };
            }

            _logger.LogDebug("Query '{Query}' routed to specialists: {Specialists}",
                request.Message.Substring(0, Math.Min(50, request.Message.Length)),
                string.Join(", ", relevantSpecialists));

            // Get relevant Built-in agents
            var specialists = await _dbContext.AiAgents
                .Where(a => a.OrganizationId == organizationId
                    && a.AgentType == AgentType.BuiltIn
                    && a.IsActive
                    && a.SpecialistKey != null
                    && relevantSpecialists.Contains(a.SpecialistKey))
                .ToListAsync(cancellationToken);

            if (specialists.Count == 0)
            {
                return new OrchestratorResponse
                {
                    Message = "No active specialist agents available. Please enable Built-in agents in settings.",
                    Error = "NO_SPECIALISTS"
                };
            }

            var contributions = new List<AgentContribution>();

            // Consult each specialist in parallel
            var tasks = specialists.Select(async specialist =>
            {
                try
                {
                    var response = await ConsultSpecialistInternalAsync(
                        organizationId,
                        specialist,
                        request.Message,
                        request.History,
                        cancellationToken);

                    return new AgentContribution
                    {
                        AgentName = specialist.Name,
                        SpecialistKey = specialist.SpecialistKey!,
                        Response = response.Message,
                        TokensUsed = response.TokensUsed
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consulting specialist {SpecialistKey}", specialist.SpecialistKey);
                    return new AgentContribution
                    {
                        AgentName = specialist.Name,
                        SpecialistKey = specialist.SpecialistKey!,
                        Response = $"[Unable to consult {specialist.Name}]",
                        TokensUsed = 0
                    };
                }
            });

            var results = await Task.WhenAll(tasks);
            contributions.AddRange(results);

            // Synthesize responses if multiple specialists contributed
            string finalMessage;
            if (contributions.Count == 1)
            {
                finalMessage = contributions[0].Response;
            }
            else
            {
                finalMessage = SynthesizeResponses(request.Message, contributions);
            }

            return new OrchestratorResponse
            {
                Message = finalMessage,
                AgentsConsulted = contributions.Select(c => c.AgentName).ToList(),
                Contributions = contributions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in orchestration for org {OrgId}", organizationId);
            return new OrchestratorResponse
            {
                Message = "I encountered an error while processing your request. Please try again.",
                Error = ex.Message
            };
        }
    }

    public async Task<SpecialistResponse> ConsultSpecialistAsync(
        Guid organizationId,
        string specialistKey,
        string query,
        CancellationToken cancellationToken = default)
    {
        // Ensure Built-in agents exist
        await _builtInAgentService.SeedBuiltInAgentsAsync(organizationId, cancellationToken);

        var specialist = await _dbContext.AiAgents
            .FirstOrDefaultAsync(a => a.OrganizationId == organizationId
                && a.AgentType == AgentType.BuiltIn
                && a.SpecialistKey == specialistKey
                && a.IsActive,
                cancellationToken);

        if (specialist == null)
        {
            return new SpecialistResponse
            {
                Message = $"Specialist '{specialistKey}' not found or is disabled.",
                Error = "SPECIALIST_NOT_FOUND"
            };
        }

        return await ConsultSpecialistInternalAsync(organizationId, specialist, query, null, cancellationToken);
    }

    public async Task<string> BuildAgentPromptAsync(
        AiAgent agent,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // For Built-in agents, use BasePrompt + CustomInstructions + Scoped Context
        if (agent.AgentType == AgentType.BuiltIn)
        {
            sb.AppendLine("## Your Role");
            sb.AppendLine(agent.BasePrompt ?? agent.SystemPrompt);
            sb.AppendLine();

            // Add custom instructions if any
            if (!string.IsNullOrWhiteSpace(agent.CustomInstructions))
            {
                sb.AppendLine("## Organization-Specific Instructions");
                sb.AppendLine(agent.CustomInstructions);
                sb.AppendLine();
            }

            // Load scoped context
            if (!string.IsNullOrEmpty(agent.ContextScopesJson))
            {
                var scopes = JsonSerializer.Deserialize<string[]>(agent.ContextScopesJson) ?? Array.Empty<string>();
                if (scopes.Length > 0)
                {
                    var context = await _contextLoader.LoadContextAsync(organizationId, scopes, cancellationToken);
                    sb.AppendLine("## Organization Data (Your Scope)");
                    sb.AppendLine(_contextLoader.BuildContextString(context));
                }
            }
        }
        else
        {
            // For Custom agents, use SystemPrompt directly
            sb.AppendLine("## Your Role and Personality");
            sb.AppendLine(agent.SystemPrompt);
            sb.AppendLine();

            // If agent can call Built-in agents, add instructions
            if (agent.CanCallBuiltInAgents)
            {
                sb.AppendLine("## Expert Consultation");
                sb.AppendLine("You have access to specialist experts for detailed data analysis:");
                sb.AppendLine("- **People Expert**: Team members, roles, functions, capacity, org chart");
                sb.AppendLine("- **Process Expert**: Workflows, activities, bottlenecks, automation");
                sb.AppendLine("- **Strategy Expert**: Business canvas, goals, partners, channels, revenue");
                sb.AppendLine("- **Finance Expert**: Costs, budgets, ROI analysis");
                sb.AppendLine();
                sb.AppendLine("When you need detailed organizational data, explicitly request it from the appropriate expert.");
                sb.AppendLine("Format: \"@PeopleExpert: [your question]\" or \"@ProcessExpert: [your question]\"");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private async Task<SpecialistResponse> ConsultSpecialistInternalAsync(
        Guid organizationId,
        AiAgent specialist,
        string query,
        List<ChatMessage>? history,
        CancellationToken cancellationToken)
    {
        // Build the system prompt with scoped context
        var systemPrompt = await BuildAgentPromptAsync(specialist, organizationId, cancellationToken);

        // Build messages
        var messages = new List<ProviderMessage>();
        if (history != null)
        {
            foreach (var msg in history)
            {
                messages.Add(new ProviderMessage { Role = msg.Role, Content = msg.Content });
            }
        }
        messages.Add(new ProviderMessage { Role = "user", Content = query });

        // Call the AI
        var response = await _aiService.SendMessageAsync(specialist, systemPrompt, messages);

        // Get context scopes used
        var scopesUsed = new List<string>();
        if (!string.IsNullOrEmpty(specialist.ContextScopesJson))
        {
            scopesUsed = JsonSerializer.Deserialize<List<string>>(specialist.ContextScopesJson) ?? new List<string>();
        }

        return new SpecialistResponse
        {
            Message = response.Content ?? "No response from specialist.",
            ContextUsed = scopesUsed,
            TokensUsed = response.TokensUsed,
            Error = response.Error
        };
    }

    private List<string> DetermineRelevantSpecialists(string query)
    {
        var queryLower = query.ToLowerInvariant();
        var relevantSpecialists = new List<string>();

        foreach (var (specialistKey, keywords) in _specialistKeywords)
        {
            if (keywords.Any(k => queryLower.Contains(k)))
            {
                relevantSpecialists.Add(specialistKey);
            }
        }

        return relevantSpecialists.Distinct().ToList();
    }

    private string SynthesizeResponses(string query, List<AgentContribution> contributions)
    {
        var sb = new StringBuilder();

        // Simple synthesis - concatenate with headers
        // In a more advanced implementation, this could use an LLM to synthesize
        foreach (var contribution in contributions)
        {
            sb.AppendLine($"### {contribution.AgentName}");
            sb.AppendLine(contribution.Response);
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }
}
