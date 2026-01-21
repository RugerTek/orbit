using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages customer relationships for the Business Model Canvas.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Customer Relationships")]
public class CustomerRelationshipsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<CustomerRelationshipsController> _logger;

    public CustomerRelationshipsController(OrbitOSDbContext dbContext, ILogger<CustomerRelationshipsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerRelationshipDto>>> GetCustomerRelationships(
        Guid organizationId,
        [FromQuery] CustomerRelationshipType? type = null,
        [FromQuery] CustomerRelationshipStatus? status = null,
        [FromQuery] Guid? segmentId = null)
    {
        var query = _dbContext.CustomerRelationships
            .Include(cr => cr.Segment)
            .Where(cr => cr.OrganizationId == organizationId);

        if (type.HasValue)
            query = query.Where(cr => cr.Type == type.Value);

        if (status.HasValue)
            query = query.Where(cr => cr.Status == status.Value);

        if (segmentId.HasValue)
            query = query.Where(cr => cr.SegmentId == segmentId.Value);

        var customerRelationships = await query
            .Select(cr => new CustomerRelationshipDto
            {
                Id = cr.Id,
                Name = cr.Name,
                Slug = cr.Slug,
                Description = cr.Description,
                Type = cr.Type,
                Status = cr.Status,
                PurposeJson = cr.PurposeJson,
                TouchpointsJson = cr.TouchpointsJson,
                LifecycleJson = cr.LifecycleJson,
                MetricsJson = cr.MetricsJson,
                CostJson = cr.CostJson,
                ExpectationsJson = cr.ExpectationsJson,
                TagsJson = cr.TagsJson,
                OrganizationId = cr.OrganizationId,
                SegmentId = cr.SegmentId,
                SegmentName = cr.Segment != null ? cr.Segment.Name : null,
                CreatedAt = cr.CreatedAt,
                UpdatedAt = cr.UpdatedAt,
                CanvasReferenceCount = cr.BlockReferences.Count
            })
            .OrderBy(cr => cr.Name)
            .ToListAsync();

        return Ok(customerRelationships);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerRelationshipDto>> GetCustomerRelationship(Guid organizationId, Guid id)
    {
        var customerRelationship = await _dbContext.CustomerRelationships
            .Include(cr => cr.Segment)
            .Where(cr => cr.Id == id && cr.OrganizationId == organizationId)
            .Select(cr => new CustomerRelationshipDto
            {
                Id = cr.Id,
                Name = cr.Name,
                Slug = cr.Slug,
                Description = cr.Description,
                Type = cr.Type,
                Status = cr.Status,
                PurposeJson = cr.PurposeJson,
                TouchpointsJson = cr.TouchpointsJson,
                LifecycleJson = cr.LifecycleJson,
                MetricsJson = cr.MetricsJson,
                CostJson = cr.CostJson,
                ExpectationsJson = cr.ExpectationsJson,
                TagsJson = cr.TagsJson,
                OrganizationId = cr.OrganizationId,
                SegmentId = cr.SegmentId,
                SegmentName = cr.Segment != null ? cr.Segment.Name : null,
                CreatedAt = cr.CreatedAt,
                UpdatedAt = cr.UpdatedAt,
                CanvasReferenceCount = cr.BlockReferences.Count
            })
            .FirstOrDefaultAsync();

        if (customerRelationship == null)
            return NotFound();

        return Ok(customerRelationship);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerRelationshipDto>> CreateCustomerRelationship(
        Guid organizationId,
        [FromBody] CreateCustomerRelationshipRequest request)
    {
        if (request.SegmentId.HasValue)
        {
            var segmentExists = await _dbContext.Set<Segment>()
                .AnyAsync(s => s.Id == request.SegmentId.Value && s.OrganizationId == organizationId);
            if (!segmentExists)
                return BadRequest("Invalid segment");
        }

        var customerRelationship = new CustomerRelationship
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Type = request.Type,
            Status = request.Status,
            PurposeJson = request.PurposeJson,
            TouchpointsJson = request.TouchpointsJson,
            LifecycleJson = request.LifecycleJson,
            MetricsJson = request.MetricsJson,
            CostJson = request.CostJson,
            ExpectationsJson = request.ExpectationsJson,
            TagsJson = request.TagsJson,
            OrganizationId = organizationId,
            SegmentId = request.SegmentId
        };

        _dbContext.CustomerRelationships.Add(customerRelationship);
        await _dbContext.SaveChangesAsync();

        string? segmentName = null;
        if (request.SegmentId.HasValue)
        {
            segmentName = await _dbContext.Set<Segment>()
                .Where(s => s.Id == request.SegmentId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
        }

        return CreatedAtAction(nameof(GetCustomerRelationship), new { organizationId, id = customerRelationship.Id },
            new CustomerRelationshipDto
            {
                Id = customerRelationship.Id,
                Name = customerRelationship.Name,
                Slug = customerRelationship.Slug,
                Description = customerRelationship.Description,
                Type = customerRelationship.Type,
                Status = customerRelationship.Status,
                PurposeJson = customerRelationship.PurposeJson,
                TouchpointsJson = customerRelationship.TouchpointsJson,
                LifecycleJson = customerRelationship.LifecycleJson,
                MetricsJson = customerRelationship.MetricsJson,
                CostJson = customerRelationship.CostJson,
                ExpectationsJson = customerRelationship.ExpectationsJson,
                TagsJson = customerRelationship.TagsJson,
                OrganizationId = customerRelationship.OrganizationId,
                SegmentId = customerRelationship.SegmentId,
                SegmentName = segmentName,
                CreatedAt = customerRelationship.CreatedAt,
                UpdatedAt = customerRelationship.UpdatedAt,
                CanvasReferenceCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerRelationshipDto>> UpdateCustomerRelationship(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateCustomerRelationshipRequest request)
    {
        var customerRelationship = await _dbContext.CustomerRelationships
            .FirstOrDefaultAsync(cr => cr.Id == id && cr.OrganizationId == organizationId);

        if (customerRelationship == null)
            return NotFound();

        if (request.SegmentId.HasValue)
        {
            var segmentExists = await _dbContext.Set<Segment>()
                .AnyAsync(s => s.Id == request.SegmentId.Value && s.OrganizationId == organizationId);
            if (!segmentExists)
                return BadRequest("Invalid segment");
        }

        customerRelationship.Name = request.Name;
        customerRelationship.Slug = request.Slug;
        customerRelationship.Description = request.Description;
        customerRelationship.Type = request.Type;
        customerRelationship.Status = request.Status;
        customerRelationship.PurposeJson = request.PurposeJson;
        customerRelationship.TouchpointsJson = request.TouchpointsJson;
        customerRelationship.LifecycleJson = request.LifecycleJson;
        customerRelationship.MetricsJson = request.MetricsJson;
        customerRelationship.CostJson = request.CostJson;
        customerRelationship.ExpectationsJson = request.ExpectationsJson;
        customerRelationship.TagsJson = request.TagsJson;
        customerRelationship.SegmentId = request.SegmentId;

        await _dbContext.SaveChangesAsync();

        string? segmentName = null;
        if (customerRelationship.SegmentId.HasValue)
        {
            segmentName = await _dbContext.Set<Segment>()
                .Where(s => s.Id == customerRelationship.SegmentId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
        }

        var referenceCount = await _dbContext.BlockReferences.CountAsync(r => r.EntityId == id && r.EntityType == ReferenceEntityType.CustomerRelationship);

        return Ok(new CustomerRelationshipDto
        {
            Id = customerRelationship.Id,
            Name = customerRelationship.Name,
            Slug = customerRelationship.Slug,
            Description = customerRelationship.Description,
            Type = customerRelationship.Type,
            Status = customerRelationship.Status,
            PurposeJson = customerRelationship.PurposeJson,
            TouchpointsJson = customerRelationship.TouchpointsJson,
            LifecycleJson = customerRelationship.LifecycleJson,
            MetricsJson = customerRelationship.MetricsJson,
            CostJson = customerRelationship.CostJson,
            ExpectationsJson = customerRelationship.ExpectationsJson,
            TagsJson = customerRelationship.TagsJson,
            OrganizationId = customerRelationship.OrganizationId,
            SegmentId = customerRelationship.SegmentId,
            SegmentName = segmentName,
            CreatedAt = customerRelationship.CreatedAt,
            UpdatedAt = customerRelationship.UpdatedAt,
            CanvasReferenceCount = referenceCount
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomerRelationship(Guid organizationId, Guid id)
    {
        var customerRelationship = await _dbContext.CustomerRelationships
            .FirstOrDefaultAsync(cr => cr.Id == id && cr.OrganizationId == organizationId);

        if (customerRelationship == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        customerRelationship.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
