using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Represents an AI-proposed data modification awaiting user approval.
/// Enables a confirmation workflow where AI agents suggest changes that users must explicitly approve.
/// </summary>
public class PendingAction : BaseEntity
{
    public required Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// Optional conversation context - null for standalone AI chat
    /// </summary>
    public Guid? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Optional message that contains this proposed action
    /// </summary>
    public Guid? MessageId { get; set; }
    public Message? Message { get; set; }

    /// <summary>
    /// Optional AI agent that proposed this action (null for system-generated)
    /// </summary>
    public Guid? AgentId { get; set; }
    public AiAgent? Agent { get; set; }

    /// <summary>
    /// Type of CRUD operation: Create, Update, Delete
    /// </summary>
    public required ActionType ActionType { get; set; }

    /// <summary>
    /// Target entity type name (e.g., "Partner", "Function", "Process")
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// ID of existing entity for Update/Delete operations. Null for Create.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Display name of the entity being modified (for UI display)
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// JSON object containing the proposed entity data
    /// </summary>
    public required string ProposedDataJson { get; set; }

    /// <summary>
    /// Original entity data before proposed changes (for Update operations)
    /// </summary>
    public string? PreviousDataJson { get; set; }

    /// <summary>
    /// AI's explanation for why this action should be taken
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Current status of the pending action
    /// </summary>
    public required PendingActionStatus Status { get; set; } = PendingActionStatus.Pending;

    /// <summary>
    /// User's modifications to the proposed data before approval
    /// </summary>
    public string? UserModificationsJson { get; set; }

    /// <summary>
    /// The actual data that was executed (proposedData + userModifications)
    /// </summary>
    public string? FinalDataJson { get; set; }

    /// <summary>
    /// Result of the execution attempt including any errors
    /// </summary>
    public string? ExecutionResultJson { get; set; }

    /// <summary>
    /// ID of the created/updated entity after successful execution
    /// </summary>
    public Guid? ResultEntityId { get; set; }

    /// <summary>
    /// User who approved or rejected the action
    /// </summary>
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }

    /// <summary>
    /// Timestamp when the action was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// User's reason for rejecting the action
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Timestamp when the action was executed
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Optional expiration time for pending actions
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

public enum ActionType
{
    Create,
    Update,
    Delete
}

public enum PendingActionStatus
{
    /// <summary>Action proposed, awaiting user review</summary>
    Pending,

    /// <summary>User approved the action as-is</summary>
    Approved,

    /// <summary>User rejected the action</summary>
    Rejected,

    /// <summary>User approved with modifications</summary>
    Modified,

    /// <summary>Action was successfully executed</summary>
    Executed,

    /// <summary>Action execution failed</summary>
    Failed,

    /// <summary>Action expired before review</summary>
    Expired
}
