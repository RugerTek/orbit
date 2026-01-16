using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages operational functions (business capabilities/skills) within an organization.
/// Functions represent what the organization can do - like "Data Migration", "Customer Support", etc.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Functions")]
public class FunctionsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<FunctionsController> _logger;

    public FunctionsController(OrbitOSDbContext dbContext, ILogger<FunctionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<OpsFunctionDto>>> GetFunctions(Guid organizationId, [FromQuery] string? category = null)
    {
        var query = _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(f => f.Category == category);
        }

        var functions = await query
            .Select(f => new OpsFunctionDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                Purpose = f.Purpose,
                Category = f.Category,
                Status = f.Status,
                OrganizationId = f.OrganizationId,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                CapabilityCount = f.FunctionCapabilities.Count,
                RoleCount = f.RoleFunctions.Count
            })
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToListAsync();

        return Ok(functions);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories(Guid organizationId)
    {
        var categories = await _dbContext.Functions
            .Where(f => f.OrganizationId == organizationId && f.Category != null)
            .Select(f => f.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OpsFunctionDto>> GetFunction(Guid organizationId, Guid id)
    {
        var func = await _dbContext.Functions
            .Where(f => f.Id == id && f.OrganizationId == organizationId)
            .Select(f => new OpsFunctionDto
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                Purpose = f.Purpose,
                Category = f.Category,
                Status = f.Status,
                OrganizationId = f.OrganizationId,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                CapabilityCount = f.FunctionCapabilities.Count,
                RoleCount = f.RoleFunctions.Count
            })
            .FirstOrDefaultAsync();

        if (func == null)
            return NotFound();

        return Ok(func);
    }

    [HttpPost]
    public async Task<ActionResult<OpsFunctionDto>> CreateFunction(
        Guid organizationId,
        [FromBody] CreateOpsFunctionRequest request)
    {
        var func = new Function
        {
            Name = request.Name,
            Description = request.Description,
            Purpose = request.Purpose,
            Category = request.Category,
            Status = FunctionStatus.Active,
            OrganizationId = organizationId
        };

        _dbContext.Functions.Add(func);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFunction), new { organizationId, id = func.Id },
            new OpsFunctionDto
            {
                Id = func.Id,
                Name = func.Name,
                Description = func.Description,
                Purpose = func.Purpose,
                Category = func.Category,
                Status = func.Status,
                OrganizationId = func.OrganizationId,
                CreatedAt = func.CreatedAt,
                UpdatedAt = func.UpdatedAt,
                CapabilityCount = 0,
                RoleCount = 0
            });
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<BulkCreateFunctionsResponse>> BulkCreateFunctions(
        Guid organizationId,
        [FromBody] BulkCreateFunctionsRequest request)
    {
        var response = new BulkCreateFunctionsResponse();

        foreach (var funcRequest in request.Functions)
        {
            try
            {
                // Check for duplicate name
                var exists = await _dbContext.Functions
                    .AnyAsync(f => f.OrganizationId == organizationId && f.Name == funcRequest.Name);

                if (exists)
                {
                    response.Errors.Add($"Function '{funcRequest.Name}' already exists");
                    continue;
                }

                var func = new Function
                {
                    Name = funcRequest.Name,
                    Description = funcRequest.Description,
                    Purpose = funcRequest.Purpose,
                    Category = funcRequest.Category,
                    Status = FunctionStatus.Active,
                    OrganizationId = organizationId
                };

                _dbContext.Functions.Add(func);
                await _dbContext.SaveChangesAsync();

                response.Created.Add(new OpsFunctionDto
                {
                    Id = func.Id,
                    Name = func.Name,
                    Description = func.Description,
                    Purpose = func.Purpose,
                    Category = func.Category,
                    Status = func.Status,
                    OrganizationId = func.OrganizationId,
                    CreatedAt = func.CreatedAt,
                    UpdatedAt = func.UpdatedAt,
                    CapabilityCount = 0,
                    RoleCount = 0
                });
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Error creating '{funcRequest.Name}': {ex.Message}");
            }
        }

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OpsFunctionDto>> UpdateFunction(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateOpsFunctionRequest request)
    {
        var func = await _dbContext.Functions
            .FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == organizationId);

        if (func == null)
            return NotFound();

        func.Name = request.Name;
        func.Description = request.Description;
        func.Purpose = request.Purpose;
        func.Category = request.Category;

        await _dbContext.SaveChangesAsync();

        return Ok(new OpsFunctionDto
        {
            Id = func.Id,
            Name = func.Name,
            Description = func.Description,
            Purpose = func.Purpose,
            Category = func.Category,
            Status = func.Status,
            OrganizationId = func.OrganizationId,
            CreatedAt = func.CreatedAt,
            UpdatedAt = func.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFunction(Guid organizationId, Guid id)
    {
        var func = await _dbContext.Functions
            .FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == organizationId);

        if (func == null)
            return NotFound();

        _dbContext.Functions.Remove(func);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
