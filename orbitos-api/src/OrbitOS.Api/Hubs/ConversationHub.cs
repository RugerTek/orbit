using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Hubs;

/// <summary>
/// SignalR hub for real-time conversation messaging.
/// Clients join conversation groups and receive messages in real-time.
/// </summary>
public class ConversationHub : Hub
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<ConversationHub> _logger;

    public ConversationHub(OrbitOSDbContext dbContext, ILogger<ConversationHub> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Join a conversation to receive real-time messages
    /// </summary>
    public async Task JoinConversation(string organizationId, string conversationId)
    {
        var userId = GetCurrentUserId();

        if (Guid.TryParse(organizationId, out var orgId) &&
            Guid.TryParse(conversationId, out var convId))
        {
            var isParticipant = await _dbContext.ConversationParticipants
                .AnyAsync(p => p.ConversationId == convId &&
                               p.UserId == userId &&
                               p.IsActive &&
                               p.Conversation.OrganizationId == orgId);

            // Allow dev user or actual participants
            if (isParticipant || userId == Guid.Parse("22222222-2222-2222-2222-222222222222"))
            {
                var groupName = GetConversationGroupName(organizationId, conversationId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId, conversationId);
            }
            else
            {
                _logger.LogWarning("User {UserId} attempted to join conversation {ConversationId} without being a participant", userId, conversationId);
                throw new HubException("Not authorized to join this conversation");
            }
        }
    }

    /// <summary>
    /// Leave a conversation group
    /// </summary>
    public async Task LeaveConversation(string organizationId, string conversationId)
    {
        var groupName = GetConversationGroupName(organizationId, conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left conversation {ConversationId}", Context.ConnectionId, conversationId);
    }

    /// <summary>
    /// Join the user's notification channel for unread counts
    /// </summary>
    public async Task JoinNotifications(string organizationId)
    {
        var userId = GetCurrentUserId();
        var groupName = GetNotificationGroupName(organizationId, userId.ToString());
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} joined notifications for org {OrgId}", userId, organizationId);
    }

    /// <summary>
    /// Leave the user's notification channel
    /// </summary>
    public async Task LeaveNotifications(string organizationId)
    {
        var userId = GetCurrentUserId();
        var groupName = GetNotificationGroupName(organizationId, userId.ToString());
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Mark messages as read up to a specific message
    /// </summary>
    public async Task MarkAsRead(string conversationId, string messageId)
    {
        var userId = GetCurrentUserId();

        if (Guid.TryParse(conversationId, out var convId) &&
            Guid.TryParse(messageId, out var msgId))
        {
            var participant = await _dbContext.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == convId && p.UserId == userId && p.IsActive);

            if (participant != null)
            {
                participant.LastSeenMessageId = msgId;
                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("User {UserId} marked conversation {ConversationId} read up to {MessageId}", userId, conversationId, messageId);
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} connected to ConversationHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} disconnected from ConversationHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId) && userId != Guid.Empty)
            return userId;

        // For local development without auth
        return Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    public static string GetConversationGroupName(string organizationId, string conversationId)
        => $"conversation:{organizationId}:{conversationId}";

    public static string GetNotificationGroupName(string organizationId, string userId)
        => $"notifications:{organizationId}:{userId}";
}

/// <summary>
/// DTOs for SignalR messages
/// </summary>
public record NewMessageNotification(
    Guid ConversationId,
    Guid MessageId,
    string SenderType,
    string SenderName,
    string? SenderAvatarColor,
    string ContentPreview,
    int SequenceNumber,
    DateTime CreatedAt
);

public record ConversationUpdatedNotification(
    Guid ConversationId,
    string Title,
    int MessageCount,
    DateTime? LastMessageAt
);

public record UnreadCountNotification(
    Guid ConversationId,
    int UnreadCount
);
