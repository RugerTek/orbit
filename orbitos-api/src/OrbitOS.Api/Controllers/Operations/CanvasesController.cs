using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages business model canvases and their blocks.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Canvases")]
public class CanvasesController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<CanvasesController> _logger;

    public CanvasesController(OrbitOSDbContext dbContext, ILogger<CanvasesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all canvases for an organization
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CanvasDto>>> GetCanvases(
        Guid organizationId,
        [FromQuery] CanvasScopeType? scopeType = null,
        [FromQuery] CanvasStatus? status = null,
        [FromQuery] Guid? productId = null)
    {
        var query = _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.OrganizationId == organizationId);

        if (scopeType.HasValue)
            query = query.Where(c => c.ScopeType == scopeType.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (productId.HasValue)
            query = query.Where(c => c.ProductId == productId.Value);

        var canvases = await query
            .Select(c => new CanvasDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                CanvasType = c.CanvasType,
                ScopeType = c.ScopeType,
                Status = c.Status,
                Version = c.Version,
                VersionNote = c.VersionNote,
                AiSummary = c.AiSummary,
                OrganizationId = c.OrganizationId,
                ParentCanvasId = c.ParentCanvasId,
                ProductId = c.ProductId,
                ProductName = c.Product != null ? c.Product.Name : null,
                SegmentId = c.SegmentId,
                SegmentName = c.Segment != null ? c.Segment.Name : null,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .OrderBy(c => c.ScopeType)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Ok(canvases);
    }

    /// <summary>
    /// Get BMC canvases (Business Model Canvas type only) - convenience endpoint
    /// </summary>
    [HttpGet("bmc")]
    public async Task<ActionResult<List<CanvasDto>>> GetBmcCanvases(
        Guid organizationId,
        [FromQuery] CanvasScopeType? scopeType = null,
        [FromQuery] CanvasStatus? status = null,
        [FromQuery] Guid? productId = null)
    {
        var query = _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel);

        if (scopeType.HasValue)
            query = query.Where(c => c.ScopeType == scopeType.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (productId.HasValue)
            query = query.Where(c => c.ProductId == productId.Value);

        var canvases = await query
            .Select(c => new CanvasDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                CanvasType = c.CanvasType,
                ScopeType = c.ScopeType,
                Status = c.Status,
                Version = c.Version,
                VersionNote = c.VersionNote,
                AiSummary = c.AiSummary,
                OrganizationId = c.OrganizationId,
                ParentCanvasId = c.ParentCanvasId,
                ProductId = c.ProductId,
                ProductName = c.Product != null ? c.Product.Name : null,
                SegmentId = c.SegmentId,
                SegmentName = c.Segment != null ? c.Segment.Name : null,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .OrderBy(c => c.ScopeType)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Ok(canvases);
    }

    /// <summary>
    /// Get a specific canvas by ID with all block references
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CanvasDto>> GetCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
                .ThenInclude(b => b.References)
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.Id == id && c.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (canvas == null)
            return NotFound();

        return Ok(await MapToDtoWithReferences(canvas));
    }

    /// <summary>
    /// Get a specific BMC canvas by ID with all block references
    /// </summary>
    [HttpGet("bmc/{id}")]
    public async Task<ActionResult<CanvasDto>> GetBmcCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
                .ThenInclude(b => b.References)
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.Id == id && c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel)
            .FirstOrDefaultAsync();

        if (canvas == null)
            return NotFound();

        return Ok(await MapToDtoWithReferences(canvas));
    }

    /// <summary>
    /// Get a canvas with enriched block references (includes people under roles, activities in processes)
    /// This is the endpoint for AI consumption and detailed UI views.
    /// </summary>
    [HttpGet("{id}/enriched")]
    public async Task<ActionResult<CanvasDto>> GetCanvasEnriched(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
                .ThenInclude(b => b.References)
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .Where(c => c.Id == id && c.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (canvas == null)
            return NotFound();

        return Ok(await MapToDtoEnriched(canvas));
    }

    /// <summary>
    /// Create a new canvas
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CanvasDto>> CreateCanvas(
        Guid organizationId,
        [FromBody] CreateCanvasRequest request)
    {
        var canvas = new Canvas
        {
            Name = request.Name,
            Description = request.Description,
            CanvasType = request.CanvasType,
            ScopeType = request.ScopeType,
            Status = request.Status,
            OrganizationId = organizationId,
            ParentCanvasId = request.ParentCanvasId,
            ProductId = request.ProductId,
            SegmentId = request.SegmentId,
            Version = 1
        };

        // Create default blocks for Business Model Canvas
        if (request.CanvasType == CanvasType.BusinessModel)
        {
            var blockTypes = Enum.GetValues<CanvasBlockType>();
            foreach (var blockType in blockTypes)
            {
                canvas.Blocks.Add(new CanvasBlock
                {
                    BlockType = blockType,
                    Content = "[]",
                    DisplayOrder = (int)blockType,
                    OrganizationId = organizationId
                });
            }
        }

        _dbContext.Canvases.Add(canvas);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCanvas), new { organizationId, id = canvas.Id }, MapToDto(canvas));
    }

    /// <summary>
    /// Create a new BMC canvas (Business Model Canvas)
    /// </summary>
    [HttpPost("bmc")]
    public async Task<ActionResult<CanvasDto>> CreateBmcCanvas(
        Guid organizationId,
        [FromBody] CreateCanvasRequest request)
    {
        // Force BusinessModel type
        request.CanvasType = CanvasType.BusinessModel;

        var canvas = new Canvas
        {
            Name = request.Name,
            Description = request.Description,
            CanvasType = CanvasType.BusinessModel,
            ScopeType = request.ScopeType,
            Status = request.Status,
            OrganizationId = organizationId,
            ParentCanvasId = request.ParentCanvasId,
            ProductId = request.ProductId,
            SegmentId = request.SegmentId,
            Version = 1
        };

        // Create default blocks for Business Model Canvas
        var blockTypes = Enum.GetValues<CanvasBlockType>();
        foreach (var blockType in blockTypes)
        {
            canvas.Blocks.Add(new CanvasBlock
            {
                BlockType = blockType,
                Content = "[]",
                DisplayOrder = (int)blockType,
                OrganizationId = organizationId
            });
        }

        _dbContext.Canvases.Add(canvas);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBmcCanvas), new { organizationId, id = canvas.Id }, MapToDto(canvas));
    }

    /// <summary>
    /// Update an existing canvas
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CanvasDto>> UpdateCanvas(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateCanvasRequest request)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound();

        canvas.Name = request.Name;
        canvas.Description = request.Description;
        canvas.CanvasType = request.CanvasType;
        canvas.ScopeType = request.ScopeType;
        canvas.Status = request.Status;
        canvas.ParentCanvasId = request.ParentCanvasId;
        canvas.ProductId = request.ProductId;
        canvas.SegmentId = request.SegmentId;

        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(canvas));
    }

    /// <summary>
    /// Update an existing BMC canvas
    /// </summary>
    [HttpPut("bmc/{id}")]
    public async Task<ActionResult<CanvasDto>> UpdateBmcCanvas(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateCanvasRequest request)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Product)
            .Include(c => c.Segment)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel);

        if (canvas == null)
            return NotFound();

        canvas.Name = request.Name;
        canvas.Description = request.Description;
        canvas.ScopeType = request.ScopeType;
        canvas.Status = request.Status;
        canvas.ParentCanvasId = request.ParentCanvasId;
        canvas.ProductId = request.ProductId;
        canvas.SegmentId = request.SegmentId;

        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(canvas));
    }

    /// <summary>
    /// Update a specific block within a canvas
    /// </summary>
    [HttpPut("{canvasId}/blocks/{blockType}")]
    public async Task<ActionResult<CanvasBlockDto>> UpdateCanvasBlock(
        Guid organizationId,
        Guid canvasId,
        CanvasBlockType blockType,
        [FromBody] UpdateCanvasBlockRequest request)
    {
        var canvas = await _dbContext.Canvases
            .Include(c => c.Blocks)
            .FirstOrDefaultAsync(c => c.Id == canvasId && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound("Canvas not found");

        var block = canvas.Blocks.FirstOrDefault(b => b.BlockType == blockType);
        if (block == null)
        {
            block = new CanvasBlock
            {
                BlockType = blockType,
                Content = request.Content,
                DisplayOrder = request.DisplayOrder,
                CanvasId = canvasId
            };
            _dbContext.CanvasBlocks.Add(block);
        }
        else
        {
            block.Content = request.Content;
            block.DisplayOrder = request.DisplayOrder;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new CanvasBlockDto
        {
            Id = block.Id,
            BlockType = block.BlockType,
            Content = block.Content,
            DisplayOrder = block.DisplayOrder,
            CanvasId = block.CanvasId
        });
    }

    /// <summary>
    /// Delete a canvas
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

        if (canvas == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        canvas.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a BMC canvas
    /// </summary>
    [HttpDelete("bmc/{id}")]
    public async Task<IActionResult> DeleteBmcCanvas(Guid organizationId, Guid id)
    {
        var canvas = await _dbContext.Canvases
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.CanvasType == CanvasType.BusinessModel);

        if (canvas == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        canvas.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================================
    // BLOCK REFERENCE ENDPOINTS
    // =========================================================================

    /// <summary>
    /// Get all references for a specific canvas block
    /// </summary>
    [HttpGet("{canvasId}/blocks/{blockType}/references")]
    public async Task<ActionResult<List<BlockReferenceDto>>> GetBlockReferences(
        Guid organizationId,
        Guid canvasId,
        CanvasBlockType blockType)
    {
        var block = await _dbContext.CanvasBlocks
            .Include(b => b.References)
            .FirstOrDefaultAsync(b => b.CanvasId == canvasId
                && b.BlockType == blockType
                && b.OrganizationId == organizationId);

        if (block == null)
            return NotFound("Block not found");

        var references = block.References.OrderBy(r => r.SortOrder).ToList();
        var dtos = new List<BlockReferenceDto>();

        foreach (var reference in references)
        {
            var dto = await MapBlockReferenceToDto(reference);
            dtos.Add(dto);
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Add a reference to a canvas block (link an entity like Role or Process)
    /// </summary>
    [HttpPost("{canvasId}/blocks/{blockType}/references")]
    public async Task<ActionResult<BlockReferenceDto>> AddBlockReference(
        Guid organizationId,
        Guid canvasId,
        CanvasBlockType blockType,
        [FromBody] CreateBlockReferenceRequest request)
    {
        var block = await _dbContext.CanvasBlocks
            .Include(b => b.References)
            .FirstOrDefaultAsync(b => b.CanvasId == canvasId
                && b.BlockType == blockType
                && b.OrganizationId == organizationId);

        if (block == null)
            return NotFound("Block not found");

        // Validate that entity exists
        var entityExists = await ValidateEntityExists(organizationId, request.EntityType, request.EntityId);
        if (!entityExists)
            return BadRequest($"Entity of type {request.EntityType} with ID {request.EntityId} not found");

        // Check for duplicate reference
        var existingRef = block.References.FirstOrDefault(r =>
            r.EntityType == request.EntityType && r.EntityId == request.EntityId);
        if (existingRef != null)
            return BadRequest("This entity is already linked to this block");

        var reference = new BlockReference
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Role = request.Role,
            LinkType = request.LinkType,
            ContextNote = request.ContextNote,
            SortOrder = request.SortOrder > 0 ? request.SortOrder : block.References.Count,
            IsHighlighted = request.IsHighlighted,
            MetricsJson = request.MetricsJson,
            TagsJson = request.TagsJson,
            OrganizationId = organizationId,
            CanvasBlockId = block.Id
        };

        _dbContext.BlockReferences.Add(reference);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Added {EntityType} reference {EntityId} to block {BlockType} in canvas {CanvasId}",
            request.EntityType, request.EntityId, blockType, canvasId);

        return CreatedAtAction(
            nameof(GetBlockReferences),
            new { organizationId, canvasId, blockType },
            await MapBlockReferenceToDto(reference));
    }

    /// <summary>
    /// Update a block reference (change role, context note, etc.)
    /// </summary>
    [HttpPut("{canvasId}/blocks/{blockType}/references/{referenceId}")]
    public async Task<ActionResult<BlockReferenceDto>> UpdateBlockReference(
        Guid organizationId,
        Guid canvasId,
        CanvasBlockType blockType,
        Guid referenceId,
        [FromBody] UpdateBlockReferenceRequest request)
    {
        var reference = await _dbContext.BlockReferences
            .Include(r => r.CanvasBlock)
            .FirstOrDefaultAsync(r => r.Id == referenceId
                && r.OrganizationId == organizationId
                && r.CanvasBlock.CanvasId == canvasId
                && r.CanvasBlock.BlockType == blockType);

        if (reference == null)
            return NotFound("Reference not found");

        reference.Role = request.Role;
        reference.LinkType = request.LinkType;
        reference.ContextNote = request.ContextNote;
        reference.SortOrder = request.SortOrder;
        reference.IsHighlighted = request.IsHighlighted;
        reference.MetricsJson = request.MetricsJson;
        reference.TagsJson = request.TagsJson;

        await _dbContext.SaveChangesAsync();

        return Ok(await MapBlockReferenceToDto(reference));
    }

    /// <summary>
    /// Remove a reference from a canvas block
    /// </summary>
    [HttpDelete("{canvasId}/blocks/{blockType}/references/{referenceId}")]
    public async Task<IActionResult> RemoveBlockReference(
        Guid organizationId,
        Guid canvasId,
        CanvasBlockType blockType,
        Guid referenceId)
    {
        var reference = await _dbContext.BlockReferences
            .Include(r => r.CanvasBlock)
            .FirstOrDefaultAsync(r => r.Id == referenceId
                && r.OrganizationId == organizationId
                && r.CanvasBlock.CanvasId == canvasId
                && r.CanvasBlock.BlockType == blockType);

        if (reference == null)
            return NotFound("Reference not found");

        _dbContext.BlockReferences.Remove(reference);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Removed reference {ReferenceId} from block {BlockType} in canvas {CanvasId}",
            referenceId, blockType, canvasId);

        return NoContent();
    }

    /// <summary>
    /// Reorder references within a block
    /// </summary>
    [HttpPut("{canvasId}/blocks/{blockType}/references/reorder")]
    public async Task<IActionResult> ReorderBlockReferences(
        Guid organizationId,
        Guid canvasId,
        CanvasBlockType blockType,
        [FromBody] List<Guid> referenceIds)
    {
        var block = await _dbContext.CanvasBlocks
            .Include(b => b.References)
            .FirstOrDefaultAsync(b => b.CanvasId == canvasId
                && b.BlockType == blockType
                && b.OrganizationId == organizationId);

        if (block == null)
            return NotFound("Block not found");

        for (int i = 0; i < referenceIds.Count; i++)
        {
            var reference = block.References.FirstOrDefault(r => r.Id == referenceIds[i]);
            if (reference != null)
            {
                reference.SortOrder = i;
            }
        }

        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    private async Task<bool> ValidateEntityExists(Guid organizationId, ReferenceEntityType entityType, Guid entityId)
    {
        return entityType switch
        {
            ReferenceEntityType.Role => await _dbContext.Roles
                .AnyAsync(r => r.Id == entityId && r.OrganizationId == organizationId),
            ReferenceEntityType.Process => await _dbContext.Processes
                .AnyAsync(p => p.Id == entityId && p.OrganizationId == organizationId),
            ReferenceEntityType.Resource => await _dbContext.Resources
                .AnyAsync(r => r.Id == entityId && r.OrganizationId == organizationId),
            ReferenceEntityType.Function => await _dbContext.Functions
                .AnyAsync(f => f.Id == entityId && f.OrganizationId == organizationId),
            ReferenceEntityType.Partner => await _dbContext.Partners
                .AnyAsync(p => p.Id == entityId && p.OrganizationId == organizationId),
            ReferenceEntityType.Channel => await _dbContext.Channels
                .AnyAsync(c => c.Id == entityId && c.OrganizationId == organizationId),
            ReferenceEntityType.ValueProposition => await _dbContext.ValuePropositions
                .AnyAsync(v => v.Id == entityId && v.OrganizationId == organizationId),
            ReferenceEntityType.CustomerRelationship => await _dbContext.CustomerRelationships
                .AnyAsync(c => c.Id == entityId && c.OrganizationId == organizationId),
            ReferenceEntityType.RevenueStream => await _dbContext.RevenueStreams
                .AnyAsync(r => r.Id == entityId && r.OrganizationId == organizationId),
            ReferenceEntityType.Activity => await _dbContext.Activities
                .AnyAsync(a => a.Id == entityId),
            _ => false
        };
    }

    private async Task<BlockReferenceDto> MapBlockReferenceToDto(BlockReference reference)
    {
        string? entityName = null;

        // Resolve entity name based on type
        entityName = reference.EntityType switch
        {
            ReferenceEntityType.Role => await _dbContext.Roles
                .Where(r => r.Id == reference.EntityId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.Process => await _dbContext.Processes
                .Where(p => p.Id == reference.EntityId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.Resource => await _dbContext.Resources
                .Where(r => r.Id == reference.EntityId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.Function => await _dbContext.Functions
                .Where(f => f.Id == reference.EntityId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.Partner => await _dbContext.Partners
                .Where(p => p.Id == reference.EntityId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.Channel => await _dbContext.Channels
                .Where(c => c.Id == reference.EntityId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.ValueProposition => await _dbContext.ValuePropositions
                .Where(v => v.Id == reference.EntityId)
                .Select(v => v.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.CustomerRelationship => await _dbContext.CustomerRelationships
                .Where(c => c.Id == reference.EntityId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.RevenueStream => await _dbContext.RevenueStreams
                .Where(r => r.Id == reference.EntityId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(),
            ReferenceEntityType.Activity => await _dbContext.Activities
                .Where(a => a.Id == reference.EntityId)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(),
            _ => null
        };

        return new BlockReferenceDto
        {
            Id = reference.Id,
            EntityType = reference.EntityType,
            EntityId = reference.EntityId,
            EntityName = entityName,
            Role = reference.Role,
            LinkType = reference.LinkType,
            ContextNote = reference.ContextNote,
            SortOrder = reference.SortOrder,
            IsHighlighted = reference.IsHighlighted,
            MetricsJson = reference.MetricsJson,
            TagsJson = reference.TagsJson,
            OrganizationId = reference.OrganizationId,
            CanvasBlockId = reference.CanvasBlockId,
            CreatedAt = reference.CreatedAt,
            UpdatedAt = reference.UpdatedAt
        };
    }

    private static CanvasDto MapToDto(Canvas canvas)
    {
        return new CanvasDto
        {
            Id = canvas.Id,
            Name = canvas.Name,
            Slug = canvas.Slug,
            Description = canvas.Description,
            CanvasType = canvas.CanvasType,
            ScopeType = canvas.ScopeType,
            Status = canvas.Status,
            Version = canvas.Version,
            VersionNote = canvas.VersionNote,
            AiSummary = canvas.AiSummary,
            OrganizationId = canvas.OrganizationId,
            ParentCanvasId = canvas.ParentCanvasId,
            ProductId = canvas.ProductId,
            ProductName = canvas.Product?.Name,
            SegmentId = canvas.SegmentId,
            SegmentName = canvas.Segment?.Name,
            CreatedAt = canvas.CreatedAt,
            UpdatedAt = canvas.UpdatedAt,
            Blocks = canvas.Blocks.OrderBy(b => b.DisplayOrder).Select(b => new CanvasBlockDto
            {
                Id = b.Id,
                BlockType = b.BlockType,
                Content = b.Content,
                DisplayOrder = b.DisplayOrder,
                CanvasId = b.CanvasId
            }).ToList()
        };
    }

    /// <summary>
    /// Map canvas with basic block references (entity names only)
    /// </summary>
    private async Task<CanvasDto> MapToDtoWithReferences(Canvas canvas)
    {
        var dto = new CanvasDto
        {
            Id = canvas.Id,
            Name = canvas.Name,
            Slug = canvas.Slug,
            Description = canvas.Description,
            CanvasType = canvas.CanvasType,
            ScopeType = canvas.ScopeType,
            Status = canvas.Status,
            Version = canvas.Version,
            VersionNote = canvas.VersionNote,
            AiSummary = canvas.AiSummary,
            OrganizationId = canvas.OrganizationId,
            ParentCanvasId = canvas.ParentCanvasId,
            ProductId = canvas.ProductId,
            ProductName = canvas.Product?.Name,
            SegmentId = canvas.SegmentId,
            SegmentName = canvas.Segment?.Name,
            CreatedAt = canvas.CreatedAt,
            UpdatedAt = canvas.UpdatedAt,
            Blocks = new List<CanvasBlockDto>()
        };

        foreach (var block in canvas.Blocks.OrderBy(b => b.DisplayOrder))
        {
            var blockDto = new CanvasBlockDto
            {
                Id = block.Id,
                BlockType = block.BlockType,
                Content = block.Content,
                DisplayOrder = block.DisplayOrder,
                CanvasId = block.CanvasId,
                References = new List<BlockReferenceDto>()
            };

            foreach (var reference in block.References.OrderBy(r => r.SortOrder))
            {
                blockDto.References.Add(await MapBlockReferenceToDto(reference));
            }

            dto.Blocks.Add(blockDto);
        }

        return dto;
    }

    /// <summary>
    /// Map canvas with enriched block references (includes people under roles, activities in processes)
    /// </summary>
    private async Task<CanvasDto> MapToDtoEnriched(Canvas canvas)
    {
        var dto = new CanvasDto
        {
            Id = canvas.Id,
            Name = canvas.Name,
            Slug = canvas.Slug,
            Description = canvas.Description,
            CanvasType = canvas.CanvasType,
            ScopeType = canvas.ScopeType,
            Status = canvas.Status,
            Version = canvas.Version,
            VersionNote = canvas.VersionNote,
            AiSummary = canvas.AiSummary,
            OrganizationId = canvas.OrganizationId,
            ParentCanvasId = canvas.ParentCanvasId,
            ProductId = canvas.ProductId,
            ProductName = canvas.Product?.Name,
            SegmentId = canvas.SegmentId,
            SegmentName = canvas.Segment?.Name,
            CreatedAt = canvas.CreatedAt,
            UpdatedAt = canvas.UpdatedAt,
            Blocks = new List<CanvasBlockDto>()
        };

        foreach (var block in canvas.Blocks.OrderBy(b => b.DisplayOrder))
        {
            var blockDto = new CanvasBlockDto
            {
                Id = block.Id,
                BlockType = block.BlockType,
                Content = block.Content,
                DisplayOrder = block.DisplayOrder,
                CanvasId = block.CanvasId,
                References = new List<BlockReferenceDto>()
            };

            foreach (var reference in block.References.OrderBy(r => r.SortOrder))
            {
                blockDto.References.Add(await MapBlockReferenceToEnrichedDto(reference));
            }

            dto.Blocks.Add(blockDto);
        }

        return dto;
    }

    /// <summary>
    /// Map block reference with enriched entity details
    /// </summary>
    private async Task<EnrichedBlockReferenceDto> MapBlockReferenceToEnrichedDto(BlockReference reference)
    {
        var baseDto = await MapBlockReferenceToDto(reference);

        var enrichedDto = new EnrichedBlockReferenceDto
        {
            Id = baseDto.Id,
            EntityType = baseDto.EntityType,
            EntityId = baseDto.EntityId,
            EntityName = baseDto.EntityName,
            Role = baseDto.Role,
            LinkType = baseDto.LinkType,
            ContextNote = baseDto.ContextNote,
            SortOrder = baseDto.SortOrder,
            IsHighlighted = baseDto.IsHighlighted,
            MetricsJson = baseDto.MetricsJson,
            TagsJson = baseDto.TagsJson,
            OrganizationId = baseDto.OrganizationId,
            CanvasBlockId = baseDto.CanvasBlockId,
            CreatedAt = baseDto.CreatedAt,
            UpdatedAt = baseDto.UpdatedAt
        };

        // Enrich based on entity type
        if (reference.EntityType == ReferenceEntityType.Role)
        {
            // Get assigned people for this role
            var assignments = await _dbContext.RoleAssignments
                .Include(ra => ra.Resource)
                .Where(ra => ra.RoleId == reference.EntityId && ra.Resource.DeletedAt == null)
                .Select(ra => new RoleAssignmentSummaryDto
                {
                    ResourceId = ra.ResourceId,
                    ResourceName = ra.Resource.Name,
                    AllocationPercentage = ra.AllocationPercentage,
                    IsPrimary = ra.IsPrimary,
                    Status = ra.Resource.Status
                })
                .ToListAsync();

            enrichedDto.AssignedPeople = assignments;

            // Calculate coverage status
            enrichedDto.CoverageStatus = assignments.Count switch
            {
                0 => "Uncovered",
                1 => "AtRisk", // Single point of failure
                _ => "Covered"
            };

            // Get functions for this role
            var functions = await _dbContext.RoleFunctions
                .Include(rf => rf.Function)
                .Where(rf => rf.RoleId == reference.EntityId)
                .Select(rf => new FunctionSummaryDto
                {
                    Id = rf.FunctionId,
                    Name = rf.Function.Name,
                    Category = rf.Function.Category
                })
                .ToListAsync();

            enrichedDto.Functions = functions;
        }
        else if (reference.EntityType == ReferenceEntityType.Process)
        {
            // Get activities for this process
            var activities = await _dbContext.Activities
                .Where(a => a.ProcessId == reference.EntityId && a.DeletedAt == null)
                .OrderBy(a => a.Order)
                .Select(a => new ActivitySummaryDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    ActivityType = a.ActivityType,
                    Order = a.Order
                })
                .ToListAsync();

            enrichedDto.Activities = activities;

            // Get process status
            var processStatus = await _dbContext.Processes
                .Where(p => p.Id == reference.EntityId)
                .Select(p => p.Status)
                .FirstOrDefaultAsync();

            enrichedDto.ProcessStatus = processStatus;
        }

        return enrichedDto;
    }
}
