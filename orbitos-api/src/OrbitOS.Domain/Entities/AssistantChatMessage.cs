using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A message in the floating AI assistant chat (not multi-agent conversations)
/// </summary>
public class AssistantChatMessage : BaseEntity
{
    /// <summary>
    /// The organization this chat belongs to
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The user who owns this chat session
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role: 'user' or 'assistant'
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Message content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Order of message in the conversation
    /// </summary>
    public int SequenceNumber { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}
