using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers;

/// <summary>
/// Controller for AI usage analytics - token consumption, costs, and model usage
/// </summary>
[ApiController]
[AllowAnonymous] // TODO: Re-enable [Authorize] for production
public class UsageAnalyticsController : ControllerBase
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<UsageAnalyticsController> _logger;

    public UsageAnalyticsController(OrbitOSDbContext dbContext, ILogger<UsageAnalyticsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get usage analytics for a specific organization
    /// </summary>
    [HttpGet("api/organizations/{organizationId}/usage")]
    public async Task<ActionResult<OrganizationUsageDto>> GetOrganizationUsage(
        Guid organizationId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        // Default to last 30 days if no dates provided
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddDays(-30);

        // Get conversation-level stats
        var conversations = await _dbContext.Conversations
            .Where(c => c.OrganizationId == organizationId)
            .Where(c => c.CreatedAt >= start && c.CreatedAt <= end)
            .Select(c => new
            {
                c.Id,
                c.TotalTokens,
                c.TotalCostCents,
                c.MessageCount,
                c.AiResponseCount,
                c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Get message-level stats with model breakdown
        var messages = await _dbContext.Messages
            .Include(m => m.Conversation)
            .Include(m => m.SenderAiAgent)
            .Where(m => m.Conversation.OrganizationId == organizationId)
            .Where(m => m.SenderType == SenderType.Ai)
            .Where(m => m.CreatedAt >= start && m.CreatedAt <= end)
            .Select(m => new
            {
                m.TokensUsed,
                m.CostCents,
                m.ModelUsed,
                Provider = m.SenderAiAgent != null ? (AiProvider?)m.SenderAiAgent.Provider : null,
                Name = m.SenderAiAgent != null ? m.SenderAiAgent.Name : "Unknown",
                m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Calculate totals
        var totalTokens = messages.Sum(m => m.TokensUsed ?? 0);
        var totalCostCents = messages.Sum(m => m.CostCents ?? 0);
        var totalResponses = messages.Count;

        // Group by model
        var byModel = messages
            .Where(m => !string.IsNullOrEmpty(m.ModelUsed))
            .GroupBy(m => m.ModelUsed)
            .Select(g => new ModelUsageDto
            {
                ModelId = g.Key!,
                Provider = g.First().Provider?.ToString() ?? "Unknown",
                TokensUsed = g.Sum(m => m.TokensUsed ?? 0),
                CostCents = g.Sum(m => m.CostCents ?? 0),
                ResponseCount = g.Count()
            })
            .OrderByDescending(m => m.TokensUsed)
            .ToList();

        // Group by agent
        var byAgent = messages
            .GroupBy(m => m.Name)
            .Select(g => new AgentUsageDto
            {
                AgentName = g.Key,
                TokensUsed = g.Sum(m => m.TokensUsed ?? 0),
                CostCents = g.Sum(m => m.CostCents ?? 0),
                ResponseCount = g.Count()
            })
            .OrderByDescending(a => a.TokensUsed)
            .ToList();

        // Daily breakdown
        var dailyUsage = messages
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new DailyUsageDto
            {
                Date = g.Key,
                TokensUsed = g.Sum(m => m.TokensUsed ?? 0),
                CostCents = g.Sum(m => m.CostCents ?? 0),
                ResponseCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Ok(new OrganizationUsageDto
        {
            OrganizationId = organizationId,
            StartDate = start,
            EndDate = end,
            TotalTokens = totalTokens,
            TotalCostCents = totalCostCents,
            TotalCostDollars = totalCostCents / 100.0m,
            TotalResponses = totalResponses,
            ConversationCount = conversations.Count,
            ByModel = byModel,
            ByAgent = byAgent,
            DailyUsage = dailyUsage
        });
    }

    /// <summary>
    /// Get global usage analytics across all organizations (Super Admin only)
    /// </summary>
    [HttpGet("api/admin/usage")]
    public async Task<ActionResult<GlobalUsageDto>> GetGlobalUsage(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        // Default to last 30 days if no dates provided
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddDays(-30);

        // Get all messages with usage data
        var messages = await _dbContext.Messages
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Organization)
            .Include(m => m.SenderAiAgent)
            .Where(m => m.SenderType == SenderType.Ai)
            .Where(m => m.CreatedAt >= start && m.CreatedAt <= end)
            .Select(m => new
            {
                m.TokensUsed,
                m.CostCents,
                m.ModelUsed,
                Provider = m.SenderAiAgent != null ? (AiProvider?)m.SenderAiAgent.Provider : null,
                OrganizationId = m.Conversation.OrganizationId,
                OrganizationName = m.Conversation.Organization != null ? m.Conversation.Organization.Name : "Unknown",
                m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Calculate totals
        var totalTokens = messages.Sum(m => m.TokensUsed ?? 0);
        var totalCostCents = messages.Sum(m => m.CostCents ?? 0);
        var totalResponses = messages.Count;

        // Group by organization
        var byOrganization = messages
            .GroupBy(m => new { m.OrganizationId, m.OrganizationName })
            .Select(g => new OrganizationUsageSummaryDto
            {
                OrganizationId = g.Key.OrganizationId,
                OrganizationName = g.Key.OrganizationName,
                TokensUsed = g.Sum(m => m.TokensUsed ?? 0),
                CostCents = g.Sum(m => m.CostCents ?? 0),
                CostDollars = g.Sum(m => m.CostCents ?? 0) / 100.0m,
                ResponseCount = g.Count()
            })
            .OrderByDescending(o => o.TokensUsed)
            .ToList();

        // Group by model
        var byModel = messages
            .Where(m => !string.IsNullOrEmpty(m.ModelUsed))
            .GroupBy(m => m.ModelUsed)
            .Select(g => new ModelUsageDto
            {
                ModelId = g.Key!,
                Provider = g.First().Provider?.ToString() ?? "Unknown",
                TokensUsed = g.Sum(m => m.TokensUsed ?? 0),
                CostCents = g.Sum(m => m.CostCents ?? 0),
                ResponseCount = g.Count()
            })
            .OrderByDescending(m => m.TokensUsed)
            .ToList();

        // Group by provider
        var byProvider = messages
            .GroupBy(m => m.Provider?.ToString() ?? "Unknown")
            .Select(g => new ProviderUsageDto
            {
                Provider = g.Key,
                TokensUsed = g.Sum(m => m.TokensUsed ?? 0),
                CostCents = g.Sum(m => m.CostCents ?? 0),
                CostDollars = g.Sum(m => m.CostCents ?? 0) / 100.0m,
                ResponseCount = g.Count()
            })
            .OrderByDescending(p => p.TokensUsed)
            .ToList();

        // Daily breakdown
        var dailyUsage = messages
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new DailyUsageDto
            {
                Date = g.Key,
                TokensUsed = g.Sum(m => m.TokensUsed ?? 0),
                CostCents = g.Sum(m => m.CostCents ?? 0),
                ResponseCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Ok(new GlobalUsageDto
        {
            StartDate = start,
            EndDate = end,
            TotalTokens = totalTokens,
            TotalCostCents = totalCostCents,
            TotalCostDollars = totalCostCents / 100.0m,
            TotalResponses = totalResponses,
            OrganizationCount = byOrganization.Count,
            ByOrganization = byOrganization,
            ByModel = byModel,
            ByProvider = byProvider,
            DailyUsage = dailyUsage
        });
    }
}

// DTOs

public class OrganizationUsageDto
{
    public Guid OrganizationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long TotalTokens { get; set; }
    public int TotalCostCents { get; set; }
    public decimal TotalCostDollars { get; set; }
    public int TotalResponses { get; set; }
    public int ConversationCount { get; set; }
    public List<ModelUsageDto> ByModel { get; set; } = new();
    public List<AgentUsageDto> ByAgent { get; set; } = new();
    public List<DailyUsageDto> DailyUsage { get; set; } = new();
}

public class GlobalUsageDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long TotalTokens { get; set; }
    public int TotalCostCents { get; set; }
    public decimal TotalCostDollars { get; set; }
    public int TotalResponses { get; set; }
    public int OrganizationCount { get; set; }
    public List<OrganizationUsageSummaryDto> ByOrganization { get; set; } = new();
    public List<ModelUsageDto> ByModel { get; set; } = new();
    public List<ProviderUsageDto> ByProvider { get; set; } = new();
    public List<DailyUsageDto> DailyUsage { get; set; } = new();
}

public class OrganizationUsageSummaryDto
{
    public Guid OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public long TokensUsed { get; set; }
    public int CostCents { get; set; }
    public decimal CostDollars { get; set; }
    public int ResponseCount { get; set; }
}

public class ModelUsageDto
{
    public required string ModelId { get; set; }
    public required string Provider { get; set; }
    public long TokensUsed { get; set; }
    public int CostCents { get; set; }
    public int ResponseCount { get; set; }
}

public class AgentUsageDto
{
    public required string AgentName { get; set; }
    public long TokensUsed { get; set; }
    public int CostCents { get; set; }
    public int ResponseCount { get; set; }
}

public class ProviderUsageDto
{
    public required string Provider { get; set; }
    public long TokensUsed { get; set; }
    public int CostCents { get; set; }
    public decimal CostDollars { get; set; }
    public int ResponseCount { get; set; }
}

public class DailyUsageDto
{
    public DateTime Date { get; set; }
    public long TokensUsed { get; set; }
    public int CostCents { get; set; }
    public int ResponseCount { get; set; }
}
