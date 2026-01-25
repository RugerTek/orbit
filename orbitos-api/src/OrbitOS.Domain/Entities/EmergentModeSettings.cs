namespace OrbitOS.Domain.Entities;

/// <summary>
/// Configuration for Emergent conversation mode where agents self-moderate
/// based on relevance scoring
/// </summary>
public class EmergentModeSettings
{
    /// <summary>
    /// Minimum relevance score (0-100) required for an agent to respond.
    /// Agents scoring below this threshold will pass/stay silent.
    /// Default: 70
    /// </summary>
    public int RelevanceThreshold { get; set; } = 70;

    /// <summary>
    /// Maximum number of reaction rounds after the initial response round.
    /// Each round allows agents to react to new responses.
    /// Default: 2 (so up to 3 total rounds including initial)
    /// </summary>
    public int MaxRoundsPerMessage { get; set; } = 2;

    /// <summary>
    /// Maximum number of agent responses allowed per round.
    /// Prevents runaway conversations with many agents.
    /// Default: 3
    /// </summary>
    public int MaxResponsesPerRound { get; set; } = 3;

    /// <summary>
    /// If true, use a cheap/fast model for relevance scoring to save costs.
    /// The actual response will still use the agent's configured model.
    /// Default: true
    /// </summary>
    public bool UseCheapModelForScoring { get; set; } = true;

    /// <summary>
    /// Model ID to use for relevance scoring when UseCheapModelForScoring is true.
    /// If null, defaults to gemini-2.5-flash (cheapest option).
    /// </summary>
    public string? ScoringModelId { get; set; }

    /// <summary>
    /// Provider for the scoring model (anthropic, openai, google).
    /// If null, defaults to google.
    /// </summary>
    public string? ScoringModelProvider { get; set; }

    /// <summary>
    /// If true, agents are prompted to only speak if they have unique insights
    /// not already covered by other responses.
    /// Default: true
    /// </summary>
    public bool RequireUniqueInsight { get; set; } = true;

    /// <summary>
    /// If true, show relevance scores in the UI for transparency.
    /// Default: true
    /// </summary>
    public bool ShowRelevanceScores { get; set; } = true;

    /// <summary>
    /// Minimum delay (in milliseconds) between agent responses for visual effect.
    /// Helps create a more natural conversation flow in the UI.
    /// Default: 500
    /// </summary>
    public int ResponseDelayMs { get; set; } = 500;

    /// <summary>
    /// Whether to allow agents to respond multiple times in the same message thread
    /// (if they have new insights after hearing others speak).
    /// Default: true (for natural meeting-like behavior)
    /// </summary>
    public bool AllowMultipleResponses { get; set; } = true;

    /// <summary>
    /// Whether to show brief acknowledgments from agents who don't have substantive input
    /// (e.g., "I don't have anything to add" or "Good points, nothing from my end").
    /// Default: true (for natural meeting-like behavior)
    /// </summary>
    public bool ShowBriefAcknowledgments { get; set; } = true;

    /// <summary>
    /// Threshold below which agents will give brief acknowledgments instead of full responses.
    /// Only applies when ShowBriefAcknowledgments is true.
    /// Default: 40
    /// </summary>
    public int AcknowledgmentThreshold { get; set; } = 40;

    /// <summary>
    /// Creates default settings for Emergent mode
    /// </summary>
    public static EmergentModeSettings Default => new()
    {
        RelevanceThreshold = 50, // Base threshold for substantive responses
        MaxRoundsPerMessage = 3, // Allow more rounds for back-and-forth
        MaxResponsesPerRound = 4, // Allow more agents per round
        UseCheapModelForScoring = true,
        ScoringModelId = "gemini-2.5-flash",
        ScoringModelProvider = "google",
        RequireUniqueInsight = true,
        ShowRelevanceScores = true,
        ResponseDelayMs = 500,
        AllowMultipleResponses = true, // Let agents chime in again
        ShowBriefAcknowledgments = true, // Show "nothing to add" responses
        AcknowledgmentThreshold = 40
    };
}
