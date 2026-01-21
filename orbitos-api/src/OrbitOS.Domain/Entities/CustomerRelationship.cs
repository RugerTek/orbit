using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A type of relationship that the organization establishes with customer segments.
/// Defines how the company acquires, retains, and grows customers through different relationship models.
/// </summary>
public class CustomerRelationship : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required CustomerRelationshipType Type { get; set; }
    public required CustomerRelationshipStatus Status { get; set; } = CustomerRelationshipStatus.Active;

    // Purpose stored as JSON array of strings
    public string? PurposeJson { get; set; }

    // Touchpoints stored as JSON array
    public string? TouchpointsJson { get; set; }

    // Lifecycle stages stored as JSON
    public string? LifecycleJson { get; set; }

    // Performance metrics stored as JSON
    public string? MetricsJson { get; set; }

    // Cost information stored as JSON
    public string? CostJson { get; set; }

    // Customer expectations stored as JSON array
    public string? ExpectationsJson { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Optional segment relationship
    public Guid? SegmentId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Segment? Segment { get; set; }
    public ICollection<BlockReference> BlockReferences { get; set; } = new List<BlockReference>();
}

public enum CustomerRelationshipType
{
    PersonalAssistance,
    DedicatedAssistance,
    SelfService,
    AutomatedService,
    Communities,
    CoCreation
}

public enum CustomerRelationshipStatus
{
    Planned,
    Active,
    Optimizing,
    Sunset
}
