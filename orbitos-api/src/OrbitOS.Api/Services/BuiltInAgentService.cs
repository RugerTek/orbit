using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Services;

/// <summary>
/// Service for managing Built-in AI agents
/// </summary>
public interface IBuiltInAgentService
{
    /// <summary>
    /// Seeds the default Built-in agents for an organization (if not already present)
    /// </summary>
    Task SeedBuiltInAgentsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Built-in agent definitions
    /// </summary>
    IReadOnlyList<BuiltInAgentDefinition> GetBuiltInAgentDefinitions();
}

public class BuiltInAgentService : IBuiltInAgentService
{
    private readonly OrbitOSDbContext _dbContext;
    private readonly ILogger<BuiltInAgentService> _logger;
    private static readonly SemaphoreSlim _seedLock = new(1, 1);

    private static readonly List<BuiltInAgentDefinition> _builtInAgents = new()
    {
        new BuiltInAgentDefinition
        {
            SpecialistKey = "people",
            Name = "People Expert",
            RoleTitle = "Organizational People Specialist",
            AvatarColor = "#3B82F6", // Blue
            ContextScopes = new[] { ContextScopes.Resources, ContextScopes.Roles, ContextScopes.Functions },
            ExpertiseAreas = "people,team,roles,functions,capacity,org chart,hiring,skills,capabilities,headcount,staff",
            BasePrompt = @"You are an expert in organizational people management for OrbitOS. You have deep access to:
- Team members (resources of type Person) with their status and capacity
- Roles and their assignments (who fills which role, allocation percentages)
- Functions (capabilities/skills) and who has them at what level
- Organizational structure and reporting relationships
- Capacity and allocation data

Your responsibilities:
- Analyze team capacity and identify overloaded people (>90% allocated)
- Find single points of failure (functions with only one capable person)
- Identify coverage gaps in roles and functions
- Recommend hiring priorities based on capability gaps
- Analyze organizational structure (span of control, hierarchy depth)
- Track role assignments and identify unfilled positions

Always provide data-driven insights with specific names, numbers, and percentages. When asked about people or organizational issues, query the data first using the available tools. Be specific about who is affected and why.",
            Assertiveness = 70,
            CommunicationStyle = CommunicationStyle.Analytical,
            ReactionTendency = ReactionTendency.Balanced,
            SeniorityLevel = 4
        },
        new BuiltInAgentDefinition
        {
            SpecialistKey = "process",
            Name = "Process Expert",
            RoleTitle = "Business Process Specialist",
            AvatarColor = "#10B981", // Emerald
            ContextScopes = new[] { ContextScopes.Processes, ContextScopes.Activities, ContextScopes.Resources, ContextScopes.Roles },
            ExpertiseAreas = "processes,workflows,activities,automation,efficiency,bottlenecks,handoffs,operations,procedures",
            BasePrompt = @"You are an expert in business process management for OrbitOS. You have deep access to:
- Business processes and their status (Active, Draft, Deprecated)
- Activities within processes (Manual, Automated, Hybrid, Decision, Handoff types)
- Process ownership and responsible roles
- Activity assignments and dependencies
- Process flows and sequences

Your responsibilities:
- Analyze process efficiency and identify bottlenecks
- Find activities assigned to overloaded people
- Identify processes that need documentation or updates
- Recommend automation opportunities for manual activities
- Map process dependencies and critical paths
- Assess handoff points and potential delays
- Identify deprecated or draft processes needing attention

Always provide actionable insights with specific process and activity names. Reference the people and roles involved. Quantify impact where possible (e.g., '3 activities blocked by capacity issues').",
            Assertiveness = 65,
            CommunicationStyle = CommunicationStyle.Direct,
            ReactionTendency = ReactionTendency.Balanced,
            SeniorityLevel = 4
        },
        new BuiltInAgentDefinition
        {
            SpecialistKey = "strategy",
            Name = "Strategy Expert",
            RoleTitle = "Strategic Planning Specialist",
            AvatarColor = "#F59E0B", // Amber
            ContextScopes = new[] { ContextScopes.Canvases, ContextScopes.Goals, ContextScopes.Partners, ContextScopes.Channels, ContextScopes.ValuePropositions, ContextScopes.CustomerRelationships, ContextScopes.RevenueStreams },
            ExpertiseAreas = "strategy,goals,OKRs,business model,canvas,partners,channels,value proposition,revenue,market,competitive",
            BasePrompt = @"You are an expert in strategic planning for OrbitOS. You have deep access to:
- Business Model Canvases and their blocks
- Goals, objectives, and key results (OKRs) with progress tracking
- Partners and strategic relationships
- Channels and customer touchpoints
- Value propositions and customer relationships
- Revenue streams and pricing models

Your responsibilities:
- Analyze business model completeness and identify gaps
- Track goal progress and identify off-track items
- Evaluate partnership and channel strategies
- Assess value proposition alignment with customer needs
- Analyze revenue stream diversity and risks
- Connect operational data to strategic outcomes
- Identify strategic misalignments

Provide strategic insights that connect operational realities to business objectives. Reference specific goals, partners, and canvas elements. Think both short-term (quarterly) and long-term (annual+).",
            Assertiveness = 75,
            CommunicationStyle = CommunicationStyle.Diplomatic,
            ReactionTendency = ReactionTendency.Balanced,
            SeniorityLevel = 5
        },
        new BuiltInAgentDefinition
        {
            SpecialistKey = "finance",
            Name = "Finance Expert",
            RoleTitle = "Financial Analysis Specialist",
            AvatarColor = "#EC4899", // Pink
            ContextScopes = new[] { ContextScopes.RevenueStreams, ContextScopes.Resources, ContextScopes.Goals },
            ExpertiseAreas = "finance,budget,revenue,costs,spending,ROI,headcount,financial planning,pricing,profitability",
            BasePrompt = @"You are an expert in financial analysis for OrbitOS. You have access to:
- Revenue streams and pricing models
- Resource (people) data for headcount analysis
- Goals with financial targets
- Operational data that implies costs

Your responsibilities:
- Analyze revenue stream health and diversity
- Estimate headcount costs based on team size and roles
- Identify cost optimization opportunities
- Track financial goals and projections
- Provide ROI analysis for operational decisions
- Flag financial risks from operational data

Important: Full financial data (budgets, actuals, forecasts) may not yet be available in OrbitOS. Work with available data and make reasonable estimates based on industry standards. Always clearly distinguish between actual data and estimates/inferences.

When discussing costs, use reasonable industry estimates for roles if actual salary data isn't available. Focus on relative comparisons and trends rather than absolute numbers when data is limited.",
            Assertiveness = 60,
            CommunicationStyle = CommunicationStyle.Analytical,
            ReactionTendency = ReactionTendency.Critical,
            SeniorityLevel = 4
        }
    };

    public BuiltInAgentService(OrbitOSDbContext dbContext, ILogger<BuiltInAgentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public IReadOnlyList<BuiltInAgentDefinition> GetBuiltInAgentDefinitions() => _builtInAgents;

    public async Task SeedBuiltInAgentsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Acquire lock to prevent concurrent seeding race conditions
        await _seedLock.WaitAsync(cancellationToken);
        try
        {
            await SeedBuiltInAgentsInternalAsync(organizationId, cancellationToken);
        }
        finally
        {
            _seedLock.Release();
        }
    }

    private async Task SeedBuiltInAgentsInternalAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("SeedBuiltInAgentsInternalAsync called for org {OrgId}", organizationId);

        // Get all existing agent names to prevent duplicate name conflicts
        // IMPORTANT: Use IgnoreQueryFilters to include soft-deleted agents, as the unique
        // constraint in the DB doesn't care about soft-delete status
        var existingAgents = await _dbContext.AiAgents
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == organizationId)
            .Select(a => new { a.SpecialistKey, a.Name, a.DeletedAt })
            .ToListAsync(cancellationToken);

        var activeAgents = existingAgents.Where(a => a.DeletedAt == null).ToList();
        var deletedAgents = existingAgents.Where(a => a.DeletedAt != null).ToList();

        _logger.LogInformation("Found {Active} active agents and {Deleted} soft-deleted agents",
            activeAgents.Count, deletedAgents.Count);

        var existingNames = existingAgents.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingKeys = existingAgents
            .Where(a => !string.IsNullOrEmpty(a.SpecialistKey))
            .Select(a => a.SpecialistKey)
            .ToHashSet();

        // Check if any Built-in agents are soft-deleted and need to be restored
        var deletedBuiltInNames = deletedAgents
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var builtInNamesToRestore = _builtInAgents
            .Where(def => deletedBuiltInNames.Contains(def.Name))
            .Select(def => def.Name)
            .ToList();

        // Also check for active agents with Built-in names that need their fields updated
        var activeBuiltInNames = activeAgents
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var builtInNamesToUpdate = _builtInAgents
            .Where(def => activeBuiltInNames.Contains(def.Name))
            .Select(def => def.Name)
            .ToList();

        // Update existing active Built-in agents to ensure they have all required fields
        if (builtInNamesToUpdate.Count > 0)
        {
            var agentsToUpdate = await _dbContext.AiAgents
                .Where(a => a.OrganizationId == organizationId &&
                            builtInNamesToUpdate.Contains(a.Name))
                .ToListAsync(cancellationToken);

            foreach (var agent in agentsToUpdate)
            {
                var def = _builtInAgents.FirstOrDefault(d =>
                    d.Name.Equals(agent.Name, StringComparison.OrdinalIgnoreCase));

                if (def != null)
                {
                    // Always update Built-in agents to ensure they have all required fields
                    agent.AgentType = AgentType.BuiltIn;
                    agent.IsSystemProvided = true;
                    agent.SpecialistKey = def.SpecialistKey;
                    agent.ContextScopesJson = JsonSerializer.Serialize(def.ContextScopes);
                    agent.BasePrompt = def.BasePrompt;
                    agent.CanBeOrchestrated = true;
                    agent.CanCallBuiltInAgents = false;
                    _logger.LogInformation("Updated agent: {Name} with specialistKey: {Key}", agent.Name, def.SpecialistKey);
                }
            }

            // Save updates to existing agents
            if (agentsToUpdate.Any())
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved updates to {Count} Built-in agents", agentsToUpdate.Count);
            }
        }

        if (builtInNamesToRestore.Count > 0)
        {
            _logger.LogInformation("Restoring {Count} soft-deleted Built-in agents: {Names}",
                builtInNamesToRestore.Count, string.Join(", ", builtInNamesToRestore));

            // Restore soft-deleted Built-in agents
            var agentsToRestore = await _dbContext.AiAgents
                .IgnoreQueryFilters()
                .Where(a => a.OrganizationId == organizationId &&
                            a.DeletedAt != null &&
                            builtInNamesToRestore.Contains(a.Name))
                .ToListAsync(cancellationToken);

            foreach (var agent in agentsToRestore)
            {
                // Find the matching Built-in definition
                var def = _builtInAgents.FirstOrDefault(d =>
                    d.Name.Equals(agent.Name, StringComparison.OrdinalIgnoreCase));

                if (def != null)
                {
                    // Restore with full Built-in agent configuration
                    agent.DeletedAt = null;
                    agent.AgentType = AgentType.BuiltIn;
                    agent.IsSystemProvided = true;
                    agent.SpecialistKey = def.SpecialistKey;
                    agent.ContextScopesJson = JsonSerializer.Serialize(def.ContextScopes);
                    agent.BasePrompt = def.BasePrompt;
                    agent.CanBeOrchestrated = true;
                    agent.CanCallBuiltInAgents = false;
                    _logger.LogInformation("Restored agent: {Name} with specialistKey: {Key}", agent.Name, def.SpecialistKey);
                }
            }
        }

        // Only create agents that don't exist by specialist key OR by name
        var toCreate = _builtInAgents
            .Where(def => !existingKeys.Contains(def.SpecialistKey) && !existingNames.Contains(def.Name))
            .ToList();

        _logger.LogInformation("Will create {Count} new Built-in agents: {Names}",
            toCreate.Count,
            string.Join(", ", toCreate.Select(d => d.Name)));

        if (toCreate.Count == 0 && builtInNamesToRestore.Count == 0)
        {
            _logger.LogDebug("All Built-in agents already exist for organization {OrgId}", organizationId);
            return;
        }

        if (toCreate.Count == 0)
        {
            // Only restoring, no new agents to create
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored {Count} Built-in agents for organization {OrgId}", builtInNamesToRestore.Count, organizationId);
            return;
        }

        foreach (var def in toCreate)
        {
            var agent = new AiAgent
            {
                Name = def.Name,
                RoleTitle = def.RoleTitle,
                AvatarColor = def.AvatarColor,
                Provider = AiProvider.Anthropic,
                ModelId = "claude-sonnet-4-20250514",
                ModelDisplayName = "Claude Sonnet 4",
                SystemPrompt = def.BasePrompt, // Will be combined with CustomInstructions at runtime
                MaxTokensPerResponse = 4096,
                Temperature = 0.7m,
                IsActive = true,
                SortOrder = _builtInAgents.IndexOf(def),

                // Personality
                Assertiveness = def.Assertiveness,
                CommunicationStyle = def.CommunicationStyle,
                ReactionTendency = def.ReactionTendency,
                ExpertiseAreas = def.ExpertiseAreas,
                SeniorityLevel = def.SeniorityLevel,
                AsksQuestions = true,
                GivesBriefAcknowledgments = false,

                // A2A fields
                AgentType = AgentType.BuiltIn,
                SpecialistKey = def.SpecialistKey,
                ContextScopesJson = JsonSerializer.Serialize(def.ContextScopes),
                BasePrompt = def.BasePrompt,
                CustomInstructions = null,
                CanCallBuiltInAgents = false, // Built-in agents don't call other built-in agents
                CanBeOrchestrated = true,
                IsSystemProvided = true,

                OrganizationId = organizationId
            };

            _dbContext.AiAgents.Add(agent);
        }

        try
        {
            var savedCount = await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SUCCESS: Seeded {Count} Built-in agents for organization {OrgId} (DB reported {Saved} changes)",
                toCreate.Count, organizationId, savedCount);
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "no inner exception";
            _logger.LogWarning("DbUpdateException during seeding: {Message}", innerMessage);

            if (innerMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                innerMessage.Contains("IX_AiAgents_OrganizationId_Name", StringComparison.OrdinalIgnoreCase))
            {
                // Race condition - another request already seeded these agents
                _logger.LogWarning("Built-in agents already seeded by another request for org {OrgId}", organizationId);
                // Detach all tracked entities to clean up the context
                foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                {
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
            else
            {
                // Re-throw unexpected exceptions
                throw;
            }
        }
    }
}

/// <summary>
/// Definition for a Built-in agent type
/// </summary>
public class BuiltInAgentDefinition
{
    public required string SpecialistKey { get; set; }
    public required string Name { get; set; }
    public required string RoleTitle { get; set; }
    public required string AvatarColor { get; set; }
    public required string[] ContextScopes { get; set; }
    public required string ExpertiseAreas { get; set; }
    public required string BasePrompt { get; set; }
    public int Assertiveness { get; set; } = 50;
    public CommunicationStyle CommunicationStyle { get; set; } = CommunicationStyle.Formal;
    public ReactionTendency ReactionTendency { get; set; } = ReactionTendency.Balanced;
    public int SeniorityLevel { get; set; } = 3;
}
