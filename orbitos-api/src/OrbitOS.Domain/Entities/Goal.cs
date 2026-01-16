using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Goal : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required GoalType GoalType { get; set; }
    public required GoalStatus Status { get; set; } = GoalStatus.Active;
    public DateTime? TimeframeStart { get; set; }
    public DateTime? TimeframeEnd { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public string? Unit { get; set; } // e.g., "%", "$", "count"

    public Guid OrganizationId { get; set; }
    public Guid? ParentId { get; set; } // For OKR hierarchy
    public Guid? OwnerId { get; set; } // Resource responsible for this goal

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Goal? Parent { get; set; }
    public Resource? Owner { get; set; }
    public ICollection<Goal> Children { get; set; } = new List<Goal>();
}

public enum GoalType
{
    Objective,
    KeyResult,
    Initiative
}

public enum GoalStatus
{
    Active,
    Achieved,
    Abandoned
}
