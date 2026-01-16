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
    private readonly ILogger<AiController> _logger;

    public AiController(IAiChatService aiChatService, ILogger<AiController> logger)
    {
        _aiChatService = aiChatService;
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
                new { name = "chat", description = "General conversation and questions about your organization" },
                new { name = "create_person", description = "Add new people to your organization" },
                new { name = "update_person", description = "Update existing people's information" },
                new { name = "assign_role", description = "Assign roles to people" },
                new { name = "add_capability", description = "Add function capabilities to people" },
                new { name = "analyze_coverage", description = "Analyze organizational coverage and identify gaps" }
            },
            quickActions = new[]
            {
                new { label = "Analyze health", prompt = "Analyze the organizational coverage and identify any gaps or single points of failure" },
                new { label = "List people", prompt = "Who are the people in my organization and what are their roles?" },
                new { label = "Find SPOFs", prompt = "Which roles have single points of failure?" },
                new { label = "Coverage gaps", prompt = "Are there any uncovered roles or functions?" }
            }
        });
    }
}
