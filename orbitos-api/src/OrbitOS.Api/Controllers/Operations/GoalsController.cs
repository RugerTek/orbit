using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers.Operations;

/// <summary>
/// Manages organizational goals and OKRs.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId}/operations/[controller]")]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
[Tags("Goals")]
public class GoalsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<GoalsController> _logger;

    public GoalsController(OrbitOSDbContext dbContext, ILogger<GoalsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<GoalDto>>> GetGoals(Guid organizationId, [FromQuery] GoalType? type = null, [FromQuery] bool? rootOnly = null)
    {
        var query = _dbContext.Goals
            .Include(g => g.Parent)
            .Include(g => g.Owner)
            .Include(g => g.Children)
            .Where(g => g.OrganizationId == organizationId);

        if (type.HasValue)
        {
            query = query.Where(g => g.GoalType == type.Value);
        }

        if (rootOnly == true)
        {
            query = query.Where(g => g.ParentId == null);
        }

        var goals = await query
            .Select(g => new GoalDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                GoalType = g.GoalType,
                Status = g.Status,
                TimeframeStart = g.TimeframeStart,
                TimeframeEnd = g.TimeframeEnd,
                TargetValue = g.TargetValue,
                CurrentValue = g.CurrentValue,
                Unit = g.Unit,
                Progress = g.TargetValue.HasValue && g.TargetValue.Value != 0
                    ? (g.CurrentValue ?? 0) / g.TargetValue.Value * 100
                    : null,
                OrganizationId = g.OrganizationId,
                ParentId = g.ParentId,
                ParentName = g.Parent != null ? g.Parent.Name : null,
                OwnerId = g.OwnerId,
                OwnerName = g.Owner != null ? g.Owner.Name : null,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt,
                Children = g.Children.Select(c => new GoalDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    GoalType = c.GoalType,
                    Status = c.Status,
                    TimeframeStart = c.TimeframeStart,
                    TimeframeEnd = c.TimeframeEnd,
                    TargetValue = c.TargetValue,
                    CurrentValue = c.CurrentValue,
                    Unit = c.Unit,
                    Progress = c.TargetValue.HasValue && c.TargetValue.Value != 0
                        ? (c.CurrentValue ?? 0) / c.TargetValue.Value * 100
                        : null,
                    OrganizationId = c.OrganizationId,
                    ParentId = c.ParentId,
                    OwnerId = c.OwnerId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList()
            })
            .OrderBy(g => g.Name)
            .ToListAsync();

        return Ok(goals);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GoalDto>> GetGoal(Guid organizationId, Guid id)
    {
        var goal = await _dbContext.Goals
            .Include(g => g.Parent)
            .Include(g => g.Owner)
            .Include(g => g.Children)
            .Where(g => g.Id == id && g.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (goal == null)
            return NotFound();

        return Ok(new GoalDto
        {
            Id = goal.Id,
            Name = goal.Name,
            Description = goal.Description,
            GoalType = goal.GoalType,
            Status = goal.Status,
            TimeframeStart = goal.TimeframeStart,
            TimeframeEnd = goal.TimeframeEnd,
            TargetValue = goal.TargetValue,
            CurrentValue = goal.CurrentValue,
            Unit = goal.Unit,
            Progress = goal.TargetValue.HasValue && goal.TargetValue.Value != 0
                ? (goal.CurrentValue ?? 0) / goal.TargetValue.Value * 100
                : null,
            OrganizationId = goal.OrganizationId,
            ParentId = goal.ParentId,
            ParentName = goal.Parent?.Name,
            OwnerId = goal.OwnerId,
            OwnerName = goal.Owner?.Name,
            CreatedAt = goal.CreatedAt,
            UpdatedAt = goal.UpdatedAt,
            Children = goal.Children.Select(c => new GoalDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                GoalType = c.GoalType,
                Status = c.Status,
                TimeframeStart = c.TimeframeStart,
                TimeframeEnd = c.TimeframeEnd,
                TargetValue = c.TargetValue,
                CurrentValue = c.CurrentValue,
                Unit = c.Unit,
                Progress = c.TargetValue.HasValue && c.TargetValue.Value != 0
                    ? (c.CurrentValue ?? 0) / c.TargetValue.Value * 100
                    : null,
                OrganizationId = c.OrganizationId,
                ParentId = c.ParentId,
                OwnerId = c.OwnerId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<GoalDto>> CreateGoal(
        Guid organizationId,
        [FromBody] CreateGoalRequest request)
    {
        var goal = new Goal
        {
            Name = request.Name,
            Description = request.Description,
            GoalType = request.GoalType,
            Status = request.Status,
            TimeframeStart = request.TimeframeStart,
            TimeframeEnd = request.TimeframeEnd,
            TargetValue = request.TargetValue,
            CurrentValue = request.CurrentValue,
            Unit = request.Unit,
            OrganizationId = organizationId,
            ParentId = request.ParentId,
            OwnerId = request.OwnerId
        };

        _dbContext.Goals.Add(goal);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGoal), new { organizationId, id = goal.Id },
            new GoalDto
            {
                Id = goal.Id,
                Name = goal.Name,
                Description = goal.Description,
                GoalType = goal.GoalType,
                Status = goal.Status,
                TimeframeStart = goal.TimeframeStart,
                TimeframeEnd = goal.TimeframeEnd,
                TargetValue = goal.TargetValue,
                CurrentValue = goal.CurrentValue,
                Unit = goal.Unit,
                Progress = goal.TargetValue.HasValue && goal.TargetValue.Value != 0
                    ? (goal.CurrentValue ?? 0) / goal.TargetValue.Value * 100
                    : null,
                OrganizationId = goal.OrganizationId,
                ParentId = goal.ParentId,
                OwnerId = goal.OwnerId,
                CreatedAt = goal.CreatedAt,
                UpdatedAt = goal.UpdatedAt
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GoalDto>> UpdateGoal(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateGoalRequest request)
    {
        var goal = await _dbContext.Goals
            .Include(g => g.Parent)
            .Include(g => g.Owner)
            .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == organizationId);

        if (goal == null)
            return NotFound();

        goal.Name = request.Name;
        goal.Description = request.Description;
        goal.GoalType = request.GoalType;
        goal.Status = request.Status;
        goal.TimeframeStart = request.TimeframeStart;
        goal.TimeframeEnd = request.TimeframeEnd;
        goal.TargetValue = request.TargetValue;
        goal.CurrentValue = request.CurrentValue;
        goal.Unit = request.Unit;
        goal.ParentId = request.ParentId;
        goal.OwnerId = request.OwnerId;

        await _dbContext.SaveChangesAsync();

        return Ok(new GoalDto
        {
            Id = goal.Id,
            Name = goal.Name,
            Description = goal.Description,
            GoalType = goal.GoalType,
            Status = goal.Status,
            TimeframeStart = goal.TimeframeStart,
            TimeframeEnd = goal.TimeframeEnd,
            TargetValue = goal.TargetValue,
            CurrentValue = goal.CurrentValue,
            Unit = goal.Unit,
            Progress = goal.TargetValue.HasValue && goal.TargetValue.Value != 0
                ? (goal.CurrentValue ?? 0) / goal.TargetValue.Value * 100
                : null,
            OrganizationId = goal.OrganizationId,
            ParentId = goal.ParentId,
            ParentName = goal.Parent?.Name,
            OwnerId = goal.OwnerId,
            OwnerName = goal.Owner?.Name,
            CreatedAt = goal.CreatedAt,
            UpdatedAt = goal.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGoal(Guid organizationId, Guid id)
    {
        var goal = await _dbContext.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == organizationId);

        if (goal == null)
            return NotFound();

        // Soft delete - CLAUDE.md compliance
        goal.SoftDelete();
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
