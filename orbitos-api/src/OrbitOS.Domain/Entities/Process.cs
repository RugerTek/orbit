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

    /// <summary>
    /// The activity where the process flow begins (connected from Start node).
    /// If null, the flow starts from the first activity by order.
    /// </summary>
    public Guid? EntryActivityId { get; set; }

    /// <summary>
    /// The activity where the process flow ends (connected to End node).
    /// If null, all activities without outgoing edges connect to End.
    /// </summary>
    public Guid? ExitActivityId { get; set; }

    /// <summary>
    /// When true, the process uses explicit flow mode where only user-defined edges are shown.
    /// When false, implicit edges are generated based on activity order.
    /// Default is true so new activities are unconnected until user manually connects them.
    /// </summary>
    public bool UseExplicitFlow { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Resource? Owner { get; set; }
    public Process? LinkedProcess { get; set; }
    public Activity? EntryActivity { get; set; }
    public Activity? ExitActivity { get; set; }
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
