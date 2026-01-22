using OrbitOS.Domain.Entities;

namespace OrbitOS.Api.Controllers.Operations;

#region Resource DTOs

public class ResourceSubtypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ResourceType ResourceType { get; set; }
    public string? Icon { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ResourceCount { get; set; }
}

public class CreateResourceSubtypeRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required ResourceType ResourceType { get; set; }
    public string? Icon { get; set; }
}

public class ResourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ResourceStatus Status { get; set; }
    public string? Metadata { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ResourceSubtypeId { get; set; }
    public string ResourceSubtypeName { get; set; } = "";
    public ResourceType ResourceType { get; set; }
    public Guid? LinkedUserId { get; set; }
    public string? LinkedUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int RoleAssignmentCount { get; set; }
    public int FunctionCapabilityCount { get; set; }
}

public class CreateResourceRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ResourceStatus Status { get; set; } = ResourceStatus.Active;
    public string? Metadata { get; set; }
    public required Guid ResourceSubtypeId { get; set; }
    public Guid? LinkedUserId { get; set; }
}

public class UpdateResourceRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ResourceStatus Status { get; set; }
    public string? Metadata { get; set; }
    public Guid? LinkedUserId { get; set; }
}

#endregion

#region Process DTOs

public class ProcessDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Purpose { get; set; }
    public string? Description { get; set; }
    public string? Trigger { get; set; }
    public string? Output { get; set; }
    public ProcessFrequency? Frequency { get; set; }
    public ProcessStatus Status { get; set; }
    public ProcessStateType StateType { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public Guid? LinkedProcessId { get; set; }
    public string? LinkedProcessName { get; set; }
    /// <summary>
    /// The activity where the flow starts (connected from Start node).
    /// </summary>
    public Guid? EntryActivityId { get; set; }
    /// <summary>
    /// The activity where the flow ends (connected to End node).
    /// </summary>
    public Guid? ExitActivityId { get; set; }
    /// <summary>
    /// When true, the process uses explicit flow mode (user-defined edges only).
    /// When false, implicit edges are generated based on activity order.
    /// </summary>
    public bool UseExplicitFlow { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ActivityCount { get; set; }
    public List<ActivityDto> Activities { get; set; } = new();
    public List<ActivityEdgeDto> Edges { get; set; } = new();
}

public class CreateProcessRequest
{
    public required string Name { get; set; }
    public string? Purpose { get; set; }
    public string? Description { get; set; }
    public string? Trigger { get; set; }
    public string? Output { get; set; }
    public ProcessFrequency? Frequency { get; set; }
    public ProcessStatus Status { get; set; } = ProcessStatus.Draft;
    public ProcessStateType StateType { get; set; } = ProcessStateType.Current;
    public Guid? OwnerId { get; set; }
    public Guid? LinkedProcessId { get; set; }
}

public class UpdateProcessRequest
{
    public required string Name { get; set; }
    public string? Purpose { get; set; }
    public string? Description { get; set; }
    public string? Trigger { get; set; }
    public string? Output { get; set; }
    public ProcessFrequency? Frequency { get; set; }
    public ProcessStatus Status { get; set; }
    public ProcessStateType StateType { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? LinkedProcessId { get; set; }
    /// <summary>
    /// The activity where the flow starts (connected from Start node).
    /// </summary>
    public Guid? EntryActivityId { get; set; }
    /// <summary>
    /// The activity where the flow ends (connected to End node).
    /// </summary>
    public Guid? ExitActivityId { get; set; }
}

/// <summary>
/// Request to update the flow entry and/or exit points.
/// Used when connecting from Start node or to End node in the flow editor.
/// </summary>
public class UpdateFlowEndpointsRequest
{
    /// <summary>
    /// The activity to connect from the Start node.
    /// </summary>
    public Guid? EntryActivityId { get; set; }
    /// <summary>
    /// The activity to connect to the End node.
    /// </summary>
    public Guid? ExitActivityId { get; set; }
    /// <summary>
    /// Set to true to clear the entry activity (disconnect from Start).
    /// </summary>
    public bool ClearEntry { get; set; }
    /// <summary>
    /// Set to true to clear the exit activity (disconnect from End).
    /// </summary>
    public bool ClearExit { get; set; }
}

#endregion

#region Activity DTOs

public class ActivityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int Order { get; set; }
    public ActivityType ActivityType { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? Instructions { get; set; }
    public Guid ProcessId { get; set; }
    public Guid? FunctionId { get; set; }
    public string? FunctionName { get; set; }
    public Guid? AssignedResourceId { get; set; }
    public string? AssignedResourceName { get; set; }
    public Guid? LinkedProcessId { get; set; }
    public string? LinkedProcessName { get; set; }
    /// <summary>
    /// Full subprocess details when LinkedProcessId is set.
    /// Enables the activity to act as a "portal" showing subprocess information.
    /// </summary>
    public LinkedProcessSummaryDto? LinkedProcess { get; set; }
    public DateTime CreatedAt { get; set; }
    // Vue Flow canvas position
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}

/// <summary>
/// Summary of a linked subprocess for display in activity details.
/// </summary>
public class LinkedProcessSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Purpose { get; set; }
    public string? Trigger { get; set; }
    public string? Output { get; set; }
    public int ActivityCount { get; set; }
}

public class CreateActivityRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int Order { get; set; }
    public required ActivityType ActivityType { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? Instructions { get; set; }
    public Guid? FunctionId { get; set; }
    public Guid? AssignedResourceId { get; set; }
    public Guid? LinkedProcessId { get; set; }
    // Vue Flow canvas position
    public double PositionX { get; set; } = 0;
    public double PositionY { get; set; } = 0;
}

public class UpdateActivityRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int Order { get; set; }
    public required ActivityType ActivityType { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? Instructions { get; set; }
    public Guid? FunctionId { get; set; }
    public Guid? AssignedResourceId { get; set; }
    public Guid? LinkedProcessId { get; set; }
    // Vue Flow canvas position
    public double PositionX { get; set; } = 0;
    public double PositionY { get; set; } = 0;
}

public class ReorderActivitiesRequest
{
    public required List<Guid> ActivityIds { get; set; }
}

/// <summary>
/// Request to update multiple activity positions at once (Vue Flow canvas).
/// </summary>
public class UpdateActivityPositionsRequest
{
    public required List<ActivityPositionUpdate> Positions { get; set; }
}

public class ActivityPositionUpdate
{
    public required Guid ActivityId { get; set; }
    public required double PositionX { get; set; }
    public required double PositionY { get; set; }
}

#endregion

#region Activity Edge DTOs (Vue Flow connections)

public class ActivityEdgeDto
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public Guid SourceActivityId { get; set; }
    public Guid TargetActivityId { get; set; }
    public string? SourceHandle { get; set; }
    public string? TargetHandle { get; set; }
    public EdgeType EdgeType { get; set; }
    public string? Label { get; set; }
    public bool Animated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateActivityEdgeRequest
{
    public required Guid SourceActivityId { get; set; }
    public required Guid TargetActivityId { get; set; }
    public string? SourceHandle { get; set; }
    public string? TargetHandle { get; set; }
    public EdgeType EdgeType { get; set; } = EdgeType.Default;
    public string? Label { get; set; }
    public bool Animated { get; set; } = false;
}

public class UpdateActivityEdgeRequest
{
    public string? SourceHandle { get; set; }
    public string? TargetHandle { get; set; }
    public EdgeType EdgeType { get; set; }
    public string? Label { get; set; }
    public bool Animated { get; set; }
}

#endregion

#region Canvas DTOs

public class CanvasDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public CanvasType CanvasType { get; set; }
    public CanvasScopeType ScopeType { get; set; }
    public CanvasStatus Status { get; set; }
    public int Version { get; set; }
    public string? VersionNote { get; set; }
    public string? AiSummary { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ParentCanvasId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public Guid? SegmentId { get; set; }
    public string? SegmentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CanvasBlockDto> Blocks { get; set; } = new();
}

public class CreateCanvasRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public CanvasType CanvasType { get; set; } = CanvasType.BusinessModel;
    public CanvasScopeType ScopeType { get; set; } = CanvasScopeType.Organization;
    public CanvasStatus Status { get; set; } = CanvasStatus.Draft;
    public Guid? ParentCanvasId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }
}

public class UpdateCanvasRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public CanvasType CanvasType { get; set; }
    public CanvasScopeType ScopeType { get; set; }
    public CanvasStatus Status { get; set; }
    public Guid? ParentCanvasId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }
}

public class CanvasBlockDto
{
    public Guid Id { get; set; }
    public CanvasBlockType BlockType { get; set; }
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public Guid CanvasId { get; set; }
    /// <summary>
    /// Linked entity references for this block (Roles, Processes, etc.)
    /// </summary>
    public List<BlockReferenceDto> References { get; set; } = new();
}

public class UpdateCanvasBlockRequest
{
    public required CanvasBlockType BlockType { get; set; }
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
}

#endregion

#region Goal DTOs

public class GoalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public GoalType GoalType { get; set; }
    public GoalStatus Status { get; set; }
    public DateTime? TimeframeStart { get; set; }
    public DateTime? TimeframeEnd { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public string? Unit { get; set; }
    public decimal? Progress { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<GoalDto> Children { get; set; } = new();
}

public class CreateGoalRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required GoalType GoalType { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public DateTime? TimeframeStart { get; set; }
    public DateTime? TimeframeEnd { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public string? Unit { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? OwnerId { get; set; }
}

public class UpdateGoalRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required GoalType GoalType { get; set; }
    public GoalStatus Status { get; set; }
    public DateTime? TimeframeStart { get; set; }
    public DateTime? TimeframeEnd { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public string? Unit { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? OwnerId { get; set; }
}

#endregion

#region Role Assignment DTOs

public class RoleAssignmentDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = "";
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public decimal? AllocationPercentage { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRoleAssignmentRequest
{
    public required Guid ResourceId { get; set; }
    public required Guid RoleId { get; set; }
    public decimal? AllocationPercentage { get; set; } = 100;
    public bool IsPrimary { get; set; } = false;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

#endregion

#region Function Capability DTOs

public class FunctionCapabilityDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = "";
    public Guid FunctionId { get; set; }
    public string FunctionName { get; set; } = "";
    public CapabilityLevel Level { get; set; }
    public DateTime? CertifiedDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFunctionCapabilityRequest
{
    public required Guid ResourceId { get; set; }
    public required Guid FunctionId { get; set; }
    public CapabilityLevel Level { get; set; } = CapabilityLevel.Capable;
    public DateTime? CertifiedDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateFunctionCapabilityRequest
{
    public CapabilityLevel Level { get; set; }
    public DateTime? CertifiedDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Operations Roles DTOs

public class OpsRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Department { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AssignmentCount { get; set; }
    public int FunctionCount { get; set; }
}

public class CreateOpsRoleRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Department { get; set; }
}

public class UpdateOpsRoleRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Department { get; set; }
}

#endregion

#region Operations Functions DTOs

public class OpsFunctionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Category { get; set; }
    public FunctionStatus Status { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CapabilityCount { get; set; }
    public int RoleCount { get; set; }
}

public class CreateOpsFunctionRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Category { get; set; }
}

public class UpdateOpsFunctionRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Category { get; set; }
}

public class BulkCreateFunctionsRequest
{
    public required List<CreateOpsFunctionRequest> Functions { get; set; }
}

public class BulkCreateFunctionsResponse
{
    public List<OpsFunctionDto> Created { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

#endregion

#region Partner DTOs

public class PartnerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public PartnerType Type { get; set; }
    public PartnerStatus Status { get; set; }
    public StrategicValue StrategicValue { get; set; }
    public int? RelationshipStrength { get; set; }
    public string? Website { get; set; }
    public string? ContactJson { get; set; }
    public string? ContractJson { get; set; }
    public string? ServicesProvidedJson { get; set; }
    public string? ServicesReceivedJson { get; set; }
    public string? CostJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ChannelCount { get; set; }
    public int CanvasReferenceCount { get; set; }
}

public class CreatePartnerRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required PartnerType Type { get; set; }
    public PartnerStatus Status { get; set; } = PartnerStatus.Active;
    public StrategicValue StrategicValue { get; set; } = StrategicValue.Medium;
    public int? RelationshipStrength { get; set; }
    public string? Website { get; set; }
    public string? ContactJson { get; set; }
    public string? ContractJson { get; set; }
    public string? ServicesProvidedJson { get; set; }
    public string? ServicesReceivedJson { get; set; }
    public string? CostJson { get; set; }
    public string? TagsJson { get; set; }
}

public class UpdatePartnerRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required PartnerType Type { get; set; }
    public PartnerStatus Status { get; set; }
    public StrategicValue StrategicValue { get; set; }
    public int? RelationshipStrength { get; set; }
    public string? Website { get; set; }
    public string? ContactJson { get; set; }
    public string? ContractJson { get; set; }
    public string? ServicesProvidedJson { get; set; }
    public string? ServicesReceivedJson { get; set; }
    public string? CostJson { get; set; }
    public string? TagsJson { get; set; }
}

#endregion

#region Channel DTOs

public class ChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public ChannelType Type { get; set; }
    public ChannelCategory Category { get; set; }
    public ChannelStatus Status { get; set; }
    public ChannelOwnership Ownership { get; set; }
    public string? PhasesJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? CostJson { get; set; }
    public string? IntegrationJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CanvasReferenceCount { get; set; }
}

public class CreateChannelRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required ChannelType Type { get; set; }
    public required ChannelCategory Category { get; set; }
    public ChannelStatus Status { get; set; } = ChannelStatus.Active;
    public ChannelOwnership Ownership { get; set; } = ChannelOwnership.Owned;
    public string? PhasesJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? CostJson { get; set; }
    public string? IntegrationJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? PartnerId { get; set; }
}

public class UpdateChannelRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required ChannelType Type { get; set; }
    public required ChannelCategory Category { get; set; }
    public ChannelStatus Status { get; set; }
    public ChannelOwnership Ownership { get; set; }
    public string? PhasesJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? CostJson { get; set; }
    public string? IntegrationJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? PartnerId { get; set; }
}

#endregion

#region ValueProposition DTOs

public class ValuePropositionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string Headline { get; set; } = "";
    public string? Description { get; set; }
    public ValuePropositionStatus Status { get; set; }
    public string? CustomerJobsJson { get; set; }
    public string? PainsJson { get; set; }
    public string? GainsJson { get; set; }
    public string? PainRelieversJson { get; set; }
    public string? GainCreatorsJson { get; set; }
    public string? ProductsServicesJson { get; set; }
    public string? DifferentiatorsJson { get; set; }
    public string? ValidationJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public Guid? SegmentId { get; set; }
    public string? SegmentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CanvasReferenceCount { get; set; }
}

public class CreateValuePropositionRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public required string Headline { get; set; }
    public string? Description { get; set; }
    public ValuePropositionStatus Status { get; set; } = ValuePropositionStatus.Draft;
    public string? CustomerJobsJson { get; set; }
    public string? PainsJson { get; set; }
    public string? GainsJson { get; set; }
    public string? PainRelieversJson { get; set; }
    public string? GainCreatorsJson { get; set; }
    public string? ProductsServicesJson { get; set; }
    public string? DifferentiatorsJson { get; set; }
    public string? ValidationJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }
}

public class UpdateValuePropositionRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public required string Headline { get; set; }
    public string? Description { get; set; }
    public ValuePropositionStatus Status { get; set; }
    public string? CustomerJobsJson { get; set; }
    public string? PainsJson { get; set; }
    public string? GainsJson { get; set; }
    public string? PainRelieversJson { get; set; }
    public string? GainCreatorsJson { get; set; }
    public string? ProductsServicesJson { get; set; }
    public string? DifferentiatorsJson { get; set; }
    public string? ValidationJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }
}

#endregion

#region CustomerRelationship DTOs

public class CustomerRelationshipDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public CustomerRelationshipType Type { get; set; }
    public CustomerRelationshipStatus Status { get; set; }
    public string? PurposeJson { get; set; }
    public string? TouchpointsJson { get; set; }
    public string? LifecycleJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? CostJson { get; set; }
    public string? ExpectationsJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? SegmentId { get; set; }
    public string? SegmentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CanvasReferenceCount { get; set; }
}

public class CreateCustomerRelationshipRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required CustomerRelationshipType Type { get; set; }
    public CustomerRelationshipStatus Status { get; set; } = CustomerRelationshipStatus.Active;
    public string? PurposeJson { get; set; }
    public string? TouchpointsJson { get; set; }
    public string? LifecycleJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? CostJson { get; set; }
    public string? ExpectationsJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? SegmentId { get; set; }
}

public class UpdateCustomerRelationshipRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required CustomerRelationshipType Type { get; set; }
    public CustomerRelationshipStatus Status { get; set; }
    public string? PurposeJson { get; set; }
    public string? TouchpointsJson { get; set; }
    public string? LifecycleJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? CostJson { get; set; }
    public string? ExpectationsJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? SegmentId { get; set; }
}

#endregion

#region RevenueStream DTOs

public class RevenueStreamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public RevenueStreamType Type { get; set; }
    public RevenueStreamStatus Status { get; set; }
    public PricingMechanism PricingMechanism { get; set; }
    public string? PricingJson { get; set; }
    public string? RevenueJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? WillingnessToPayJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public Guid? SegmentId { get; set; }
    public string? SegmentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CanvasReferenceCount { get; set; }
}

public class CreateRevenueStreamRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required RevenueStreamType Type { get; set; }
    public RevenueStreamStatus Status { get; set; } = RevenueStreamStatus.Active;
    public PricingMechanism PricingMechanism { get; set; } = PricingMechanism.Fixed;
    public string? PricingJson { get; set; }
    public string? RevenueJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? WillingnessToPayJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }
}

public class UpdateRevenueStreamRequest
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required RevenueStreamType Type { get; set; }
    public RevenueStreamStatus Status { get; set; }
    public PricingMechanism PricingMechanism { get; set; }
    public string? PricingJson { get; set; }
    public string? RevenueJson { get; set; }
    public string? MetricsJson { get; set; }
    public string? WillingnessToPayJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }
}

#endregion

#region Org Chart DTOs

/// <summary>
/// Resource with org chart hierarchy information
/// </summary>
public class OrgChartResourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ResourceStatus Status { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ResourceSubtypeId { get; set; }
    public string ResourceSubtypeName { get; set; } = "";
    public ResourceType ResourceType { get; set; }
    public Guid? LinkedUserId { get; set; }
    public string? LinkedUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Org Chart hierarchy fields
    public Guid? ReportsToResourceId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsVacant { get; set; }
    public string? VacantPositionTitle { get; set; }

    // Computed metrics
    public int DirectReportsCount { get; set; }
    public int IndirectReportsCount { get; set; }
    public int ManagementDepth { get; set; }

    // Children for tree view (populated when building tree structure)
    public List<OrgChartResourceDto> DirectReports { get; set; } = new();
}

/// <summary>
/// Full org chart tree structure for visualization
/// </summary>
public class OrgChartTreeDto
{
    /// <summary>Top-level resources with no manager (typically CEO/founders)</summary>
    public List<OrgChartResourceDto> RootNodes { get; set; } = new();
    /// <summary>Total count of people in the organization (excluding vacancies)</summary>
    public int TotalPeople { get; set; }
    /// <summary>Total count of unfilled positions</summary>
    public int TotalVacancies { get; set; }
    /// <summary>Maximum depth of the org chart hierarchy</summary>
    public int MaxDepth { get; set; }
    /// <summary>Count of people at each level (depth -> count)</summary>
    public Dictionary<int, int> PeopleByDepth { get; set; } = new();
}

/// <summary>
/// Summary metrics for organizational health and span of control
/// </summary>
public class OrgChartMetricsDto
{
    public int TotalPeople { get; set; }
    public int TotalVacancies { get; set; }
    public int MaxDepth { get; set; }
    /// <summary>Average number of direct reports per manager</summary>
    public decimal AverageSpanOfControl { get; set; }
    /// <summary>Breakdown of span of control for each manager</summary>
    public List<SpanOfControlEntry> SpanOfControlByManager { get; set; } = new();
}

public class SpanOfControlEntry
{
    public Guid ManagerId { get; set; }
    public string ManagerName { get; set; } = "";
    public int DirectReports { get; set; }
    public int IndirectReports { get; set; }
    public int Depth { get; set; }
}

/// <summary>
/// Request to update a person's reporting relationship
/// </summary>
public class UpdateReportingRequest
{
    /// <summary>The resource (person) this person should report to. Null to remove reporting relationship.</summary>
    public Guid? ReportsToResourceId { get; set; }
}

/// <summary>
/// Request to create a vacant position in the org chart
/// </summary>
public class CreateVacantPositionRequest
{
    /// <summary>The job title or role name for the vacant position</summary>
    public required string VacantPositionTitle { get; set; }
    /// <summary>The manager this position should report to</summary>
    public Guid? ReportsToResourceId { get; set; }
    /// <summary>The resource subtype (should be a Person type)</summary>
    public required Guid ResourceSubtypeId { get; set; }
    /// <summary>Optional description of the position</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to fill a vacant position with person details
/// </summary>
public class FillVacancyRequest
{
    /// <summary>Name of the person filling the position</summary>
    public required string Name { get; set; }
    /// <summary>Optional description/bio</summary>
    public string? Description { get; set; }
    /// <summary>Optional link to a platform user</summary>
    public Guid? LinkedUserId { get; set; }
}

#endregion

#region BlockReference DTOs

public class BlockReferenceDto
{
    public Guid Id { get; set; }
    public ReferenceEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string? EntityName { get; set; }
    public ReferenceRole Role { get; set; }
    public ReferenceLinkType LinkType { get; set; }
    public string? ContextNote { get; set; }
    public int SortOrder { get; set; }
    public bool IsHighlighted { get; set; }
    public string? MetricsJson { get; set; }
    public string? TagsJson { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CanvasBlockId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBlockReferenceRequest
{
    public required ReferenceEntityType EntityType { get; set; }
    public required Guid EntityId { get; set; }
    public ReferenceRole Role { get; set; } = ReferenceRole.Primary;
    public ReferenceLinkType LinkType { get; set; } = ReferenceLinkType.Linked;
    public string? ContextNote { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsHighlighted { get; set; } = false;
    public string? MetricsJson { get; set; }
    public string? TagsJson { get; set; }
}

public class UpdateBlockReferenceRequest
{
    public ReferenceRole Role { get; set; }
    public ReferenceLinkType LinkType { get; set; }
    public string? ContextNote { get; set; }
    public int SortOrder { get; set; }
    public bool IsHighlighted { get; set; }
    public string? MetricsJson { get; set; }
    public string? TagsJson { get; set; }
}

/// <summary>
/// Enriched block reference with full entity details for AI consumption and UI expansion
/// </summary>
public class EnrichedBlockReferenceDto : BlockReferenceDto
{
    /// <summary>
    /// For Role references: assigned people with their allocation
    /// </summary>
    public List<RoleAssignmentSummaryDto>? AssignedPeople { get; set; }

    /// <summary>
    /// For Role references: coverage status (Covered, AtRisk, Uncovered)
    /// </summary>
    public string? CoverageStatus { get; set; }

    /// <summary>
    /// For Role references: functions this role is responsible for
    /// </summary>
    public List<FunctionSummaryDto>? Functions { get; set; }

    /// <summary>
    /// For Process references: activities within the process
    /// </summary>
    public List<ActivitySummaryDto>? Activities { get; set; }

    /// <summary>
    /// For Process references: process status
    /// </summary>
    public ProcessStatus? ProcessStatus { get; set; }
}

public class RoleAssignmentSummaryDto
{
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = "";
    public decimal? AllocationPercentage { get; set; }
    public bool IsPrimary { get; set; }
    public ResourceStatus Status { get; set; }
}

public class FunctionSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
}

public class ActivitySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public ActivityType ActivityType { get; set; }
    public int Order { get; set; }
}

#endregion

#region RoleFunction DTOs

public class RoleFunctionDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public string? RoleDepartment { get; set; }
    public Guid FunctionId { get; set; }
    public string FunctionName { get; set; } = "";
    public string? FunctionCategory { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignFunctionToRoleRequest
{
    public required Guid FunctionId { get; set; }
}

public class AssignRoleToFunctionRequest
{
    public required Guid RoleId { get; set; }
}

#endregion
