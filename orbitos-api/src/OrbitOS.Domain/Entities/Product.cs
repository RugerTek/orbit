using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Represents a product or service offered by the organization.
/// Part of the Business Model Canvas value proposition.
/// </summary>
public class Product : BaseEntity
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public ProductType Type { get; set; } = ProductType.Product;
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    // Pricing info stored as JSON
    public string? PricingJson { get; set; }

    // Features stored as JSON array
    public string? FeaturesJson { get; set; }

    // Tags stored as JSON array
    public string? TagsJson { get; set; }

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<ValueProposition> ValuePropositions { get; set; } = new List<ValueProposition>();
    public ICollection<RevenueStream> RevenueStreams { get; set; } = new List<RevenueStream>();
    public ICollection<Canvas> Canvases { get; set; } = new List<Canvas>();
}

public enum ProductType
{
    Product,
    Service,
    Subscription,
    Digital,
    Physical,
    Bundle
}

public enum ProductStatus
{
    Draft,
    Active,
    Deprecated,
    Discontinued
}
