using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId}/ai-agents")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
public class AiAgentsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<AiAgentsController> _logger;

    public AiAgentsController(OrbitOSDbContext dbContext, ILogger<AiAgentsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all AI agents for the organization
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AiAgentDto>>> GetAgents(Guid organizationId, CancellationToken cancellationToken)
    {
        var agents = await _dbContext.AiAgents
            .Where(a => a.OrganizationId == organizationId)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name)
            .Select(a => new AiAgentDto
            {
                Id = a.Id,
                Name = a.Name,
                RoleTitle = a.RoleTitle,
                AvatarUrl = a.AvatarUrl,
                AvatarColor = a.AvatarColor,
                Provider = a.Provider.ToString().ToLower(),
                ModelId = a.ModelId,
                ModelDisplayName = a.ModelDisplayName,
                SystemPrompt = a.SystemPrompt,
                MaxTokensPerResponse = a.MaxTokensPerResponse,
                Temperature = a.Temperature,
                IsActive = a.IsActive,
                SortOrder = a.SortOrder,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                // Personality fields
                Assertiveness = a.Assertiveness,
                CommunicationStyle = a.CommunicationStyle.ToString(),
                ReactionTendency = a.ReactionTendency.ToString(),
                ExpertiseAreas = a.ExpertiseAreas,
                SeniorityLevel = a.SeniorityLevel,
                AsksQuestions = a.AsksQuestions,
                GivesBriefAcknowledgments = a.GivesBriefAcknowledgments
            })
            .ToListAsync(cancellationToken);

        return Ok(agents);
    }

    /// <summary>
    /// Get a specific AI agent
    /// </summary>
    [HttpGet("{agentId}")]
    public async Task<ActionResult<AiAgentDto>> GetAgent(Guid organizationId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await _dbContext.AiAgents
            .Where(a => a.OrganizationId == organizationId && a.Id == agentId)
            .Select(a => new AiAgentDto
            {
                Id = a.Id,
                Name = a.Name,
                RoleTitle = a.RoleTitle,
                AvatarUrl = a.AvatarUrl,
                AvatarColor = a.AvatarColor,
                Provider = a.Provider.ToString().ToLower(),
                ModelId = a.ModelId,
                ModelDisplayName = a.ModelDisplayName,
                SystemPrompt = a.SystemPrompt,
                MaxTokensPerResponse = a.MaxTokensPerResponse,
                Temperature = a.Temperature,
                IsActive = a.IsActive,
                SortOrder = a.SortOrder,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                // Personality fields
                Assertiveness = a.Assertiveness,
                CommunicationStyle = a.CommunicationStyle.ToString(),
                ReactionTendency = a.ReactionTendency.ToString(),
                ExpertiseAreas = a.ExpertiseAreas,
                SeniorityLevel = a.SeniorityLevel,
                AsksQuestions = a.AsksQuestions,
                GivesBriefAcknowledgments = a.GivesBriefAcknowledgments
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (agent == null)
            return NotFound();

        return Ok(agent);
    }

    /// <summary>
    /// Create a new AI agent
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AiAgentDto>> CreateAgent(
        Guid organizationId,
        CreateAiAgentRequest request,
        CancellationToken cancellationToken)
    {
        // Validate organization exists
        var orgExists = await _dbContext.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken);
        if (!orgExists)
            return NotFound("Organization not found");

        // Check for duplicate name
        var nameExists = await _dbContext.AiAgents
            .AnyAsync(a => a.OrganizationId == organizationId && a.Name == request.Name, cancellationToken);
        if (nameExists)
            return BadRequest("An agent with this name already exists");

        // Parse provider
        if (!Enum.TryParse<AiProvider>(request.Provider, true, out var provider))
            return BadRequest("Invalid provider. Must be one of: anthropic, openai, google");

        // Parse communication style
        var communicationStyle = CommunicationStyle.Formal;
        if (!string.IsNullOrEmpty(request.CommunicationStyle))
        {
            Enum.TryParse<CommunicationStyle>(request.CommunicationStyle, true, out communicationStyle);
        }

        // Parse reaction tendency
        var reactionTendency = ReactionTendency.Balanced;
        if (!string.IsNullOrEmpty(request.ReactionTendency))
        {
            Enum.TryParse<ReactionTendency>(request.ReactionTendency, true, out reactionTendency);
        }

        var agent = new AiAgent
        {
            Name = request.Name,
            RoleTitle = request.RoleTitle,
            AvatarUrl = request.AvatarUrl,
            AvatarColor = request.AvatarColor,
            Provider = provider,
            ModelId = request.ModelId,
            ModelDisplayName = request.ModelDisplayName,
            SystemPrompt = request.SystemPrompt,
            MaxTokensPerResponse = request.MaxTokensPerResponse ?? 4096,
            Temperature = request.Temperature ?? 0.7m,
            IsActive = request.IsActive ?? true,
            SortOrder = request.SortOrder ?? 0,
            OrganizationId = organizationId,
            // Personality fields
            Assertiveness = request.Assertiveness ?? 50,
            CommunicationStyle = communicationStyle,
            ReactionTendency = reactionTendency,
            ExpertiseAreas = request.ExpertiseAreas,
            SeniorityLevel = request.SeniorityLevel ?? 3,
            AsksQuestions = request.AsksQuestions ?? false,
            GivesBriefAcknowledgments = request.GivesBriefAcknowledgments ?? true
        };

        _dbContext.AiAgents.Add(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created AI agent {AgentName} ({Provider}/{Model}) for organization {OrgId}",
            agent.Name, agent.Provider, agent.ModelId, organizationId);

        return CreatedAtAction(nameof(GetAgent), new { organizationId, agentId = agent.Id }, new AiAgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            RoleTitle = agent.RoleTitle,
            AvatarUrl = agent.AvatarUrl,
            AvatarColor = agent.AvatarColor,
            Provider = agent.Provider.ToString().ToLower(),
            ModelId = agent.ModelId,
            ModelDisplayName = agent.ModelDisplayName,
            SystemPrompt = agent.SystemPrompt,
            MaxTokensPerResponse = agent.MaxTokensPerResponse,
            Temperature = agent.Temperature,
            IsActive = agent.IsActive,
            SortOrder = agent.SortOrder,
            CreatedAt = agent.CreatedAt,
            UpdatedAt = agent.UpdatedAt,
            // Personality fields
            Assertiveness = agent.Assertiveness,
            CommunicationStyle = agent.CommunicationStyle.ToString(),
            ReactionTendency = agent.ReactionTendency.ToString(),
            ExpertiseAreas = agent.ExpertiseAreas,
            SeniorityLevel = agent.SeniorityLevel,
            AsksQuestions = agent.AsksQuestions,
            GivesBriefAcknowledgments = agent.GivesBriefAcknowledgments
        });
    }

    /// <summary>
    /// Update an AI agent
    /// </summary>
    [HttpPut("{agentId}")]
    public async Task<ActionResult<AiAgentDto>> UpdateAgent(
        Guid organizationId,
        Guid agentId,
        UpdateAiAgentRequest request,
        CancellationToken cancellationToken)
    {
        var agent = await _dbContext.AiAgents
            .FirstOrDefaultAsync(a => a.OrganizationId == organizationId && a.Id == agentId, cancellationToken);

        if (agent == null)
            return NotFound();

        // Check for duplicate name if name is being changed
        if (request.Name != null && request.Name != agent.Name)
        {
            var nameExists = await _dbContext.AiAgents
                .AnyAsync(a => a.OrganizationId == organizationId && a.Name == request.Name && a.Id != agentId, cancellationToken);
            if (nameExists)
                return BadRequest("An agent with this name already exists");
        }

        // Update fields
        if (request.Name != null) agent.Name = request.Name;
        if (request.RoleTitle != null) agent.RoleTitle = request.RoleTitle;
        if (request.AvatarUrl != null) agent.AvatarUrl = request.AvatarUrl;
        if (request.AvatarColor != null) agent.AvatarColor = request.AvatarColor;

        if (request.Provider != null)
        {
            if (!Enum.TryParse<AiProvider>(request.Provider, true, out var provider))
                return BadRequest("Invalid provider");
            agent.Provider = provider;
        }

        if (request.ModelId != null) agent.ModelId = request.ModelId;
        if (request.ModelDisplayName != null) agent.ModelDisplayName = request.ModelDisplayName;
        if (request.SystemPrompt != null) agent.SystemPrompt = request.SystemPrompt;
        if (request.MaxTokensPerResponse != null) agent.MaxTokensPerResponse = request.MaxTokensPerResponse.Value;
        if (request.Temperature != null) agent.Temperature = request.Temperature.Value;
        if (request.IsActive != null) agent.IsActive = request.IsActive.Value;
        if (request.SortOrder != null) agent.SortOrder = request.SortOrder.Value;

        // Personality fields
        if (request.Assertiveness != null) agent.Assertiveness = Math.Clamp(request.Assertiveness.Value, 0, 100);
        if (request.CommunicationStyle != null && Enum.TryParse<CommunicationStyle>(request.CommunicationStyle, true, out var commStyle))
            agent.CommunicationStyle = commStyle;
        if (request.ReactionTendency != null && Enum.TryParse<ReactionTendency>(request.ReactionTendency, true, out var reactTendency))
            agent.ReactionTendency = reactTendency;
        if (request.ExpertiseAreas != null) agent.ExpertiseAreas = request.ExpertiseAreas;
        if (request.SeniorityLevel != null) agent.SeniorityLevel = Math.Clamp(request.SeniorityLevel.Value, 1, 5);
        if (request.AsksQuestions != null) agent.AsksQuestions = request.AsksQuestions.Value;
        if (request.GivesBriefAcknowledgments != null) agent.GivesBriefAcknowledgments = request.GivesBriefAcknowledgments.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated AI agent {AgentId} for organization {OrgId}", agentId, organizationId);

        return Ok(new AiAgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            RoleTitle = agent.RoleTitle,
            AvatarUrl = agent.AvatarUrl,
            AvatarColor = agent.AvatarColor,
            Provider = agent.Provider.ToString().ToLower(),
            ModelId = agent.ModelId,
            ModelDisplayName = agent.ModelDisplayName,
            SystemPrompt = agent.SystemPrompt,
            MaxTokensPerResponse = agent.MaxTokensPerResponse,
            Temperature = agent.Temperature,
            IsActive = agent.IsActive,
            SortOrder = agent.SortOrder,
            CreatedAt = agent.CreatedAt,
            UpdatedAt = agent.UpdatedAt,
            // Personality fields
            Assertiveness = agent.Assertiveness,
            CommunicationStyle = agent.CommunicationStyle.ToString(),
            ReactionTendency = agent.ReactionTendency.ToString(),
            ExpertiseAreas = agent.ExpertiseAreas,
            SeniorityLevel = agent.SeniorityLevel,
            AsksQuestions = agent.AsksQuestions,
            GivesBriefAcknowledgments = agent.GivesBriefAcknowledgments
        });
    }

    /// <summary>
    /// Delete an AI agent
    /// </summary>
    [HttpDelete("{agentId}")]
    public async Task<IActionResult> DeleteAgent(Guid organizationId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await _dbContext.AiAgents
            .FirstOrDefaultAsync(a => a.OrganizationId == organizationId && a.Id == agentId, cancellationToken);

        if (agent == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        agent.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted AI agent {AgentId} for organization {OrgId}", agentId, organizationId);

        return NoContent();
    }

    /// <summary>
    /// Get available AI models
    /// </summary>
    [HttpGet("available-models")]
    [AllowAnonymous]
    public ActionResult<List<AvailableModelDto>> GetAvailableModels()
    {
        var models = new List<AvailableModelDto>
        {
            // Anthropic models (currently the only configured provider)
            new() { Provider = "anthropic", ModelId = "claude-sonnet-4-20250514", DisplayName = "Claude Sonnet 4", Description = "Best balance of intelligence and speed", ContextWindow = 200000 },
            new() { Provider = "anthropic", ModelId = "claude-opus-4-20250514", DisplayName = "Claude Opus 4", Description = "Most capable model for complex tasks", ContextWindow = 200000 },
            new() { Provider = "anthropic", ModelId = "claude-3-5-haiku-20241022", DisplayName = "Claude 3.5 Haiku", Description = "Fastest model for simple tasks", ContextWindow = 200000 },

            // OpenAI models
            new() { Provider = "openai", ModelId = "gpt-4o", DisplayName = "GPT-4o", Description = "Most capable GPT-4 model", ContextWindow = 128000 },
            new() { Provider = "openai", ModelId = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Description = "Fast and affordable", ContextWindow = 128000 },
            new() { Provider = "openai", ModelId = "gpt-4-turbo", DisplayName = "GPT-4 Turbo", Description = "GPT-4 with vision capabilities", ContextWindow = 128000 },

            // Google models
            new() { Provider = "google", ModelId = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash", Description = "Latest fast multimodal model", ContextWindow = 1000000 },
            new() { Provider = "google", ModelId = "gemini-2.0-flash", DisplayName = "Gemini 2.0 Flash", Description = "Fast multimodal model", ContextWindow = 1000000 },
            new() { Provider = "google", ModelId = "gemini-1.5-pro", DisplayName = "Gemini 1.5 Pro", Description = "Most capable Gemini model", ContextWindow = 2000000 },
            new() { Provider = "google", ModelId = "gemini-1.5-flash", DisplayName = "Gemini 1.5 Flash", Description = "Fast and efficient", ContextWindow = 1000000 }
        };

        return Ok(models);
    }
}

// DTOs
public class AiAgentDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string RoleTitle { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarColor { get; set; }
    public required string Provider { get; set; }
    public required string ModelId { get; set; }
    public required string ModelDisplayName { get; set; }
    public required string SystemPrompt { get; set; }
    public int MaxTokensPerResponse { get; set; }
    public decimal Temperature { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Personality & Meeting Behavior (for Emergent mode)
    public int Assertiveness { get; set; } = 50;
    public string CommunicationStyle { get; set; } = "Formal";
    public string ReactionTendency { get; set; } = "Balanced";
    public string? ExpertiseAreas { get; set; }
    public int SeniorityLevel { get; set; } = 3;
    public bool AsksQuestions { get; set; } = false;
    public bool GivesBriefAcknowledgments { get; set; } = true;
}

public class CreateAiAgentRequest
{
    public required string Name { get; set; }
    public required string RoleTitle { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarColor { get; set; }
    public required string Provider { get; set; }
    public required string ModelId { get; set; }
    public required string ModelDisplayName { get; set; }
    public required string SystemPrompt { get; set; }
    public int? MaxTokensPerResponse { get; set; }
    public decimal? Temperature { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }

    // Personality & Meeting Behavior
    public int? Assertiveness { get; set; }
    public string? CommunicationStyle { get; set; }
    public string? ReactionTendency { get; set; }
    public string? ExpertiseAreas { get; set; }
    public int? SeniorityLevel { get; set; }
    public bool? AsksQuestions { get; set; }
    public bool? GivesBriefAcknowledgments { get; set; }
}

public class UpdateAiAgentRequest
{
    public string? Name { get; set; }
    public string? RoleTitle { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarColor { get; set; }
    public string? Provider { get; set; }
    public string? ModelId { get; set; }
    public string? ModelDisplayName { get; set; }
    public string? SystemPrompt { get; set; }
    public int? MaxTokensPerResponse { get; set; }
    public decimal? Temperature { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }

    // Personality & Meeting Behavior
    public int? Assertiveness { get; set; }
    public string? CommunicationStyle { get; set; }
    public string? ReactionTendency { get; set; }
    public string? ExpertiseAreas { get; set; }
    public int? SeniorityLevel { get; set; }
    public bool? AsksQuestions { get; set; }
    public bool? GivesBriefAcknowledgments { get; set; }
}

public class AvailableModelDto
{
    public required string Provider { get; set; }
    public required string ModelId { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public int ContextWindow { get; set; }
}
