using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages value propositions for the Business Model Canvas.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Value Propositions")]
public class ValuePropositionsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<ValuePropositionsController> _logger;

    public ValuePropositionsController(OrbitOSDbContext dbContext, ILogger<ValuePropositionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ValuePropositionDto>>> GetValuePropositions(
        Guid organizationId,
        [FromQuery] ValuePropositionStatus? status = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? segmentId = null)
    {
        var query = _dbContext.ValuePropositions
            .Include(v => v.Product)
            .Include(v => v.Segment)
            .Where(v => v.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        if (productId.HasValue)
            query = query.Where(v => v.ProductId == productId.Value);

        if (segmentId.HasValue)
            query = query.Where(v => v.SegmentId == segmentId.Value);

        var valuePropositions = await query
            .Select(v => new ValuePropositionDto
            {
                Id = v.Id,
                Name = v.Name,
                Slug = v.Slug,
                Headline = v.Headline,
                Description = v.Description,
                Status = v.Status,
                CustomerJobsJson = v.CustomerJobsJson,
                PainsJson = v.PainsJson,
                GainsJson = v.GainsJson,
                PainRelieversJson = v.PainRelieversJson,
                GainCreatorsJson = v.GainCreatorsJson,
                ProductsServicesJson = v.ProductsServicesJson,
                DifferentiatorsJson = v.DifferentiatorsJson,
                ValidationJson = v.ValidationJson,
                TagsJson = v.TagsJson,
                OrganizationId = v.OrganizationId,
                ProductId = v.ProductId,
                ProductName = v.Product != null ? v.Product.Name : null,
                SegmentId = v.SegmentId,
                SegmentName = v.Segment != null ? v.Segment.Name : null,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                CanvasReferenceCount = v.BlockReferences.Count
            })
            .OrderBy(v => v.Name)
            .ToListAsync();

        return Ok(valuePropositions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ValuePropositionDto>> GetValueProposition(Guid organizationId, Guid id)
    {
        var valueProposition = await _dbContext.ValuePropositions
            .Include(v => v.Product)
            .Include(v => v.Segment)
            .Where(v => v.Id == id && v.OrganizationId == organizationId)
            .Select(v => new ValuePropositionDto
            {
                Id = v.Id,
                Name = v.Name,
                Slug = v.Slug,
                Headline = v.Headline,
                Description = v.Description,
                Status = v.Status,
                CustomerJobsJson = v.CustomerJobsJson,
                PainsJson = v.PainsJson,
                GainsJson = v.GainsJson,
                PainRelieversJson = v.PainRelieversJson,
                GainCreatorsJson = v.GainCreatorsJson,
                ProductsServicesJson = v.ProductsServicesJson,
                DifferentiatorsJson = v.DifferentiatorsJson,
                ValidationJson = v.ValidationJson,
                TagsJson = v.TagsJson,
                OrganizationId = v.OrganizationId,
                ProductId = v.ProductId,
                ProductName = v.Product != null ? v.Product.Name : null,
                SegmentId = v.SegmentId,
                SegmentName = v.Segment != null ? v.Segment.Name : null,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                CanvasReferenceCount = v.BlockReferences.Count
            })
            .FirstOrDefaultAsync();

        if (valueProposition == null)
            return NotFound();

        return Ok(valueProposition);
    }

    [HttpPost]
    public async Task<ActionResult<ValuePropositionDto>> CreateValueProposition(
        Guid organizationId,
        [FromBody] CreateValuePropositionRequest request)
    {
        var valueProposition = new ValueProposition
        {
            Name = request.Name,
            Slug = request.Slug,
            Headline = request.Headline,
            Description = request.Description,
            Status = request.Status,
            CustomerJobsJson = request.CustomerJobsJson,
            PainsJson = request.PainsJson,
            GainsJson = request.GainsJson,
            PainRelieversJson = request.PainRelieversJson,
            GainCreatorsJson = request.GainCreatorsJson,
            ProductsServicesJson = request.ProductsServicesJson,
            DifferentiatorsJson = request.DifferentiatorsJson,
            ValidationJson = request.ValidationJson,
            TagsJson = request.TagsJson,
            OrganizationId = organizationId,
            ProductId = request.ProductId,
            SegmentId = request.SegmentId
        };

        _dbContext.ValuePropositions.Add(valueProposition);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetValueProposition), new { organizationId, id = valueProposition.Id },
            new ValuePropositionDto
            {
                Id = valueProposition.Id,
                Name = valueProposition.Name,
                Slug = valueProposition.Slug,
                Headline = valueProposition.Headline,
                Description = valueProposition.Description,
                Status = valueProposition.Status,
                CustomerJobsJson = valueProposition.CustomerJobsJson,
                PainsJson = valueProposition.PainsJson,
                GainsJson = valueProposition.GainsJson,
                PainRelieversJson = valueProposition.PainRelieversJson,
                GainCreatorsJson = valueProposition.GainCreatorsJson,
                ProductsServicesJson = valueProposition.ProductsServicesJson,
                DifferentiatorsJson = valueProposition.DifferentiatorsJson,
                ValidationJson = valueProposition.ValidationJson,
                TagsJson = valueProposition.TagsJson,
                OrganizationId = valueProposition.OrganizationId,
                ProductId = valueProposition.ProductId,
                SegmentId = valueProposition.SegmentId,
                CreatedAt = valueProposition.CreatedAt,
                UpdatedAt = valueProposition.UpdatedAt,
                CanvasReferenceCount = 0
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ValuePropositionDto>> UpdateValueProposition(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateValuePropositionRequest request)
    {
        var valueProposition = await _dbContext.ValuePropositions
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizationId == organizationId);

        if (valueProposition == null)
            return NotFound();

        valueProposition.Name = request.Name;
        valueProposition.Slug = request.Slug;
        valueProposition.Headline = request.Headline;
        valueProposition.Description = request.Description;
        valueProposition.Status = request.Status;
        valueProposition.CustomerJobsJson = request.CustomerJobsJson;
        valueProposition.PainsJson = request.PainsJson;
        valueProposition.GainsJson = request.GainsJson;
        valueProposition.PainRelieversJson = request.PainRelieversJson;
        valueProposition.GainCreatorsJson = request.GainCreatorsJson;
        valueProposition.ProductsServicesJson = request.ProductsServicesJson;
        valueProposition.DifferentiatorsJson = request.DifferentiatorsJson;
        valueProposition.ValidationJson = request.ValidationJson;
        valueProposition.TagsJson = request.TagsJson;
        valueProposition.ProductId = request.ProductId;
        valueProposition.SegmentId = request.SegmentId;

        await _dbContext.SaveChangesAsync();

        var referenceCount = await _dbContext.BlockReferences.CountAsync(r => r.EntityId == id && r.EntityType == ReferenceEntityType.ValueProposition);

        return Ok(new ValuePropositionDto
        {
            Id = valueProposition.Id,
            Name = valueProposition.Name,
            Slug = valueProposition.Slug,
            Headline = valueProposition.Headline,
            Description = valueProposition.Description,
            Status = valueProposition.Status,
            CustomerJobsJson = valueProposition.CustomerJobsJson,
            PainsJson = valueProposition.PainsJson,
            GainsJson = valueProposition.GainsJson,
            PainRelieversJson = valueProposition.PainRelieversJson,
            GainCreatorsJson = valueProposition.GainCreatorsJson,
            ProductsServicesJson = valueProposition.ProductsServicesJson,
            DifferentiatorsJson = valueProposition.DifferentiatorsJson,
            ValidationJson = valueProposition.ValidationJson,
            TagsJson = valueProposition.TagsJson,
            OrganizationId = valueProposition.OrganizationId,
            ProductId = valueProposition.ProductId,
            SegmentId = valueProposition.SegmentId,
            CreatedAt = valueProposition.CreatedAt,
            UpdatedAt = valueProposition.UpdatedAt,
            CanvasReferenceCount = referenceCount
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteValueProposition(Guid organizationId, Guid id)
    {
        var valueProposition = await _dbContext.ValuePropositions
            .FirstOrDefaultAsync(v => v.Id == id && v.OrganizationId == organizationId);

        if (valueProposition == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        valueProposition.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
