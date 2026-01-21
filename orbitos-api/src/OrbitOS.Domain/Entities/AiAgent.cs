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
