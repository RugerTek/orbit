using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class User : BaseEntity
{
    public string? AzureAdObjectId { get; set; }
    public string? GoogleId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<OrganizationMembership> OrganizationMemberships { get; set; } = new List<OrganizationMembership>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>(); // Deprecated - use UserSystemRoles
    public ICollection<UserSystemRole> UserSystemRoles { get; set; } = new List<UserSystemRole>();
    public ICollection<Resource> LinkedResources { get; set; } = new List<Resource>(); // Person resources linked to this user
}
