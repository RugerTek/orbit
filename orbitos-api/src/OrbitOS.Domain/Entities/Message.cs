using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A message in a multi-agent conversation, sent by either a human user or an AI agent
/// </summary>
public class Message : BaseEntity
{
    /// <summary>
    /// Type of sender: User or Ai
    /// </summary>
    public SenderType SenderType { get; set; }

    /// <summary>
    /// User ID if sender is human (null for AI messages)
    /// </summary>
    public Guid? SenderUserId { get; set; }

    /// <summary>
    /// AI Agent ID if sender is AI (null for human messages)
    /// </summary>
    public Guid? SenderAiAgentId { get; set; }

    /// <summary>
    /// Message content (supports markdown)
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Pre-rendered HTML for rich content (metrics cards, charts)
    /// </summary>
    public string? ContentHtml { get; set; }

    /// <summary>
    /// JSON array of AI agent IDs that were @mentioned in this message
    /// </summary>
    public string? MentionedAgentIdsJson { get; set; }

    /// <summary>
    /// JSON array of referenced items (canvases, processes, etc.) with type and ID
    /// </summary>
    public string? ReferencedItemsJson { get; set; }

    /// <summary>
    /// JSON array of data sources used by AI to generate this response
    /// </summary>
    public string? SourcesJson { get; set; }

    /// <summary>
    /// Tokens consumed for AI response (null for human messages)
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// AI response time in milliseconds (null for human messages)
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Cost in cents for this AI response (null for human messages)
    /// </summary>
    public int? CostCents { get; set; }

    /// <summary>
    /// Model ID used for AI response (e.g., 'claude-sonnet-4-20250514')
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Message status: Pending, Sent, Streaming, Failed
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    /// <summary>
    /// Parent message ID for threaded replies
    /// </summary>
    public Guid? ParentMessageId { get; set; }

    /// <summary>
    /// Whether this message is part of A2A inner dialogue (not the main response)
    /// </summary>
    public bool IsInnerDialogue { get; set; } = false;

    /// <summary>
    /// JSON array of inner dialogue steps showing the AI's thought process
    /// Structure: [{ stepNumber, type, title, description, agentId?, agentName?, query?, response? }]
    /// </summary>
    public string? InnerDialogueJson { get; set; }

    /// <summary>
    /// Type of inner dialogue step (for filtering/display)
    /// </summary>
    public InnerDialogueType? InnerDialogueType { get; set; }

    /// <summary>
    /// Order of message within the conversation
    /// </summary>
    public int SequenceNumber { get; set; }

    // Foreign keys
    public Guid ConversationId { get; set; }

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User? SenderUser { get; set; }
    public AiAgent? SenderAiAgent { get; set; }
    public Message? ParentMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
}

/// <summary>
/// Type of message sender
/// </summary>
public enum SenderType
{
    User,
    Ai
}

/// <summary>
/// Message status
/// </summary>
public enum MessageStatus
{
    Pending,
    Sent,
    Streaming,
    Failed
}

/// <summary>
/// Type of inner dialogue step for A2A communication
/// </summary>
public enum InnerDialogueType
{
    /// <summary>Routing query to specialists</summary>
    Routing,
    /// <summary>Consulting a specialist agent</summary>
    Consulting,
    /// <summary>Agent asking another agent a question</summary>
    AgentToAgent,
    /// <summary>Synthesizing responses from multiple agents</summary>
    Synthesis,
    /// <summary>General reasoning/thinking step</summary>
    Reasoning
}
