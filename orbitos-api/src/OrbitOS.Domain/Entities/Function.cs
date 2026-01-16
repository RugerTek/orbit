using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Function : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Category { get; set; }
    public FunctionComplexity? Complexity { get; set; }
    public bool RequiresApproval { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? Instructions { get; set; } // SOP markdown
    public required FunctionStatus Status { get; set; } = FunctionStatus.Active;
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<RoleFunction> RoleFunctions { get; set; } = new List<RoleFunction>();
    public ICollection<FunctionCapability> FunctionCapabilities { get; set; } = new List<FunctionCapability>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

public enum FunctionComplexity
{
    Simple,
    Moderate,
    Complex
}

public enum FunctionStatus
{
    Active,
    Deprecated
}
