using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Multi-agent conversation session where humans and AI agents collaborate
/// on strategic discussions
/// </summary>
public class Conversation : BaseEntity
{
    /// <summary>
    /// Title of the conversation (e.g., 'Q4 Strategy Review')
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Conversation mode: OnDemand, Moderated, RoundRobin, Free
    /// </summary>
    public ConversationMode Mode { get; set; } = ConversationMode.OnDemand;

    /// <summary>
    /// Conversation status: Active, Paused, Archived
    /// </summary>
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;

    /// <summary>
    /// Total tokens consumed in this conversation
    /// </summary>
    public long TotalTokens { get; set; } = 0;

    /// <summary>
    /// Total cost in cents for AI usage
    /// </summary>
    public int TotalCostCents { get; set; } = 0;

    /// <summary>
    /// Total number of messages in the conversation
    /// </summary>
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// Number of AI-generated responses
    /// </summary>
    public int AiResponseCount { get; set; } = 0;

    /// <summary>
    /// Optional limit on conversation turns
    /// </summary>
    public int? MaxTurns { get; set; }

    /// <summary>
    /// Optional limit on total tokens
    /// </summary>
    public long? MaxTokens { get; set; }

    /// <summary>
    /// JSON-serialized EmergentModeSettings for Emergent mode conversations
    /// </summary>
    public string? EmergentSettingsJson { get; set; }

    /// <summary>
    /// Timestamp of the most recent message
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// When the conversation was started
    /// </summary>
    public DateTime StartedAt { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Foreign keys
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
}

/// <summary>
/// Conversation mode determines how AI agents respond
/// </summary>
public enum ConversationMode
{
    /// <summary>
    /// AI agents only respond when explicitly @mentioned
    /// </summary>
    OnDemand,

    /// <summary>
    /// AI responses require moderator approval before being shown
    /// </summary>
    Moderated,

    /// <summary>
    /// Each AI agent responds in turn after a user message
    /// </summary>
    RoundRobin,

    /// <summary>
    /// All AI agents may respond freely (can lead to high token usage)
    /// </summary>
    Free,

    /// <summary>
    /// Agents self-moderate using relevance scoring - only respond if they have valuable input.
    /// Supports multiple rounds where agents react to each other's responses.
    /// </summary>
    Emergent
}

/// <summary>
/// Conversation status
/// </summary>
public enum ConversationStatus
{
    Active,
    Paused,
    Archived
}
