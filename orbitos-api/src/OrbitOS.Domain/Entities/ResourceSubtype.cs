using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class ResourceSubtype : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required ResourceType ResourceType { get; set; }
    public string? Icon { get; set; }
    public string? MetadataSchema { get; set; } // JSON schema for custom fields
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}

public enum ResourceType
{
    Person,
    Team,
    Tool,
    Automation,
    Partner,
    Asset
}
