using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Represents a connection between two activities in a process flow.
/// Supports branching workflows where decision nodes can have multiple outgoing edges.
/// </summary>
public class ActivityEdge : BaseEntity
{
    public Guid ProcessId { get; set; }
    public Guid SourceActivityId { get; set; }
    public Guid TargetActivityId { get; set; }

    /// <summary>
    /// Source handle identifier for decision nodes (e.g., "yes", "no", "default").
    /// Null for regular sequential connections.
    /// </summary>
    public string? SourceHandle { get; set; }

    /// <summary>
    /// Target handle identifier (usually null, but allows for multiple entry points).
    /// </summary>
    public string? TargetHandle { get; set; }

    /// <summary>
    /// Visual edge type: 0=Default (bezier), 1=Step, 2=SmoothStep, 3=Straight
    /// </summary>
    public EdgeType EdgeType { get; set; } = EdgeType.Default;

    /// <summary>
    /// Optional label displayed on the edge (e.g., "Yes", "No", "Approved").
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Whether the edge should be animated (flowing dots effect).
    /// </summary>
    public bool Animated { get; set; } = false;

    // Navigation properties
    public Process Process { get; set; } = null!;
    public Activity SourceActivity { get; set; } = null!;
    public Activity TargetActivity { get; set; } = null!;
}

public enum EdgeType
{
    Default,      // Bezier curve
    Step,         // Right angles
    SmoothStep,   // Rounded right angles
    Straight      // Direct line
}
