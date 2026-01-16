using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Links a person Resource to a Role - this is how people get roles in the organization
/// Different from UserRole which is for platform access permissions
/// </summary>
public class RoleAssignment : BaseEntity
{
    public Guid ResourceId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPrimary { get; set; } = false;
    public decimal? AllocationPercentage { get; set; } // e.g., 50% in this role

    // Navigation properties
    public Resource Resource { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
