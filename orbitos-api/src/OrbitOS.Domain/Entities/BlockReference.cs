using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A polymorphic reference from a canvas block to an operational entity.
/// Enables reusability where the same entity can appear in multiple canvases with different context.
/// </summary>
public class BlockReference : BaseEntity
{
    public required ReferenceEntityType EntityType { get; set; }
    public required Guid EntityId { get; set; }
    public required ReferenceRole Role { get; set; } = ReferenceRole.Primary;
    public required ReferenceLinkType LinkType { get; set; } = ReferenceLinkType.Linked;
    public string? ContextNote { get; set; }
    public int SortOrder { get; set; }
    public bool IsHighlighted { get; set; }

    // Metrics stored as JSON
    public string? MetricsJson { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    // Multi-tenancy (denormalized for query efficiency)
    public Guid OrganizationId { get; set; }

    public Guid CanvasBlockId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public CanvasBlock CanvasBlock { get; set; } = null!;

    // Note: Navigation to the actual entity is not defined here due to polymorphism.
    // Use EntityType + EntityId to resolve the actual entity in queries.
}

public enum ReferenceEntityType
{
    Resource,
    Process,
    Activity,
    Product,
    Segment,
    Function,
    Partner,
    Channel,
    ValueProposition,
    CustomerRelationship,
    RevenueStream,
    Role // Added for Key Resources block - links to operational roles
}

public enum ReferenceRole
{
    Primary,
    Supporting,
    Enabling,
    Dependency
}

public enum ReferenceLinkType
{
    Linked,  // Single source of truth, changes propagate
    Copied   // Snapshot, changes don't propagate
}
