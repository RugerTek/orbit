using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Api.Hubs;
using OrbitOS.Api.Services;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers;

[ApiController]
[Route("api/organizations/{organizationId}/conversations")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
public class ConversationsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly IMultiProviderAiService _aiService;
    private readonly IOrganizationContextService _contextService;
    private readonly IHubContext<ConversationHub> _hubContext;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        OrbitOSDbContext dbContext,
        IMultiProviderAiService aiService,
        IOrganizationContextService contextService,
        IHubContext<ConversationHub> hubContext,
        ILogger<ConversationsController> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _contextService = contextService;
        _hubContext = hubContext;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId) && userId != Guid.Empty)
            return userId;

        // For local development without auth, use the seeded test user
        // TODO: Remove this fallback in production
        return Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    /// <summary>
    /// Get all conversations for the organization
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetConversations(
        Guid organizationId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Conversations
            .Where(c => c.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ConversationStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                Mode = c.Mode.ToString(),
                Status = c.Status.ToString(),
                MessageCount = c.MessageCount,
                AiResponseCount = c.AiResponseCount,
                TotalTokens = c.TotalTokens,
                TotalCost = c.TotalCostCents / 100.0m,
                LastMessageAt = c.LastMessageAt,
                StartedAt = c.StartedAt,
                CreatedAt = c.CreatedAt,
                ParticipantCount = c.Participants.Count(p => p.IsActive)
            })
            .ToListAsync(cancellationToken);

        return Ok(conversations);
    }

    /// <summary>
    /// Get a specific conversation with participants and recent messages
    /// </summary>
    [HttpGet("{conversationId}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(
        Guid organizationId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants.Where(p => p.IsActive))
                .ThenInclude(p => p.User)
            .Include(c => c.Participants.Where(p => p.IsActive))
                .ThenInclude(p => p.AiAgent)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        return Ok(MapToConversationDto(conversation));
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConversationDto>> CreateConversation(
        Guid organizationId,
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        // Validate organization exists
        var orgExists = await _dbContext.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken);
        if (!orgExists)
            return NotFound("Organization not found");

        var userId = GetCurrentUserId();

        // Parse mode
        if (!Enum.TryParse<ConversationMode>(request.Mode, true, out var mode))
        {
            mode = ConversationMode.OnDemand;
        }

        var conversation = new Conversation
        {
            Title = request.Title,
            Mode = mode,
            Status = ConversationStatus.Active,
            StartedAt = DateTime.UtcNow,
            OrganizationId = organizationId,
            CreatedByUserId = userId,
            MaxTurns = request.MaxTurns,
            MaxTokens = request.MaxTokens
        };

        _dbContext.Conversations.Add(conversation);

        // Add the creator as owner
        var creatorParticipant = new ConversationParticipant
        {
            ConversationId = conversation.Id,
            ParticipantType = ParticipantType.User,
            UserId = userId,
            Role = ParticipantRole.Owner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
        _dbContext.ConversationParticipants.Add(creatorParticipant);

        // Add AI agents as participants
        if (request.AiAgentIds?.Any() == true)
        {
            var agents = await _dbContext.AiAgents
                .Where(a => a.OrganizationId == organizationId && request.AiAgentIds.Contains(a.Id) && a.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var agent in agents)
            {
                var participant = new ConversationParticipant
                {
                    ConversationId = conversation.Id,
                    ParticipantType = ParticipantType.Ai,
                    AiAgentId = agent.Id,
                    Role = ParticipantRole.Participant,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _dbContext.ConversationParticipants.Add(participant);
            }
        }

        // Add additional users as participants
        if (request.UserIds?.Any() == true)
        {
            var users = await _dbContext.Users
                .Where(u => request.UserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            foreach (var user in users.Where(u => u.Id != userId))
            {
                var participant = new ConversationParticipant
                {
                    ConversationId = conversation.Id,
                    ParticipantType = ParticipantType.User,
                    UserId = user.Id,
                    Role = ParticipantRole.Participant,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _dbContext.ConversationParticipants.Add(participant);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created conversation {ConversationId} for organization {OrgId}", conversation.Id, organizationId);

        // Reload with participants
        var created = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Participants)
                .ThenInclude(p => p.AiAgent)
            .FirstAsync(c => c.Id == conversation.Id, cancellationToken);

        return CreatedAtAction(nameof(GetConversation), new { organizationId, conversationId = conversation.Id }, MapToConversationDto(created));
    }

    /// <summary>
    /// Update conversation settings
    /// </summary>
    [HttpPut("{conversationId}/settings")]
    public async Task<ActionResult<ConversationDto>> UpdateSettings(
        Guid organizationId,
        Guid conversationId,
        UpdateConversationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Participants)
                .ThenInclude(p => p.AiAgent)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.Title))
            conversation.Title = request.Title;

        if (!string.IsNullOrEmpty(request.Mode) && Enum.TryParse<ConversationMode>(request.Mode, true, out var mode))
            conversation.Mode = mode;

        if (request.MaxTurns.HasValue)
            conversation.MaxTurns = request.MaxTurns;

        if (request.MaxTokens.HasValue)
            conversation.MaxTokens = request.MaxTokens;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToConversationDto(conversation));
    }

    /// <summary>
    /// Pause the conversation (AI agents stop responding)
    /// </summary>
    [HttpPost("{conversationId}/pause")]
    public async Task<ActionResult<ConversationDto>> PauseConversation(
        Guid organizationId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Participants)
                .ThenInclude(p => p.AiAgent)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        conversation.Status = ConversationStatus.Paused;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Paused conversation {ConversationId}", conversationId);

        return Ok(MapToConversationDto(conversation));
    }

    /// <summary>
    /// Resume the conversation
    /// </summary>
    [HttpPost("{conversationId}/resume")]
    public async Task<ActionResult<ConversationDto>> ResumeConversation(
        Guid organizationId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Participants)
                .ThenInclude(p => p.AiAgent)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        conversation.Status = ConversationStatus.Active;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Resumed conversation {ConversationId}", conversationId);

        return Ok(MapToConversationDto(conversation));
    }

    /// <summary>
    /// Delete a conversation (soft delete by archiving)
    /// </summary>
    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> DeleteConversation(
        Guid organizationId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        conversation.Status = ConversationStatus.Archived;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Archived conversation {ConversationId}", conversationId);

        return NoContent();
    }

    /// <summary>
    /// Get messages for a conversation (paginated)
    /// </summary>
    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<PaginatedMessagesResponse>> GetMessages(
        Guid organizationId,
        Guid conversationId,
        [FromQuery] int? beforeSequence,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var conversationExists = await _dbContext.Conversations
            .AnyAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (!conversationExists)
            return NotFound();

        var query = _dbContext.Messages
            .Include(m => m.SenderUser)
            .Include(m => m.SenderAiAgent)
            .Where(m => m.ConversationId == conversationId);

        if (beforeSequence.HasValue)
        {
            query = query.Where(m => m.SequenceNumber < beforeSequence.Value);
        }

        var messages = await query
            .OrderByDescending(m => m.SequenceNumber)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > limit;
        if (hasMore)
            messages = messages.Take(limit).ToList();

        return Ok(new PaginatedMessagesResponse
        {
            Messages = messages.OrderBy(m => m.SequenceNumber).Select(MapToMessageDto).ToList(),
            HasMore = hasMore,
            NextCursor = hasMore ? messages.Last().SequenceNumber : null
        });
    }

    /// <summary>
    /// Send a message to the conversation
    /// </summary>
    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(
        Guid organizationId,
        Guid conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.AiAgent)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        if (conversation.Status == ConversationStatus.Archived)
            return BadRequest("Cannot send messages to archived conversation");

        var userId = GetCurrentUserId();

        // Get the next sequence number
        var maxSequence = await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .MaxAsync(m => (int?)m.SequenceNumber, cancellationToken) ?? 0;

        // Parse @mentions
        var mentionedAgentIds = ParseMentions(request.Content, conversation.Participants
            .Where(p => p.ParticipantType == ParticipantType.Ai && p.AiAgent != null)
            .Select(p => p.AiAgent!)
            .ToList());

        var message = new Message
        {
            ConversationId = conversationId,
            SenderType = SenderType.User,
            SenderUserId = userId,
            Content = request.Content,
            MentionedAgentIdsJson = mentionedAgentIds.Any() ? JsonSerializer.Serialize(mentionedAgentIds) : null,
            Status = MessageStatus.Sent,
            SequenceNumber = maxSequence + 1
        };

        _dbContext.Messages.Add(message);

        // Update conversation stats
        conversation.MessageCount++;
        conversation.LastMessageAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Load the sender user for the response
        var senderUser = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        message.SenderUser = senderUser;

        _logger.LogInformation("User sent message {MessageId} to conversation {ConversationId}", message.Id, conversationId);

        // Broadcast message to all clients in the conversation
        var groupName = ConversationHub.GetConversationGroupName(organizationId.ToString(), conversationId.ToString());
        var messageDto = MapToMessageDto(message);
        await _hubContext.Clients.Group(groupName).SendAsync("NewMessage", messageDto, cancellationToken);

        // Send notification to other participants for unread count
        await BroadcastUnreadNotifications(organizationId, conversationId, userId, message, cancellationToken);

        return Ok(messageDto);
    }

    /// <summary>
    /// Invoke AI agents to respond to the conversation
    /// </summary>
    [HttpPost("{conversationId}/invoke")]
    public async Task<ActionResult<List<MessageDto>>> InvokeAgents(
        Guid organizationId,
        Guid conversationId,
        InvokeAgentRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.AiAgent)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        if (conversation.Status != ConversationStatus.Active)
            return BadRequest("Conversation is paused or archived");

        // Get agents to invoke
        List<AiAgent> agentsToInvoke;
        if (request.AgentIds?.Any() == true)
        {
            agentsToInvoke = conversation.Participants
                .Where(p => p.ParticipantType == ParticipantType.Ai && p.AiAgent != null && request.AgentIds.Contains(p.AiAgent.Id))
                .Select(p => p.AiAgent!)
                .ToList();
        }
        else
        {
            // Invoke all active AI agents
            agentsToInvoke = conversation.Participants
                .Where(p => p.ParticipantType == ParticipantType.Ai && p.AiAgent != null && p.IsActive)
                .Select(p => p.AiAgent!)
                .ToList();
        }

        if (!agentsToInvoke.Any())
            return BadRequest("No active AI agents to invoke");

        // Get recent messages for context
        var recentMessages = await _dbContext.Messages
            .Include(m => m.SenderUser)
            .Include(m => m.SenderAiAgent)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SequenceNumber)
            .Take(20)
            .ToListAsync(cancellationToken);

        recentMessages = recentMessages.OrderBy(m => m.SequenceNumber).ToList();

        // Check for empty conversation - AI providers require at least one message
        if (!recentMessages.Any())
            return BadRequest("Cannot invoke AI on an empty conversation. Send a message first.");

        // Build organization context for all agents (shared)
        var orgContext = await _contextService.BuildContextAsync(organizationId, cancellationToken);

        var responses = new List<MessageDto>();
        var maxSequence = recentMessages.Max(m => m.SequenceNumber);

        foreach (var agent in agentsToInvoke)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Build conversation history for the AI
                var conversationHistory = recentMessages.Select(m => new ProviderMessage
                {
                    Role = m.SenderType == SenderType.User ? "user" : "assistant",
                    Content = m.Content
                }).ToList();

                // Build system prompt with organization context + agent's custom prompt
                var systemPrompt = _contextService.BuildSystemPrompt(orgContext, agent.SystemPrompt);

                // Call the AI service with enhanced context
                var response = await _aiService.SendMessageAsync(
                    agent,
                    systemPrompt,
                    conversationHistory,
                    cancellationToken);

                var aiMessage = new Message
                {
                    ConversationId = conversationId,
                    SenderType = SenderType.Ai,
                    SenderAiAgentId = agent.Id,
                    Content = response.Content,
                    TokensUsed = response.TokensUsed,
                    ResponseTimeMs = response.ResponseTimeMs,
                    CostCents = CalculateCost(response.TokensUsed, agent.Provider),
                    ModelUsed = agent.ModelId,
                    Status = MessageStatus.Sent,
                    SequenceNumber = ++maxSequence
                };

                _dbContext.Messages.Add(aiMessage);

                // Update conversation stats
                conversation.MessageCount++;
                conversation.AiResponseCount++;
                conversation.TotalTokens += response.TokensUsed;
                conversation.TotalCostCents += aiMessage.CostCents ?? 0;
                conversation.LastMessageAt = DateTime.UtcNow;

                aiMessage.SenderAiAgent = agent;
                responses.Add(MapToMessageDto(aiMessage));

                _logger.LogInformation("AI agent {AgentName} responded to conversation {ConversationId} using {Tokens} tokens",
                    agent.Name, conversationId, response.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invoke AI agent {AgentId} for conversation {ConversationId}", agent.Id, conversationId);

                // Create a failed message entry
                var failedMessage = new Message
                {
                    ConversationId = conversationId,
                    SenderType = SenderType.Ai,
                    SenderAiAgentId = agent.Id,
                    Content = $"Error: Failed to generate response. {ex.Message}",
                    Status = MessageStatus.Failed,
                    SequenceNumber = ++maxSequence
                };

                _dbContext.Messages.Add(failedMessage);
                failedMessage.SenderAiAgent = agent;
                responses.Add(MapToMessageDto(failedMessage));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast AI responses to all clients in the conversation
        var groupName = ConversationHub.GetConversationGroupName(organizationId.ToString(), conversationId.ToString());
        foreach (var response in responses)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("NewMessage", response, cancellationToken);
        }

        // Send notification to human participants for unread count
        var userParticipants = conversation.Participants
            .Where(p => p.ParticipantType == ParticipantType.User && p.IsActive && p.UserId.HasValue)
            .Select(p => p.UserId!.Value)
            .ToList();

        foreach (var participantUserId in userParticipants)
        {
            var notificationGroup = ConversationHub.GetNotificationGroupName(organizationId.ToString(), participantUserId.ToString());
            await _hubContext.Clients.Group(notificationGroup).SendAsync("UnreadCount", new UnreadCountNotification(
                conversationId,
                responses.Count
            ), cancellationToken);
        }

        return Ok(responses);
    }

    /// <summary>
    /// Add a participant to the conversation
    /// </summary>
    [HttpPost("{conversationId}/participants")]
    public async Task<ActionResult<ParticipantDto>> AddParticipant(
        Guid organizationId,
        Guid conversationId,
        AddParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == conversationId, cancellationToken);

        if (conversation == null)
            return NotFound();

        ConversationParticipant participant;

        if (request.UserId.HasValue)
        {
            var user = await _dbContext.Users.FindAsync(new object[] { request.UserId.Value }, cancellationToken);
            if (user == null)
                return NotFound("User not found");

            // Check if already a participant
            var existing = await _dbContext.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == request.UserId, cancellationToken);

            if (existing != null)
            {
                if (existing.IsActive)
                    return BadRequest("User is already a participant");

                // Reactivate
                existing.IsActive = true;
                existing.LeftAt = null;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Ok(new ParticipantDto
                {
                    Id = existing.Id,
                    ParticipantType = "user",
                    Role = existing.Role.ToString(),
                    JoinedAt = existing.JoinedAt,
                    IsActive = true,
                    User = new UserSummaryDto
                    {
                        Id = user.Id,
                        DisplayName = user.DisplayName,
                        Email = user.Email,
                        AvatarUrl = user.AvatarUrl
                    }
                });
            }

            participant = new ConversationParticipant
            {
                ConversationId = conversationId,
                ParticipantType = ParticipantType.User,
                UserId = request.UserId,
                Role = ParticipantRole.Participant,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
        }
        else if (request.AiAgentId.HasValue)
        {
            var agent = await _dbContext.AiAgents
                .FirstOrDefaultAsync(a => a.OrganizationId == organizationId && a.Id == request.AiAgentId, cancellationToken);

            if (agent == null)
                return NotFound("AI agent not found");

            // Check if already a participant
            var existing = await _dbContext.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.AiAgentId == request.AiAgentId, cancellationToken);

            if (existing != null)
            {
                if (existing.IsActive)
                    return BadRequest("AI agent is already a participant");

                // Reactivate
                existing.IsActive = true;
                existing.LeftAt = null;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Ok(new ParticipantDto
                {
                    Id = existing.Id,
                    ParticipantType = "ai",
                    Role = existing.Role.ToString(),
                    JoinedAt = existing.JoinedAt,
                    IsActive = true,
                    AiAgent = new AiAgentSummaryDto
                    {
                        Id = agent.Id,
                        Name = agent.Name,
                        RoleTitle = agent.RoleTitle,
                        AvatarColor = agent.AvatarColor,
                        Provider = agent.Provider.ToString().ToLower(),
                        ModelDisplayName = agent.ModelDisplayName
                    }
                });
            }

            participant = new ConversationParticipant
            {
                ConversationId = conversationId,
                ParticipantType = ParticipantType.Ai,
                AiAgentId = request.AiAgentId,
                Role = ParticipantRole.Participant,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
        }
        else
        {
            return BadRequest("Either userId or aiAgentId must be provided");
        }

        _dbContext.ConversationParticipants.Add(participant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        await _dbContext.Entry(participant).Reference(p => p.User).LoadAsync(cancellationToken);
        await _dbContext.Entry(participant).Reference(p => p.AiAgent).LoadAsync(cancellationToken);

        return Ok(MapToParticipantDto(participant));
    }

    /// <summary>
    /// Remove a participant from the conversation
    /// </summary>
    [HttpDelete("{conversationId}/participants/{participantId}")]
    public async Task<IActionResult> RemoveParticipant(
        Guid organizationId,
        Guid conversationId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var participant = await _dbContext.ConversationParticipants
            .Include(p => p.Conversation)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.ConversationId == conversationId && p.Conversation.OrganizationId == organizationId, cancellationToken);

        if (participant == null)
            return NotFound();

        if (participant.Role == ParticipantRole.Owner)
            return BadRequest("Cannot remove the conversation owner");

        participant.IsActive = false;
        participant.LeftAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    #region Helper Methods

    private async Task BroadcastUnreadNotifications(
        Guid organizationId,
        Guid conversationId,
        Guid senderUserId,
        Message message,
        CancellationToken cancellationToken)
    {
        // Get all human participants except the sender
        var participants = await _dbContext.ConversationParticipants
            .Where(p => p.ConversationId == conversationId &&
                        p.ParticipantType == ParticipantType.User &&
                        p.IsActive &&
                        p.UserId.HasValue &&
                        p.UserId != senderUserId)
            .ToListAsync(cancellationToken);

        var senderUser = await _dbContext.Users.FindAsync(new object[] { senderUserId }, cancellationToken);
        var senderName = senderUser?.DisplayName ?? "Unknown";

        foreach (var participant in participants)
        {
            var notificationGroup = ConversationHub.GetNotificationGroupName(organizationId.ToString(), participant.UserId!.Value.ToString());

            // Send new message notification
            var notification = new NewMessageNotification(
                conversationId,
                message.Id,
                "user",
                senderName,
                null,
                message.Content.Length > 100 ? message.Content[..100] + "..." : message.Content,
                message.SequenceNumber,
                message.CreatedAt
            );

            await _hubContext.Clients.Group(notificationGroup).SendAsync("NewMessageNotification", notification, cancellationToken);
        }
    }

    private static int CalculateCost(int tokensUsed, AiProvider provider)
    {
        // Cost calculation in cents per 1000 tokens (approximate)
        // These are rough estimates - actual pricing varies by model
        var costPer1000Tokens = provider switch
        {
            AiProvider.Anthropic => 0.3m,   // ~$3/1M tokens for Sonnet
            AiProvider.OpenAI => 0.5m,      // ~$5/1M tokens for GPT-4o
            AiProvider.Google => 0.2m,      // ~$2/1M tokens for Gemini
            _ => 0.3m
        };

        return (int)Math.Ceiling(tokensUsed / 1000.0m * costPer1000Tokens);
    }

    private List<Guid> ParseMentions(string content, List<AiAgent> availableAgents)
    {
        var mentions = new List<Guid>();
        var contentLower = content.ToLowerInvariant();

        foreach (var agent in availableAgents)
        {
            // Check for @AgentName mention (case-insensitive)
            // Also handle names with spaces by normalizing to match user input patterns
            var nameLower = agent.Name.ToLowerInvariant();

            // Try exact match first: @Updated CFO
            if (contentLower.Contains($"@{nameLower}"))
            {
                mentions.Add(agent.Id);
                continue;
            }

            // Also match without spaces in the name: @updatedcfo
            var nameNoSpaces = nameLower.Replace(" ", "");
            if (contentLower.Contains($"@{nameNoSpaces}"))
            {
                mentions.Add(agent.Id);
                continue;
            }

            // Also match with hyphen/underscore: @updated-cfo or @updated_cfo
            var nameHyphen = nameLower.Replace(" ", "-");
            var nameUnderscore = nameLower.Replace(" ", "_");
            if (contentLower.Contains($"@{nameHyphen}") || contentLower.Contains($"@{nameUnderscore}"))
            {
                mentions.Add(agent.Id);
            }
        }

        return mentions;
    }

    private ConversationDto MapToConversationDto(Conversation c)
    {
        return new ConversationDto
        {
            Id = c.Id,
            Title = c.Title,
            Mode = c.Mode.ToString(),
            Status = c.Status.ToString(),
            TotalTokens = c.TotalTokens,
            TotalCost = c.TotalCostCents / 100.0m,
            MessageCount = c.MessageCount,
            AiResponseCount = c.AiResponseCount,
            MaxTurns = c.MaxTurns,
            MaxTokens = c.MaxTokens,
            LastMessageAt = c.LastMessageAt,
            StartedAt = c.StartedAt,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Participants = c.Participants.Where(p => p.IsActive).Select(MapToParticipantDto).ToList()
        };
    }

    private ParticipantDto MapToParticipantDto(ConversationParticipant p)
    {
        return new ParticipantDto
        {
            Id = p.Id,
            ParticipantType = p.ParticipantType.ToString().ToLower(),
            Role = p.Role.ToString().ToLower(),
            JoinedAt = p.JoinedAt,
            IsActive = p.IsActive,
            User = p.User != null ? new UserSummaryDto
            {
                Id = p.User.Id,
                DisplayName = p.User.DisplayName,
                Email = p.User.Email,
                AvatarUrl = p.User.AvatarUrl
            } : null,
            AiAgent = p.AiAgent != null ? new AiAgentSummaryDto
            {
                Id = p.AiAgent.Id,
                Name = p.AiAgent.Name,
                RoleTitle = p.AiAgent.RoleTitle,
                AvatarColor = p.AiAgent.AvatarColor,
                Provider = p.AiAgent.Provider.ToString().ToLower(),
                ModelDisplayName = p.AiAgent.ModelDisplayName
            } : null
        };
    }

    private MessageDto MapToMessageDto(Message m)
    {
        return new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderType = m.SenderType.ToString().ToLower(),
            Content = m.Content,
            ContentHtml = m.ContentHtml,
            TokensUsed = m.TokensUsed,
            ResponseTimeMs = m.ResponseTimeMs,
            Cost = m.CostCents.HasValue ? m.CostCents.Value / 100.0m : null,
            ModelUsed = m.ModelUsed,
            Status = m.Status.ToString().ToLower(),
            SequenceNumber = m.SequenceNumber,
            CreatedAt = m.CreatedAt,
            SenderUser = m.SenderUser != null ? new UserSummaryDto
            {
                Id = m.SenderUser.Id,
                DisplayName = m.SenderUser.DisplayName,
                Email = m.SenderUser.Email,
                AvatarUrl = m.SenderUser.AvatarUrl
            } : null,
            SenderAiAgent = m.SenderAiAgent != null ? new AiAgentSummaryDto
            {
                Id = m.SenderAiAgent.Id,
                Name = m.SenderAiAgent.Name,
                RoleTitle = m.SenderAiAgent.RoleTitle,
                AvatarColor = m.SenderAiAgent.AvatarColor,
                Provider = m.SenderAiAgent.Provider.ToString().ToLower(),
                ModelDisplayName = m.SenderAiAgent.ModelDisplayName
            } : null,
            MentionedAgentIds = !string.IsNullOrEmpty(m.MentionedAgentIdsJson)
                ? JsonSerializer.Deserialize<List<Guid>>(m.MentionedAgentIdsJson)
                : null
        };
    }

    #endregion
}

#region DTOs

public class ConversationSummaryDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Mode { get; set; }
    public required string Status { get; set; }
    public int MessageCount { get; set; }
    public int AiResponseCount { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ParticipantCount { get; set; }
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Mode { get; set; }
    public required string Status { get; set; }
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public int MessageCount { get; set; }
    public int AiResponseCount { get; set; }
    public int? MaxTurns { get; set; }
    public long? MaxTokens { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
}

public class ParticipantDto
{
    public Guid Id { get; set; }
    public required string ParticipantType { get; set; }
    public required string Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
    public UserSummaryDto? User { get; set; }
    public AiAgentSummaryDto? AiAgent { get; set; }
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public string? AvatarUrl { get; set; }
}

public class AiAgentSummaryDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string RoleTitle { get; set; }
    public string? AvatarColor { get; set; }
    public required string Provider { get; set; }
    public required string ModelDisplayName { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public required string SenderType { get; set; }
    public required string Content { get; set; }
    public string? ContentHtml { get; set; }
    public int? TokensUsed { get; set; }
    public int? ResponseTimeMs { get; set; }
    public decimal? Cost { get; set; }
    public string? ModelUsed { get; set; }
    public required string Status { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserSummaryDto? SenderUser { get; set; }
    public AiAgentSummaryDto? SenderAiAgent { get; set; }
    public List<Guid>? MentionedAgentIds { get; set; }
}

public class PaginatedMessagesResponse
{
    public List<MessageDto> Messages { get; set; } = new();
    public bool HasMore { get; set; }
    public int? NextCursor { get; set; }
}

public class CreateConversationRequest
{
    public required string Title { get; set; }
    public string? Mode { get; set; }
    public List<Guid>? AiAgentIds { get; set; }
    public List<Guid>? UserIds { get; set; }
    public int? MaxTurns { get; set; }
    public long? MaxTokens { get; set; }
}

public class UpdateConversationSettingsRequest
{
    public string? Title { get; set; }
    public string? Mode { get; set; }
    public int? MaxTurns { get; set; }
    public long? MaxTokens { get; set; }
}

public class SendMessageRequest
{
    public required string Content { get; set; }
}

public class InvokeAgentRequest
{
    public List<Guid>? AgentIds { get; set; }
}

public class AddParticipantRequest
{
    public Guid? UserId { get; set; }
    public Guid? AiAgentId { get; set; }
}

#endregion
