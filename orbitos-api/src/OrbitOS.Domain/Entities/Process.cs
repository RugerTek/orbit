using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Process : BaseEntity
{
    public required string Name { get; set; }
    public string? Purpose { get; set; }
    public string? Description { get; set; }
    public string? Trigger { get; set; }
    public string? Output { get; set; }
    public ProcessFrequency? Frequency { get; set; }
    public required ProcessStatus Status { get; set; } = ProcessStatus.Draft;
    public required ProcessStateType StateType { get; set; } = ProcessStateType.Current;

    public Guid OrganizationId { get; set; }
    public Guid? OwnerId { get; set; } // Resource who owns this process
    public Guid? LinkedProcessId { get; set; } // Links current ↔ target version

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Resource? Owner { get; set; }
    public Process? LinkedProcess { get; set; }
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<ActivityEdge> Edges { get; set; } = new List<ActivityEdge>();
}

public enum ProcessStatus
{
    Draft,
    Active,
    Deprecated
}

public enum ProcessStateType
{
    Current,
    Target
}

public enum ProcessFrequency
{
    Daily,
    Weekly,
    Monthly,
    OnDemand,
    Continuous
}
