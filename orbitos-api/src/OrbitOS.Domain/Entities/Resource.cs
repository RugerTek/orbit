using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Resource : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required ResourceStatus Status { get; set; } = ResourceStatus.Active;
    public string? Metadata { get; set; } // JSON for type-specific data
    public Guid OrganizationId { get; set; }
    public Guid ResourceSubtypeId { get; set; }

    // For person resources - optional link to platform user
    public Guid? LinkedUserId { get; set; }

    // Org Chart hierarchy - for person resources
    public Guid? ReportsToResourceId { get; set; }
    public bool IsVacant { get; set; } = false;
    public string? VacantPositionTitle { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ResourceSubtype ResourceSubtype { get; set; } = null!;
    public User? LinkedUser { get; set; }
    public Resource? ReportsToResource { get; set; }
    public ICollection<Resource> DirectReports { get; set; } = new List<Resource>();
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = new List<RoleAssignment>();
    public ICollection<FunctionCapability> FunctionCapabilities { get; set; } = new List<FunctionCapability>();
    public ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
    public ICollection<Process> OwnedProcesses { get; set; } = new List<Process>();
    public ICollection<Goal> OwnedGoals { get; set; } = new List<Goal>();
}

public enum ResourceStatus
{
    Planned,
    Active,
    Deprecated
}
