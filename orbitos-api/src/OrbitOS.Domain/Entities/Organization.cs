using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Organization : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? AzureAdTenantId { get; set; }
    public string? Purpose { get; set; } // Organization's WHY

    // Navigation properties
    public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
    public ICollection<Function> Functions { get; set; } = new List<Function>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<ResourceSubtype> ResourceSubtypes { get; set; } = new List<ResourceSubtype>();
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public ICollection<Process> Processes { get; set; } = new List<Process>();
    public ICollection<Canvas> Canvases { get; set; } = new List<Canvas>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}
