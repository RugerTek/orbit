using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Api.Services;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

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

public class OrchestrateRequestDto
{
    public required string Message { get; set; }
    public Guid? ConversationId { get; set; }
    public List<ChatMessageDto>? History { get; set; }
}

public class OrchestrateResponseDto
{
    public required string Message { get; set; }
    public List<string> AgentsConsulted { get; set; } = new();
    public List<InnerDialogueStepDto> InnerDialogue { get; set; } = new();
    public string? Error { get; set; }
}

public class InnerDialogueStepDto
{
    public int StepNumber { get; set; }
    public required string Type { get; set; } // Routing, Consulting, AgentToAgent, Synthesis, Reasoning
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? Query { get; set; }
    public string? Response { get; set; }
    public int? TokensUsed { get; set; }
}

public class ConsultSpecialistRequestDto
{
    public required string SpecialistKey { get; set; } // people, process, strategy, finance
    public required string Query { get; set; }
}

public class ConsultSpecialistResponseDto
{
    public required string Message { get; set; }
    public List<string> ContextUsed { get; set; } = new();
    public int TokensUsed { get; set; }
    public string? Error { get; set; }
}

public class AssistantChatHistoryDto
{
    public List<AssistantChatMessageDto> Messages { get; set; } = new();
}

public class AssistantChatMessageDto
{
    public Guid Id { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SaveChatHistoryRequest
{
    public required List<ChatMessageDto> Messages { get; set; }
}

#endregion

[ApiController]
[Route("api/organizations/{organizationId}/ai")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
public class AiController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IOrchestratorService _orchestratorService;
    private readonly IDashboardInsightsService _dashboardInsightsService;
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiChatService aiChatService,
        IOrchestratorService orchestratorService,
        IDashboardInsightsService dashboardInsightsService,
        OrbitOSDbContext dbContext,
        ILogger<AiController> logger)
    {
        _aiChatService = aiChatService;
        _orchestratorService = orchestratorService;
        _dashboardInsightsService = dashboardInsightsService;
        _dbContext = dbContext;
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

    /// <summary>
    /// Get chat history for the floating AI assistant
    /// </summary>
    [HttpGet("assistant/history")]
    public async Task<ActionResult<AssistantChatHistoryDto>> GetChatHistory(
        Guid organizationId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var messages = await _dbContext.AssistantChatMessages
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId)
            .OrderBy(m => m.SequenceNumber)
            .Select(m => new AssistantChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new AssistantChatHistoryDto { Messages = messages });
    }

    /// <summary>
    /// Save/sync chat history for the floating AI assistant
    /// </summary>
    [HttpPost("assistant/history")]
    public async Task<ActionResult> SaveChatHistory(
        Guid organizationId,
        [FromQuery] Guid userId,
        [FromBody] SaveChatHistoryRequest request,
        CancellationToken cancellationToken)
    {
        // Delete existing messages for this user/org
        var existingMessages = await _dbContext.AssistantChatMessages
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbContext.AssistantChatMessages.RemoveRange(existingMessages);

        // Add new messages
        var sequenceNumber = 0;
        foreach (var msg in request.Messages)
        {
            _dbContext.AssistantChatMessages.Add(new AssistantChatMessage
            {
                OrganizationId = organizationId,
                UserId = userId,
                Role = msg.Role,
                Content = msg.Content,
                SequenceNumber = sequenceNumber++
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Clear chat history for the floating AI assistant
    /// </summary>
    [HttpDelete("assistant/history")]
    public async Task<ActionResult> ClearChatHistory(
        Guid organizationId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var existingMessages = await _dbContext.AssistantChatMessages
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbContext.AssistantChatMessages.RemoveRange(existingMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Route a query through the orchestrator to consult specialist agents.
    /// Returns the synthesized response along with inner dialogue showing the AI's thought process.
    /// </summary>
    [HttpPost("orchestrate")]
    public async Task<ActionResult<OrchestrateResponseDto>> Orchestrate(
        Guid organizationId,
        [FromBody] OrchestrateRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Orchestrate request for organization {OrganizationId}: {Message}",
            organizationId, request.Message.Length > 100 ? request.Message[..100] + "..." : request.Message);

        var orchestratorRequest = new OrchestratorRequest
        {
            Message = request.Message,
            ConversationId = request.ConversationId,
            History = request.History?.Select(h => new ChatMessage
            {
                Role = h.Role,
                Content = h.Content
            }).ToList()
        };

        var result = await _orchestratorService.OrchestrateAsync(organizationId, orchestratorRequest, cancellationToken);

        // Build inner dialogue steps from contributions
        var innerDialogue = new List<InnerDialogueStepDto>();
        var stepNumber = 1;

        // Add routing step
        if (result.AgentsConsulted.Count > 0)
        {
            innerDialogue.Add(new InnerDialogueStepDto
            {
                StepNumber = stepNumber++,
                Type = "Routing",
                Title = "Routing query to specialists",
                Description = $"Identified relevant specialists: {string.Join(", ", result.AgentsConsulted)}"
            });
        }

        // Add consulting steps for each contribution
        foreach (var contribution in result.Contributions)
        {
            innerDialogue.Add(new InnerDialogueStepDto
            {
                StepNumber = stepNumber++,
                Type = "Consulting",
                Title = $"Consulting {contribution.AgentName}",
                Description = $"{contribution.SpecialistKey} specialist analysis",
                AgentName = contribution.AgentName,
                Response = contribution.Response.Length > 500
                    ? contribution.Response[..500] + "..."
                    : contribution.Response,
                TokensUsed = contribution.TokensUsed
            });
        }

        // Add synthesis step if multiple agents
        if (result.Contributions.Count > 1)
        {
            innerDialogue.Add(new InnerDialogueStepDto
            {
                StepNumber = stepNumber++,
                Type = "Synthesis",
                Title = "Synthesizing responses",
                Description = $"Combined insights from {result.Contributions.Count} specialists"
            });
        }

        return Ok(new OrchestrateResponseDto
        {
            Message = result.Message,
            AgentsConsulted = result.AgentsConsulted,
            InnerDialogue = innerDialogue,
            Error = result.Error
        });
    }

    /// <summary>
    /// Directly consult a specific specialist agent without orchestration.
    /// Useful for targeted queries when you know which specialist to ask.
    /// </summary>
    [HttpPost("consult-specialist")]
    public async Task<ActionResult<ConsultSpecialistResponseDto>> ConsultSpecialist(
        Guid organizationId,
        [FromBody] ConsultSpecialistRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Consult specialist request for organization {OrganizationId}: {Specialist} - {Query}",
            organizationId, request.SpecialistKey,
            request.Query.Length > 100 ? request.Query[..100] + "..." : request.Query);

        var validSpecialists = new[] { "people", "process", "strategy", "finance" };
        if (!validSpecialists.Contains(request.SpecialistKey.ToLowerInvariant()))
        {
            return BadRequest(new { error = $"Invalid specialist key. Valid options: {string.Join(", ", validSpecialists)}" });
        }

        var result = await _orchestratorService.ConsultSpecialistAsync(
            organizationId,
            request.SpecialistKey.ToLowerInvariant(),
            request.Query,
            cancellationToken);

        return Ok(new ConsultSpecialistResponseDto
        {
            Message = result.Message,
            ContextUsed = result.ContextUsed,
            TokensUsed = result.TokensUsed,
            Error = result.Error
        });
    }

    /// <summary>
    /// Get list of available specialist agents for this organization
    /// </summary>
    [HttpGet("specialists")]
    public async Task<ActionResult<object>> GetSpecialists(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var specialists = await _dbContext.AiAgents
            .Where(a => a.OrganizationId == organizationId
                && a.AgentType == Domain.Entities.AgentType.BuiltIn
                && a.SpecialistKey != null)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.SpecialistKey,
                a.IsActive,
                a.AvatarColor,
                Description = a.SpecialistKey == "people" ? "Team members, roles, functions, capacity, org chart"
                    : a.SpecialistKey == "process" ? "Workflows, activities, bottlenecks, automation"
                    : a.SpecialistKey == "strategy" ? "Business canvas, goals, partners, channels, revenue"
                    : a.SpecialistKey == "finance" ? "Costs, budgets, ROI analysis"
                    : "Specialist agent"
            })
            .ToListAsync(cancellationToken);

        return Ok(new { specialists });
    }
}
