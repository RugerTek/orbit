using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages revenue streams for the Business Model Canvas.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Revenue Streams")]
public class RevenueStreamsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<RevenueStreamsController> _logger;

    public RevenueStreamsController(OrbitOSDbContext dbContext, ILogger<RevenueStreamsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<RevenueStreamDto>>> GetRevenueStreams(
        Guid organizationId,
        [FromQuery] RevenueStreamType? type = null,
        [FromQuery] RevenueStreamStatus? status = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? segmentId = null)
    {
        var query = _dbContext.RevenueStreams
            .Include(rs => rs.Product)
            .Include(rs => rs.Segment)
            .Where(rs => rs.OrganizationId == organizationId);

        if (type.HasValue)
            query = query.Where(rs => rs.Type == type.Value);

        if (status.HasValue)
            query = query.Where(rs => rs.Status == status.Value);

        if (productId.HasValue)
            query = query.Where(rs => rs.ProductId == productId.Value);

        if (segmentId.HasValue)
            query = query.Where(rs => rs.SegmentId == segmentId.Value);

        var revenueStreams = await query
            .Select(rs => new RevenueStreamDto
            {
                Id = rs.Id,
                Name = rs.Name,
                Slug = rs.Slug,
                Description = rs.Description,
                Type = rs.Type,
                Status = rs.Status,
                PricingMechanism = rs.PricingMechanism,
                PricingJson = rs.PricingJson,
                RevenueJson = rs.RevenueJson,
                MetricsJson = rs.MetricsJson,
                WillingnessToPayJson = rs.WillingnessToPayJson,
                TagsJson = rs.TagsJson,
                OrganizationId = rs.OrganizationId,
                ProductId = rs.ProductId,
                ProductName = rs.Product != null ? rs.Product.Name : null,
                SegmentId = rs.SegmentId,
                SegmentName = rs.Segment != null ? rs.Segment.Name : null,
                CreatedAt = rs.CreatedAt,
                UpdatedAt = rs.UpdatedAt,
                CanvasReferenceCount = rs.BlockReferences.Count
            })
            .OrderBy(rs => rs.Name)
            .ToListAsync();

        return Ok(revenueStreams);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RevenueStreamDto>> GetRevenueStream(Guid organizationId, Guid id)
    {
        var revenueStream = await _dbContext.RevenueStreams
            .Include(rs => rs.Product)
            .Include(rs => rs.Segment)
            .Where(rs => rs.Id == id && rs.OrganizationId == organizationId)
            .Select(rs => new RevenueStreamDto
            {
                Id = rs.Id,
                Name = rs.Name,
                Slug = rs.Slug,
                Description = rs.Description,
                Type = rs.Type,
                Status = rs.Status,
                PricingMechanism = rs.PricingMechanism,
                PricingJson = rs.PricingJson,
                RevenueJson = rs.RevenueJson,
                MetricsJson = rs.MetricsJson,
                WillingnessToPayJson = rs.WillingnessToPayJson,
                TagsJson = rs.TagsJson,
                OrganizationId = rs.OrganizationId,
                ProductId = rs.ProductId,
                ProductName = rs.Product != null ? rs.Product.Name : null,
                SegmentId = rs.SegmentId,
                SegmentName = rs.Segment != null ? rs.Segment.Name : null,
                CreatedAt = rs.CreatedAt,
                UpdatedAt = rs.UpdatedAt,
                CanvasReferenceCount = rs.BlockReferences.Count
            })
            .FirstOrDefaultAsync();

        if (revenueStream == null)
            return NotFound();

        return Ok(revenueStream);
    }

    [HttpPost]
    public async Task<ActionResult<RevenueStreamDto>> CreateRevenueStream(
        Guid organizationId,
        [FromBody] CreateRevenueStreamRequest request)
    {
        var revenueStream = new RevenueStream
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Type = request.Type,
            Status = request.Status,
            PricingMechanism = request.PricingMechanism,
            PricingJson = request.PricingJson,
            RevenueJson = request.RevenueJson,
            MetricsJson = request.MetricsJson,
            WillingnessToPayJson = request.WillingnessToPayJson,
            TagsJson = request.TagsJson,
            OrganizationId = organizationId,
            ProductId = request.ProductId,
            SegmentId = request.SegmentId
        };

        _dbContext.RevenueStreams.Add(revenueStream);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRevenueStream), new { organizationId, id = revenueStream.Id },
            new RevenueStreamDto
            {
                Id = revenueStream.Id,
                Name = revenueStream.Name,
                Slug = revenueStream.Slug,
                Description = revenueStream.Description,
                Type = revenueStream.Type,
                Status = revenueStream.Status,
                PricingMechanism = revenueStream.PricingMechanism,
                PricingJson = revenueStream.PricingJson,
                RevenueJson = revenueStream.RevenueJson,
                MetricsJson = revenueStream.MetricsJson,
                WillingnessToPayJson = revenueStream.WillingnessToPayJson,
                TagsJson = revenueStream.TagsJson,
                OrganizationId = revenueStream.OrganizationId,
                ProductId = revenueStream.ProductId,
                SegmentId = revenueStream.SegmentId,
                CreatedAt = revenueStream.CreatedAt,
                UpdatedAt = revenueStream.UpdatedAt,
                CanvasReferenceCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RevenueStreamDto>> UpdateRevenueStream(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateRevenueStreamRequest request)
    {
        var revenueStream = await _dbContext.RevenueStreams
            .FirstOrDefaultAsync(rs => rs.Id == id && rs.OrganizationId == organizationId);

        if (revenueStream == null)
            return NotFound();

        revenueStream.Name = request.Name;
        revenueStream.Slug = request.Slug;
        revenueStream.Description = request.Description;
        revenueStream.Type = request.Type;
        revenueStream.Status = request.Status;
        revenueStream.PricingMechanism = request.PricingMechanism;
        revenueStream.PricingJson = request.PricingJson;
        revenueStream.RevenueJson = request.RevenueJson;
        revenueStream.MetricsJson = request.MetricsJson;
        revenueStream.WillingnessToPayJson = request.WillingnessToPayJson;
        revenueStream.TagsJson = request.TagsJson;
        revenueStream.ProductId = request.ProductId;
        revenueStream.SegmentId = request.SegmentId;

        await _dbContext.SaveChangesAsync();

        var referenceCount = await _dbContext.BlockReferences.CountAsync(r => r.EntityId == id && r.EntityType == ReferenceEntityType.RevenueStream);

        return Ok(new RevenueStreamDto
        {
            Id = revenueStream.Id,
            Name = revenueStream.Name,
            Slug = revenueStream.Slug,
            Description = revenueStream.Description,
            Type = revenueStream.Type,
            Status = revenueStream.Status,
            PricingMechanism = revenueStream.PricingMechanism,
            PricingJson = revenueStream.PricingJson,
            RevenueJson = revenueStream.RevenueJson,
            MetricsJson = revenueStream.MetricsJson,
            WillingnessToPayJson = revenueStream.WillingnessToPayJson,
            TagsJson = revenueStream.TagsJson,
            OrganizationId = revenueStream.OrganizationId,
            ProductId = revenueStream.ProductId,
            SegmentId = revenueStream.SegmentId,
            CreatedAt = revenueStream.CreatedAt,
            UpdatedAt = revenueStream.UpdatedAt,
            CanvasReferenceCount = referenceCount
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRevenueStream(Guid organizationId, Guid id)
    {
        var revenueStream = await _dbContext.RevenueStreams
            .FirstOrDefaultAsync(rs => rs.Id == id && rs.OrganizationId == organizationId);

        if (revenueStream == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        revenueStream.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
