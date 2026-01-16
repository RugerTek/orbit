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
    public DateTime CreatedAt { get; set; }
    // Vue Flow canvas position
    public double PositionX { get; set; }
    public double PositionY { get; set; }
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
    public string? Description { get; set; }
    public CanvasType CanvasType { get; set; }
    public CanvasStatus Status { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CanvasBlockDto> Blocks { get; set; } = new();
}

public class CreateCanvasRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public CanvasType CanvasType { get; set; } = CanvasType.BusinessModel;
    public CanvasStatus Status { get; set; } = CanvasStatus.Draft;
}

public class UpdateCanvasRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public CanvasType CanvasType { get; set; }
    public CanvasStatus Status { get; set; }
}

public class CanvasBlockDto
{
    public Guid Id { get; set; }
    public CanvasBlockType BlockType { get; set; }
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public Guid CanvasId { get; set; }
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
