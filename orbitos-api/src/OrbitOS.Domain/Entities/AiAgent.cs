using OrbitOS.Domain.Common;

namespace OrbitOS.Domain.Entities;

/// <summary>
/// Configurable AI agent with specific model, role, and capabilities
/// that can participate in multi-agent conversations
/// </summary>
public class AiAgent : BaseEntity
{
    public required string Name { get; set; }
    public required string RoleTitle { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarColor { get; set; }

    /// <summary>
    /// AI provider: anthropic, openai, google
    /// </summary>
    public required AiProvider Provider { get; set; }

    /// <summary>
    /// Specific model ID (e.g., 'claude-sonnet-4-20250514', 'gpt-4o', 'gemini-pro')
    /// </summary>
    public required string ModelId { get; set; }

    /// <summary>
    /// Display name for model badge (e.g., 'Claude Sonnet', 'GPT-4', 'Gemini')
    /// </summary>
    public required string ModelDisplayName { get; set; }

    /// <summary>
    /// System prompt that defines the agent's personality, expertise, and behavior
    /// </summary>
    public required string SystemPrompt { get; set; }

    /// <summary>
    /// Maximum tokens the agent can use per response
    /// </summary>
    public int MaxTokensPerResponse { get; set; } = 4096;

    /// <summary>
    /// Model temperature setting (0.0-2.0)
    /// </summary>
    public decimal Temperature { get; set; } = 0.7m;

    /// <summary>
    /// Whether the agent is active and can participate in conversations
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order in the agents list
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // ===== Personality & Meeting Behavior (for Emergent mode) =====

    /// <summary>
    /// How likely the agent is to speak up (0-100).
    /// High: speaks first, volunteers opinions. Low: waits, speaks when asked.
    /// </summary>
    public int Assertiveness { get; set; } = 50;

    /// <summary>
    /// Communication style: Formal, Casual, Direct, Diplomatic, Analytical
    /// </summary>
    public CommunicationStyle CommunicationStyle { get; set; } = CommunicationStyle.Formal;

    /// <summary>
    /// How the agent typically reacts: Supportive, Critical, Balanced, DevilsAdvocate, ConsensusBuilder
    /// </summary>
    public ReactionTendency ReactionTendency { get; set; } = ReactionTendency.Balanced;

    /// <summary>
    /// Areas of expertise for stake detection (comma-separated keywords).
    /// E.g., "finance,budget,revenue,costs" or "hr,hiring,culture,people"
    /// </summary>
    public string? ExpertiseAreas { get; set; }

    /// <summary>
    /// Seniority level affects deference patterns (1-5, where 5 is most senior)
    /// </summary>
    public int SeniorityLevel { get; set; } = 3;

    /// <summary>
    /// Whether this agent tends to ask clarifying questions
    /// </summary>
    public bool AsksQuestions { get; set; } = false;

    /// <summary>
    /// Whether this agent gives brief acknowledgments ("Good point", "I agree")
    /// </summary>
    public bool GivesBriefAcknowledgments { get; set; } = true;

    // Multi-tenancy
    public Guid OrganizationId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}

public enum AiProvider
{
    Anthropic,
    OpenAI,
    Google
}

/// <summary>
/// Communication style affects tone and formality of responses
/// </summary>
public enum CommunicationStyle
{
    /// <summary>Professional, structured responses</summary>
    Formal,
    /// <summary>Friendly, conversational tone</summary>
    Casual,
    /// <summary>Straight to the point, no fluff</summary>
    Direct,
    /// <summary>Tactful, considers feelings and politics</summary>
    Diplomatic,
    /// <summary>Data-driven, methodical approach</summary>
    Analytical
}

/// <summary>
/// How the agent typically reacts to others' points
/// </summary>
public enum ReactionTendency
{
    /// <summary>Tends to agree and build on ideas</summary>
    Supportive,
    /// <summary>Looks for flaws and risks</summary>
    Critical,
    /// <summary>Weighs pros and cons objectively</summary>
    Balanced,
    /// <summary>Intentionally challenges assumptions</summary>
    DevilsAdvocate,
    /// <summary>Seeks common ground and alignment</summary>
    ConsensusBuilder
}
