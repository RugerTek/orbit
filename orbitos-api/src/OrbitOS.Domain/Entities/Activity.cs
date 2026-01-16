using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Activity : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required int Order { get; set; }
    public required ActivityType ActivityType { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? Instructions { get; set; } // SOP content (markdown)

    public Guid ProcessId { get; set; }
    public Guid? FunctionId { get; set; } // Linked function
    public Guid? AssignedResourceId { get; set; } // Who/what does this
    public Guid? LinkedProcessId { get; set; } // Subprocess linking - drill down into this process

    // Vue Flow canvas position
    public double PositionX { get; set; } = 0;
    public double PositionY { get; set; } = 0;

    // Navigation properties
    public Process Process { get; set; } = null!;
    public ICollection<ActivityEdge> OutgoingEdges { get; set; } = new List<ActivityEdge>();
    public ICollection<ActivityEdge> IncomingEdges { get; set; } = new List<ActivityEdge>();
    public Function? Function { get; set; }
    public Resource? AssignedResource { get; set; }
    public Process? LinkedProcess { get; set; } // Subprocess navigation
}

public enum ActivityType
{
    Manual,
    Automated,
    Hybrid,
    Decision,
    Handoff
}
