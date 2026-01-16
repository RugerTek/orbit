using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Role : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    public string? Department { get; set; }
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<RoleFunction> RoleFunctions { get; set; } = new List<RoleFunction>();
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = new List<RoleAssignment>();
}
