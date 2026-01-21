using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Junction entity linking users and AI agents to conversations they participate in
/// </summary>
public class ConversationParticipant : BaseEntity
{
    /// <summary>
    /// Type of participant: User or Ai
    /// </summary>
    public ParticipantType ParticipantType { get; set; }

    /// <summary>
    /// User ID if participant is human (null for AI agents)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// AI Agent ID if participant is AI (null for humans)
    /// </summary>
    public Guid? AiAgentId { get; set; }

    /// <summary>
    /// Role in conversation: Owner, Moderator, Participant
    /// </summary>
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;

    /// <summary>
    /// When the participant joined the conversation
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// When the participant left the conversation (null if still active)
    /// </summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>
    /// Whether the participant is currently active in the conversation
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last message the participant has seen (for unread count)
    /// </summary>
    public Guid? LastSeenMessageId { get; set; }

    /// <summary>
    /// Whether the participant receives notifications for this conversation
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    // Foreign keys
    public Guid ConversationId { get; set; }

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User? User { get; set; }
    public AiAgent? AiAgent { get; set; }
    public Message? LastSeenMessage { get; set; }
}

/// <summary>
/// Type of participant
/// </summary>
public enum ParticipantType
{
    User,
    Ai
}

/// <summary>
/// Participant role in conversation
/// </summary>
public enum ParticipantRole
{
    Owner,
    Moderator,
    Participant
}
