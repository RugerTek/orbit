# Agent-to-Agent (A2A) Architecture Specification

## Overview

This specification defines the Agent-to-Agent communication system for OrbitOS, enabling Built-in specialist agents and Custom user-defined agents to collaborate through an orchestrated architecture.

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Built-in agent prompts | Base locked + custom instructions appended | Prevents breaking core functionality while allowing customization |
| Custom agents can call Built-in | Yes | Enables A2A without bloating custom agent context |
| Power users can invoke specialists directly | Yes | Flexibility for advanced users |
| Agent memory | Session-scoped (24h TTL) | Ship fast, design for persistent later |
| Agent naming | "Built-in" and "Custom" | Clear, simple terminology |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           USER INTERFACES                                │
├─────────────────────────────┬───────────────────────────────────────────┤
│     AI Assistant Widget     │           Group Chat (F003)               │
│     (floating, quick Q&A)   │     (multi-agent conversations)           │
└─────────────────────────────┴───────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                            ORCHESTRATOR                                  │
│  • Routes queries to appropriate agents                                  │
│  • Handles A2A calls (Custom → Built-in)                                │
│  • Synthesizes multi-agent responses                                     │
│  • Injects scoped context per agent type                                │
└─────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                            AGENT POOL                                    │
├────────────────────────────────┬────────────────────────────────────────┤
│         BUILT-IN               │              CUSTOM                     │
│  (scoped context, A2A-able)    │    (summary context, can call Built-in)│
│                                │                                         │
│  👥 People Expert              │    💼 CFO Advisor                       │
│  ⚙️ Process Expert             │    😈 Devil's Advocate                  │
│  🎯 Strategy Expert            │    🎨 Creative Director                 │
│  💰 Finance Expert             │    + User-created...                    │
└────────────────────────────────┴────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       SCOPED CONTEXT LOADERS                             │
│  • PeopleContextLoader: Resources, Roles, Functions, Org Chart          │
│  • ProcessContextLoader: Processes, Activities, Flows                   │
│  • StrategyContextLoader: Canvases, Goals, OKRs                         │
│  • FinanceContextLoader: (Future) Budgets, Revenue, Costs               │
└─────────────────────────────────────────────────────────────────────────┘
```

## Data Model Changes

### AiAgent Entity (Updated)

```csharp
public class AiAgent : BaseEntity
{
    // Existing fields...
    public required string Name { get; set; }
    public required string RoleTitle { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarColor { get; set; }
    public required AiProvider Provider { get; set; }
    public required string ModelId { get; set; }
    public required string ModelDisplayName { get; set; }
    public required string SystemPrompt { get; set; }
    public int MaxTokensPerResponse { get; set; } = 4096;
    public decimal Temperature { get; set; } = 0.7m;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    // Personality fields (existing)...
    public int Assertiveness { get; set; } = 50;
    public CommunicationStyle CommunicationStyle { get; set; }
    public ReactionTendency ReactionTendency { get; set; }
    public string? ExpertiseAreas { get; set; }
    public int SeniorityLevel { get; set; } = 3;
    public bool AsksQuestions { get; set; } = false;
    public bool GivesBriefAcknowledgments { get; set; } = true;

    // ===== NEW A2A FIELDS =====

    /// <summary>
    /// Agent type: BuiltIn (system-provided) or Custom (user-created)
    /// </summary>
    public AgentType AgentType { get; set; } = AgentType.Custom;

    /// <summary>
    /// For Built-in agents: identifies the specialist (people, process, strategy, finance)
    /// </summary>
    public string? SpecialistKey { get; set; }

    /// <summary>
    /// Context domains this agent has access to (for Built-in agents)
    /// Stored as JSON array: ["resources", "roles", "functions"]
    /// </summary>
    public string? ContextScopesJson { get; set; }

    /// <summary>
    /// The locked base prompt for Built-in agents (not editable by users)
    /// </summary>
    public string? BasePrompt { get; set; }

    /// <summary>
    /// User-provided custom instructions appended to BasePrompt
    /// </summary>
    public string? CustomInstructions { get; set; }

    /// <summary>
    /// Whether this agent can call Built-in agents for detailed data
    /// </summary>
    public bool CanCallBuiltInAgents { get; set; } = false;

    /// <summary>
    /// Whether the orchestrator can delegate to this agent
    /// </summary>
    public bool CanBeOrchestrated { get; set; } = false;

    /// <summary>
    /// System-provided agents cannot be deleted, only disabled
    /// </summary>
    public bool IsSystemProvided { get; set; } = false;

    // Multi-tenancy
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}

public enum AgentType
{
    BuiltIn,
    Custom
}
```

### Context Scopes

```csharp
public static class ContextScopes
{
    public const string Resources = "resources";
    public const string Roles = "roles";
    public const string Functions = "functions";
    public const string Processes = "processes";
    public const string Activities = "activities";
    public const string Canvases = "canvases";
    public const string Goals = "goals";
    public const string Partners = "partners";
    public const string Channels = "channels";
    public const string ValuePropositions = "value_propositions";
    public const string CustomerRelationships = "customer_relationships";
    public const string RevenueStreams = "revenue_streams";
}
```

## Built-in Agent Definitions

### People Expert

```json
{
  "name": "People Expert",
  "roleTitle": "Organizational People Specialist",
  "specialistKey": "people",
  "avatarColor": "#3B82F6",
  "contextScopes": ["resources", "roles", "functions"],
  "expertiseAreas": "people,team,roles,functions,capacity,org chart,hiring,skills,capabilities",
  "basePrompt": "You are an expert in organizational people management for OrbitOS. You have deep access to:\n- Team members (resources of type Person)\n- Roles and their assignments\n- Functions (capabilities/skills) and who has them\n- Organizational structure and reporting relationships\n- Capacity and allocation data\n\nYour responsibilities:\n- Analyze team capacity and identify overloaded people\n- Find single points of failure (functions with only one capable person)\n- Identify coverage gaps in roles and functions\n- Recommend hiring priorities based on capability gaps\n- Analyze organizational structure (span of control, depth)\n\nAlways provide data-driven insights with specific names and numbers. When asked about people or organizational issues, query the data first using the available tools.",
  "canBeOrchestrated": true,
  "isSystemProvided": true
}
```

### Process Expert

```json
{
  "name": "Process Expert",
  "roleTitle": "Business Process Specialist",
  "specialistKey": "process",
  "avatarColor": "#10B981",
  "contextScopes": ["processes", "activities", "resources", "roles"],
  "expertiseAreas": "processes,workflows,activities,automation,efficiency,bottlenecks,handoffs",
  "basePrompt": "You are an expert in business process management for OrbitOS. You have deep access to:\n- Business processes and their status\n- Activities within processes (manual, automated, hybrid, decision, handoff)\n- Process ownership and responsible roles\n- Activity assignments and dependencies\n\nYour responsibilities:\n- Analyze process efficiency and identify bottlenecks\n- Find activities assigned to overloaded people\n- Identify processes that need documentation or updates\n- Recommend automation opportunities\n- Map process dependencies and critical paths\n\nAlways provide actionable insights with specific process and activity names. Reference the people and roles involved.",
  "canBeOrchestrated": true,
  "isSystemProvided": true
}
```

### Strategy Expert

```json
{
  "name": "Strategy Expert",
  "roleTitle": "Strategic Planning Specialist",
  "specialistKey": "strategy",
  "avatarColor": "#F59E0B",
  "contextScopes": ["canvases", "goals", "partners", "channels", "value_propositions", "customer_relationships", "revenue_streams"],
  "expertiseAreas": "strategy,goals,OKRs,business model,canvas,partners,channels,value proposition,revenue",
  "basePrompt": "You are an expert in strategic planning for OrbitOS. You have deep access to:\n- Business Model Canvases and their blocks\n- Goals, objectives, and key results (OKRs)\n- Partners and strategic relationships\n- Channels and customer touchpoints\n- Value propositions and customer relationships\n- Revenue streams and pricing models\n\nYour responsibilities:\n- Analyze business model completeness and gaps\n- Track goal progress and identify off-track items\n- Evaluate partnership and channel strategies\n- Assess value proposition alignment with customer needs\n- Analyze revenue stream diversity and risks\n\nProvide strategic insights that connect operational data to business outcomes. Reference specific goals, partners, and canvas elements.",
  "canBeOrchestrated": true,
  "isSystemProvided": true
}
```

### Finance Expert

```json
{
  "name": "Finance Expert",
  "roleTitle": "Financial Analysis Specialist",
  "specialistKey": "finance",
  "avatarColor": "#EC4899",
  "contextScopes": ["revenue_streams", "resources", "goals"],
  "expertiseAreas": "finance,budget,revenue,costs,spending,ROI,headcount,financial planning",
  "basePrompt": "You are an expert in financial analysis for OrbitOS. You have access to:\n- Revenue streams and pricing models\n- Resource (people) costs and headcount\n- Goals with financial targets\n- Operational data that implies costs\n\nYour responsibilities:\n- Analyze revenue stream health and diversity\n- Estimate headcount costs based on team size\n- Identify cost optimization opportunities\n- Track financial goals and projections\n- Provide ROI analysis for operational decisions\n\nNote: Full financial data (budgets, actuals, forecasts) may not yet be available. Work with available data and make reasonable estimates. Flag when you're inferring vs using actual data.",
  "canBeOrchestrated": true,
  "isSystemProvided": true
}
```

## API Endpoints

### Updated AiAgents Endpoints

```
GET    /api/organizations/{orgId}/ai-agents
       Returns all agents, now including agentType, specialistKey, etc.
       Query params: ?type=builtin|custom

GET    /api/organizations/{orgId}/ai-agents/{agentId}
       Returns single agent with all new fields

POST   /api/organizations/{orgId}/ai-agents
       Creates a Custom agent (cannot create Built-in via API)

PUT    /api/organizations/{orgId}/ai-agents/{agentId}
       Updates agent. For Built-in: only customInstructions, isActive allowed

DELETE /api/organizations/{orgId}/ai-agents/{agentId}
       For Custom: soft delete. For Built-in: returns 400 (use isActive=false)

POST   /api/organizations/{orgId}/ai-agents/seed-builtin
       Admin endpoint to seed/reset Built-in agents for an organization
```

### New Orchestrator Endpoints

```
POST   /api/organizations/{orgId}/ai/orchestrate
       Main entry point for AI queries. Orchestrator routes to appropriate agents.
       Request: { message: string, conversationId?: string }
       Response: { message: string, agentsConsulted: string[], toolCalls?: [] }

POST   /api/organizations/{orgId}/ai/consult-specialist
       Direct specialist consultation (power user feature)
       Request: { specialistKey: string, query: string }
       Response: { message: string, contextUsed: string[] }
```

## Frontend Changes

### AI Agents Page (`/app/ai-agents/index.vue`)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  AI Agents                                              + Add Agent     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  BUILT-IN                                                         ⚙️    │
│  Pre-configured experts with access to your organization data.          │
│  Customize their prompts to fit your needs.                             │
│                                                                          │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐   │
│  │ 👥           │ │ ⚙️           │ │ 🎯           │ │ 💰           │   │
│  │ People       │ │ Process      │ │ Strategy     │ │ Finance      │   │
│  │ Expert       │ │ Expert       │ │ Expert       │ │ Expert       │   │
│  │              │ │              │ │              │ │              │   │
│  │ ✓ Active     │ │ ✓ Active     │ │ ✓ Active     │ │ ○ Inactive   │   │
│  │ [Edit]       │ │ [Edit]       │ │ [Edit]       │ │ [Edit]       │   │
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘   │
│                                                                          │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                          │
│  CUSTOM                                                                  │
│  AI personas you've created for conversations.                          │
│                                                                          │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                     │
│  │ 💼           │ │ 😈           │ │    +         │                     │
│  │ CFO Advisor  │ │ Devil's      │ │              │                     │
│  │              │ │ Advocate     │ │  Add Agent   │                     │
│  │ ✓ Active     │ │ ✓ Active     │ │              │                     │
│  │ [Edit][Del]  │ │ [Edit][Del]  │ │              │                     │
│  └──────────────┘ └──────────────┘ └──────────────┘                     │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Built-in Agent Edit Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Edit: People Expert                                               ✕    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Name                                                                    │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ People Expert                                                     │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Data Access                                                             │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ 📊 Resources  📋 Roles  🔧 Functions                              │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Base Behavior (read-only)                                         🔒   │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ You are an expert in organizational people management...         │  │
│  │ [truncated, expandable]                                          │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Custom Instructions (optional)                                          │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ - Always consider our Q4 hiring freeze                           │  │
│  │ - Flag any team with span of control > 8                         │  │
│  │ - We prefer flat hierarchies                                     │  │
│  │                                                                   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│  Add org-specific context and preferences                                │
│                                                                          │
│  Status  [━━━━━●] Active                                                │
│                                                                          │
│  [Cancel]                                                [Save Changes] │
└─────────────────────────────────────────────────────────────────────────┘
```

### Custom Agent Create/Edit Dialog (Updated)

Add new field:
```
┌─────────────────────────────────────────────────────────────────────────┐
│  Can consult Built-in agents                                            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  [━━━━━●] Yes                                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  When enabled, this agent can request detailed data analysis from       │
│  Built-in specialists (People, Process, Strategy, Finance).             │
└─────────────────────────────────────────────────────────────────────────┘
```

### Conversation Participant Picker (Updated)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Add Participants                                                        │
├─────────────────────────────────────────────────────────────────────────┤
│  🔍 Search agents...                                                     │
│                                                                          │
│  BUILT-IN                                                                │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ ✓ 👥 People Expert                                                │  │
│  │   Analyzes team capacity, roles, and capabilities                 │  │
│  ├───────────────────────────────────────────────────────────────────┤  │
│  │ ✓ ⚙️ Process Expert                                               │  │
│  │   Analyzes workflows, activities, and bottlenecks                 │  │
│  ├───────────────────────────────────────────────────────────────────┤  │
│  │ ○ 🎯 Strategy Expert                                              │  │
│  │   Business canvas, goals, and strategic planning                  │  │
│  ├───────────────────────────────────────────────────────────────────┤  │
│  │ ○ 💰 Finance Expert                                               │  │
│  │   Revenue streams, costs, and financial analysis                  │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  CUSTOM                                                                  │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ ✓ 💼 CFO Advisor                                                  │  │
│  │   Financial perspective and guidance                              │  │
│  ├───────────────────────────────────────────────────────────────────┤  │
│  │ ○ 😈 Devil's Advocate                                             │  │
│  │   Challenges assumptions and finds risks                          │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  [Cancel]                                                [Add Selected] │
└─────────────────────────────────────────────────────────────────────────┘
```

## Implementation Order

### Phase 1: Data Model & Migration
1. Add new fields to AiAgent entity
2. Create migration
3. Update DTOs and API endpoints
4. Create Built-in agent seeding service

### Phase 2: Context Loaders
1. Create IScopedContextLoader interface
2. Implement PeopleContextLoader
3. Implement ProcessContextLoader
4. Implement StrategyContextLoader
5. Implement FinanceContextLoader

### Phase 3: Orchestrator Service
1. Create IOrchestratorService interface
2. Implement query routing logic
3. Implement A2A call handling
4. Update AiChatService to use orchestrator

### Phase 4: Frontend Updates
1. Update useAiAgents composable with new types
2. Redesign AI Agents page with sections
3. Create Built-in agent edit dialog
4. Update Custom agent dialog
5. Update conversation participant picker

### Phase 5: Group Chat Integration
1. Update conversation creation to handle both types
2. Add visual distinction for agent types in chat
3. Ensure @mentions work for both types

## Future Phases (Design Hooks)

### +1: Health Signals Engine
- Pre-computed signals injected into Built-in agent context
- New service: IHealthSignalsService
- Signals stored in cache (Redis or in-memory)

### +2: Temporal Context
- Track entity changes over time
- Add "since last week" context to agents
- Trend detection service

### +3: Insight Graph
- Relationship traversal for root cause analysis
- Graph query service
- "This is blocked because..." explanations

### +4: Proactive Alerts
- Background analysis job
- Notification when significant changes detected
- Agent-generated insights without user prompting

### +5: Memory & Learning
- Session memory (24h TTL)
- Preference learning from user decisions
- Outcome tracking for recommendations
