using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

public class Canvas : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required CanvasType CanvasType { get; set; }
    public required CanvasStatus Status { get; set; } = CanvasStatus.Draft;

    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<CanvasBlock> Blocks { get; set; } = new List<CanvasBlock>();
}

public enum CanvasType
{
    BusinessModel,
    Lean,
    ValueProposition
}

public enum CanvasStatus
{
    Draft,
    Active,
    Archived
}
