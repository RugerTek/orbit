using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// A Business Model Canvas that visualizes and structures strategic information.
/// Canvases can exist at organization, product, segment, or initiative level, forming a hierarchy.
/// </summary>
public class Canvas : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public required CanvasType CanvasType { get; set; }
    public required CanvasScopeType ScopeType { get; set; } = CanvasScopeType.Organization;
    public required CanvasStatus Status { get; set; } = CanvasStatus.Draft;
    public int Version { get; set; } = 1;
    public string? VersionNote { get; set; }

    // AI analysis results stored as JSON
    public string? AiSummary { get; set; }
    public string? AiMetadataJson { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Hierarchy relationships
    public Guid? ParentCanvasId { get; set; }

    // Scope relationships (only one should be set based on ScopeType)
    public Guid? ProductId { get; set; }
    public Guid? SegmentId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Canvas? ParentCanvas { get; set; }
    public Product? Product { get; set; }
    public Segment? Segment { get; set; }
    public ICollection<Canvas> ChildCanvases { get; set; } = new List<Canvas>();
    public ICollection<CanvasBlock> Blocks { get; set; } = new List<CanvasBlock>();
}

public enum CanvasType
{
    BusinessModel,
    Lean,
    ValueProposition,
    Custom
}

public enum CanvasScopeType
{
    Organization,
    Product,
    Segment,
    Initiative
}

public enum CanvasStatus
{
    Draft,
    Active,
    Archived
}
