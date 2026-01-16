using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Assigns a SystemRole to a User within an Organization.
/// Controls what platform permissions the user has for that organization.
/// </summary>
public class UserSystemRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SystemRoleId { get; set; }
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public SystemRole SystemRole { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
