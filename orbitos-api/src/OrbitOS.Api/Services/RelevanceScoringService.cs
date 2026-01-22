using System.Text.Json;
using System.Text.RegularExpressions;
using OrbitOS.Domain.Entities;

namespace OrbitOS.Api.Services;

/// <summary>
/// Service for evaluating agent relevance in Emergent conversation mode.
/// Uses personality traits, expertise matching, and social dynamics to create
/// realistic meeting behavior.
/// </summary>
public class RelevanceScoringService
{
    private readonly IMultiProviderAiService _aiService;
    private readonly ILogger<RelevanceScoringService> _logger;
    private readonly IConfiguration _configuration;

    public RelevanceScoringService(
        IMultiProviderAiService aiService,
        ILogger<RelevanceScoringService> logger,
        IConfiguration configuration)
    {
        _aiService = aiService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Result of a relevance evaluation for a single agent
    /// </summary>
    public class RelevanceResult
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = "";
        public int RelevanceScore { get; set; }
        public string? Reasoning { get; set; }
        public bool ShouldRespond { get; set; }

        /// <summary>
        /// Suggested response type: Full, Brief, Question, Acknowledgment
        /// </summary>
        public ResponseType SuggestedResponseType { get; set; } = ResponseType.Full;

        /// <summary>
        /// If agent should reference another's point
        /// </summary>
        public string? BuildOnAgent { get; set; }

        /// <summary>
        /// Detected stance: Agree, Disagree, Neutral, Question
        /// </summary>
        public Stance SuggestedStance { get; set; } = Stance.Neutral;
    }

    public enum ResponseType
    {
        Full,           // Complete response with analysis
        Brief,          // Quick acknowledgment or short input
        Question,       // Clarifying question
        Acknowledgment  // "Good point", "I agree", etc.
    }

    public enum Stance
    {
        Agree,
        Disagree,
        Neutral,
        Question,
        BuildOn  // Building on someone else's point
    }

    /// <summary>
    /// Evaluates how relevant each agent would be to the current conversation state.
    /// Incorporates personality traits, expertise matching, and social dynamics.
    /// </summary>
    public async Task<List<RelevanceResult>> EvaluateAgentRelevanceAsync(
        List<AiAgent> agents,
        List<ProviderMessage> conversationHistory,
        EmergentModeSettings settings,
        string? previousRoundContext,
        CancellationToken cancellationToken)
    {
        var results = new List<RelevanceResult>();
        var lastMessage = conversationHistory.LastOrDefault()?.Content ?? "";

        // Step 1: Calculate base expertise scores (keyword matching)
        var expertiseScores = CalculateExpertiseScores(agents, lastMessage, conversationHistory);

        // Step 2: Apply personality modifiers
        var personalityModifiers = CalculatePersonalityModifiers(agents, previousRoundContext != null);

        // Step 3: Build enhanced scoring prompt with social context
        var agentDescriptions = BuildEnhancedAgentDescriptions(agents, expertiseScores);
        var conversationSummary = BuildConversationSummary(conversationHistory);
        var socialContext = BuildSocialContext(conversationHistory, previousRoundContext);

        var scoringPrompt = BuildEnhancedScoringPrompt(
            agentDescriptions,
            conversationSummary,
            socialContext,
            previousRoundContext,
            settings);

        var scoringAgent = CreateScoringAgent(settings);

        try
        {
            var response = await _aiService.SendMessageAsync(
                scoringAgent,
                GetScoringSystemPrompt(),
                new List<ProviderMessage> { new() { Role = "user", Content = scoringPrompt } },
                cancellationToken);

            results = ParseEnhancedScoringResponse(response.Content, agents, settings.RelevanceThreshold);

            // Apply expertise and personality adjustments
            foreach (var result in results)
            {
                var agent = agents.First(a => a.Id == result.AgentId);

                // Boost score based on expertise match
                if (expertiseScores.TryGetValue(agent.Id, out var expertiseBoost))
                {
                    result.RelevanceScore = Math.Min(100, result.RelevanceScore + expertiseBoost);
                }

                // Apply personality modifier
                if (personalityModifiers.TryGetValue(agent.Id, out var personalityMod))
                {
                    result.RelevanceScore = Math.Clamp(result.RelevanceScore + personalityMod, 0, 100);
                }

                // Determine response type based on agent traits
                result.SuggestedResponseType = DetermineResponseType(agent, result.RelevanceScore, previousRoundContext != null);

                // Re-evaluate if should respond with adjusted score
                result.ShouldRespond = result.RelevanceScore >= settings.RelevanceThreshold;
            }

            _logger.LogInformation(
                "Enhanced relevance scoring completed for {AgentCount} agents. Responding: {RespondingCount}",
                agents.Count,
                results.Count(r => r.ShouldRespond));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate agent relevance, using expertise-based fallback");

            // Fallback: use expertise scores with personality
            results = agents.Select(a => {
                var score = 50 + (expertiseScores.GetValueOrDefault(a.Id, 0)) + (personalityModifiers.GetValueOrDefault(a.Id, 0));
                score = Math.Clamp(score, 0, 100);
                return new RelevanceResult
                {
                    AgentId = a.Id,
                    AgentName = a.Name,
                    RelevanceScore = score,
                    Reasoning = $"Expertise-based: {GetExpertiseMatchReason(a, lastMessage)}",
                    ShouldRespond = score >= settings.RelevanceThreshold,
                    SuggestedResponseType = DetermineResponseType(a, score, previousRoundContext != null)
                };
            }).ToList();
        }

        // Sort by score descending, then by assertiveness
        return results.OrderByDescending(r => r.RelevanceScore)
                     .ThenByDescending(r => agents.First(a => a.Id == r.AgentId).Assertiveness)
                     .ToList();
    }

    /// <summary>
    /// Calculate expertise scores based on keyword matching
    /// </summary>
    private Dictionary<Guid, int> CalculateExpertiseScores(
        List<AiAgent> agents,
        string lastMessage,
        List<ProviderMessage> history)
    {
        var scores = new Dictionary<Guid, int>();
        var allText = lastMessage + " " + string.Join(" ", history.TakeLast(5).Select(m => m.Content));
        var textLower = allText.ToLowerInvariant();

        foreach (var agent in agents)
        {
            var score = 0;

            // Check expertise areas
            if (!string.IsNullOrEmpty(agent.ExpertiseAreas))
            {
                var keywords = agent.ExpertiseAreas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim().ToLowerInvariant());

                foreach (var keyword in keywords)
                {
                    if (textLower.Contains(keyword))
                    {
                        score += 15; // Strong boost for keyword match
                    }
                }
            }

            // Check role title keywords
            var roleWords = agent.RoleTitle.ToLowerInvariant().Split(' ');
            foreach (var word in roleWords)
            {
                if (word.Length > 3 && textLower.Contains(word))
                {
                    score += 10;
                }
            }

            // Domain-specific keyword detection
            score += DetectDomainRelevance(agent, textLower);

            scores[agent.Id] = Math.Min(30, score); // Cap expertise boost at 30
        }

        return scores;
    }

    /// <summary>
    /// Detect domain relevance based on common business keywords
    /// </summary>
    private int DetectDomainRelevance(AiAgent agent, string text)
    {
        var roleTitle = agent.RoleTitle.ToLowerInvariant();
        var score = 0;

        // Finance-related
        if (roleTitle.Contains("cfo") || roleTitle.Contains("finance") || roleTitle.Contains("financial"))
        {
            var financeKeywords = new[] { "budget", "cost", "revenue", "profit", "expense", "financial", "money", "funding", "investment", "roi", "margin", "cash" };
            if (financeKeywords.Any(k => text.Contains(k))) score += 20;
        }

        // HR-related
        if (roleTitle.Contains("hr") || roleTitle.Contains("human") || roleTitle.Contains("people") || roleTitle.Contains("talent"))
        {
            var hrKeywords = new[] { "hire", "hiring", "team", "culture", "employee", "staff", "talent", "recruit", "training", "performance" };
            if (hrKeywords.Any(k => text.Contains(k))) score += 20;
        }

        // Operations-related
        if (roleTitle.Contains("coo") || roleTitle.Contains("operation") || roleTitle.Contains("logistics"))
        {
            var opsKeywords = new[] { "process", "efficiency", "workflow", "supply", "logistics", "delivery", "timeline", "capacity", "resource" };
            if (opsKeywords.Any(k => text.Contains(k))) score += 20;
        }

        // Tech-related
        if (roleTitle.Contains("cto") || roleTitle.Contains("tech") || roleTitle.Contains("engineering"))
        {
            var techKeywords = new[] { "system", "software", "technology", "platform", "infrastructure", "security", "data", "automation", "integration" };
            if (techKeywords.Any(k => text.Contains(k))) score += 20;
        }

        // Strategy-related
        if (roleTitle.Contains("ceo") || roleTitle.Contains("strategy") || roleTitle.Contains("chief"))
        {
            var strategyKeywords = new[] { "strategy", "vision", "growth", "market", "competition", "long-term", "direction", "priority", "goal" };
            if (strategyKeywords.Any(k => text.Contains(k))) score += 15;
        }

        // Marketing/Sales
        if (roleTitle.Contains("cmo") || roleTitle.Contains("marketing") || roleTitle.Contains("sales"))
        {
            var marketingKeywords = new[] { "customer", "market", "brand", "campaign", "sales", "revenue", "acquisition", "retention", "conversion" };
            if (marketingKeywords.Any(k => text.Contains(k))) score += 20;
        }

        return score;
    }

    /// <summary>
    /// Calculate personality-based modifiers
    /// </summary>
    private Dictionary<Guid, int> CalculatePersonalityModifiers(List<AiAgent> agents, bool isReactionRound)
    {
        var modifiers = new Dictionary<Guid, int>();

        foreach (var agent in agents)
        {
            var modifier = 0;

            // Assertiveness affects likelihood to speak
            // High assertiveness (70-100) = +10 to +20
            // Low assertiveness (0-30) = -10 to -20
            modifier += (agent.Assertiveness - 50) / 5;

            // In reaction rounds, certain tendencies matter more
            if (isReactionRound)
            {
                switch (agent.ReactionTendency)
                {
                    case ReactionTendency.DevilsAdvocate:
                        modifier += 10; // More likely to jump in with counterpoint
                        break;
                    case ReactionTendency.ConsensusBuilder:
                        modifier += 5; // Wants to help align
                        break;
                    case ReactionTendency.Supportive:
                        modifier += 3; // May want to agree
                        break;
                    case ReactionTendency.Critical:
                        modifier += 8; // May see issues to raise
                        break;
                }
            }

            // Seniority affects confidence to speak
            modifier += (agent.SeniorityLevel - 3) * 3;

            modifiers[agent.Id] = modifier;
        }

        return modifiers;
    }

    /// <summary>
    /// Determine the type of response based on agent traits and score
    /// </summary>
    private ResponseType DetermineResponseType(AiAgent agent, int score, bool isReactionRound)
    {
        // If score is marginal (40-60), consider brief responses
        if (score >= 40 && score < 60)
        {
            if (agent.GivesBriefAcknowledgments && isReactionRound)
                return ResponseType.Acknowledgment;
            if (agent.AsksQuestions)
                return ResponseType.Question;
            return ResponseType.Brief;
        }

        // High assertiveness + questions = may ask clarifying questions
        if (agent.AsksQuestions && score >= 50 && score < 70)
        {
            return ResponseType.Question;
        }

        // High score = full response
        if (score >= 60)
        {
            return ResponseType.Full;
        }

        return ResponseType.Brief;
    }

    private string BuildEnhancedAgentDescriptions(List<AiAgent> agents, Dictionary<Guid, int> expertiseScores)
    {
        return string.Join("\n\n", agents.Select((a, i) =>
        {
            var expertiseMatch = expertiseScores.GetValueOrDefault(a.Id, 0) > 0 ? " [EXPERTISE MATCH]" : "";
            var style = GetCommunicationStyleDescription(a.CommunicationStyle);
            var tendency = GetReactionTendencyDescription(a.ReactionTendency);

            return $@"{i + 1}. {a.Name} ({a.RoleTitle}){expertiseMatch}
   Style: {style} | Tendency: {tendency} | Assertiveness: {GetAssertivenessLevel(a.Assertiveness)}
   Expertise: {GetAgentExpertise(a)}";
        }));
    }

    private string BuildConversationSummary(List<ProviderMessage> history)
    {
        return string.Join("\n", history.TakeLast(10).Select(m =>
            $"[{m.Role}]: {TruncateContent(m.Content, 200)}"));
    }

    private string BuildSocialContext(List<ProviderMessage> history, string? previousRoundContext)
    {
        if (string.IsNullOrEmpty(previousRoundContext))
            return "";

        // Analyze previous responses for agreement/disagreement patterns
        return $@"
SOCIAL CONTEXT:
Previous responses from this round show the following themes:
{AnalyzeThemes(previousRoundContext)}

Consider: Who might want to agree, disagree, or add a different perspective?";
    }

    private string AnalyzeThemes(string context)
    {
        // Simple theme extraction
        var lines = context.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return "- No clear themes yet";

        return "- " + string.Join("\n- ", lines.Take(3).Select(l => TruncateContent(l, 100)));
    }

    private string BuildEnhancedScoringPrompt(
        string agentDescriptions,
        string conversationSummary,
        string socialContext,
        string? previousRoundContext,
        EmergentModeSettings settings)
    {
        var previousRoundClause = !string.IsNullOrEmpty(previousRoundContext)
            ? $@"
RESPONSES FROM THIS ROUND SO FAR:
{previousRoundContext}

This is a REACTION round. Consider:
- Who might want to build on these points?
- Who might disagree or offer counterpoint?
- Who might want to synthesize or find consensus?"
            : "";

        return $@"You are simulating a directors meeting. Evaluate which agents should speak next.

PARTICIPANTS:
{agentDescriptions}

CONVERSATION:
{conversationSummary}
{socialContext}
{previousRoundClause}

For each agent, evaluate:
1. RELEVANCE: Does this topic touch their expertise? (0-100)
2. STANCE: Would they likely agree, disagree, question, or build on previous points?
3. RESPONSE_TYPE: Should they give a full response, brief comment, question, or acknowledgment?
4. BUILD_ON: If they would reference another agent's point, which one?

Return JSON array:
[
  {{
    ""agentIndex"": 1,
    ""score"": 85,
    ""reasoning"": ""Budget discussion is core to CFO role"",
    ""stance"": ""neutral"",
    ""responseType"": ""full"",
    ""buildOn"": null
  }},
  {{
    ""agentIndex"": 2,
    ""score"": 60,
    ""reasoning"": ""People impact of budget cuts"",
    ""stance"": ""buildOn"",
    ""responseType"": ""brief"",
    ""buildOn"": ""CFO""
  }}
]

Return ONLY the JSON array.";
    }

    private string GetScoringSystemPrompt()
    {
        return @"You are a meeting dynamics analyzer. Your job is to determine which participants should speak next in a directors meeting, based on:
- Their expertise and the topic at hand
- Their personality (assertive people speak more, diplomatic people wait for right moment)
- Social dynamics (agreement, disagreement, building on others' points)
- Meeting flow (avoid everyone speaking at once, but ensure relevant voices are heard)

Be realistic: In a real meeting, not everyone speaks on every topic. Match expertise to topic.
Return valid JSON only.";
    }

    private List<RelevanceResult> ParseEnhancedScoringResponse(string response, List<AiAgent> agents, int threshold)
    {
        var results = new List<RelevanceResult>();

        try
        {
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var scores = JsonSerializer.Deserialize<List<EnhancedScoringItem>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (scores != null)
                {
                    foreach (var score in scores)
                    {
                        var agentIndex = score.AgentIndex - 1;
                        if (agentIndex >= 0 && agentIndex < agents.Count)
                        {
                            var agent = agents[agentIndex];
                            results.Add(new RelevanceResult
                            {
                                AgentId = agent.Id,
                                AgentName = agent.Name,
                                RelevanceScore = Math.Clamp(score.Score, 0, 100),
                                Reasoning = score.Reasoning,
                                ShouldRespond = score.Score >= threshold,
                                SuggestedStance = ParseStance(score.Stance),
                                SuggestedResponseType = ParseResponseType(score.ResponseType),
                                BuildOnAgent = score.BuildOn
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse enhanced scoring response: {Response}", TruncateContent(response, 200));
        }

        // Fill in missing agents
        foreach (var agent in agents)
        {
            if (!results.Any(r => r.AgentId == agent.Id))
            {
                results.Add(new RelevanceResult
                {
                    AgentId = agent.Id,
                    AgentName = agent.Name,
                    RelevanceScore = 50,
                    Reasoning = "Could not determine relevance",
                    ShouldRespond = 50 >= threshold
                });
            }
        }

        return results;
    }

    private Stance ParseStance(string? stance)
    {
        return stance?.ToLowerInvariant() switch
        {
            "agree" => Stance.Agree,
            "disagree" => Stance.Disagree,
            "question" => Stance.Question,
            "buildon" or "build_on" or "build on" => Stance.BuildOn,
            _ => Stance.Neutral
        };
    }

    private ResponseType ParseResponseType(string? responseType)
    {
        return responseType?.ToLowerInvariant() switch
        {
            "brief" => ResponseType.Brief,
            "question" => ResponseType.Question,
            "acknowledgment" or "ack" => ResponseType.Acknowledgment,
            _ => ResponseType.Full
        };
    }

    private string GetCommunicationStyleDescription(CommunicationStyle style)
    {
        return style switch
        {
            CommunicationStyle.Formal => "Professional & structured",
            CommunicationStyle.Casual => "Friendly & conversational",
            CommunicationStyle.Direct => "Straight to the point",
            CommunicationStyle.Diplomatic => "Tactful & considerate",
            CommunicationStyle.Analytical => "Data-driven & methodical",
            _ => "Balanced"
        };
    }

    private string GetReactionTendencyDescription(ReactionTendency tendency)
    {
        return tendency switch
        {
            ReactionTendency.Supportive => "Tends to agree & build",
            ReactionTendency.Critical => "Looks for risks & issues",
            ReactionTendency.Balanced => "Weighs pros & cons",
            ReactionTendency.DevilsAdvocate => "Challenges assumptions",
            ReactionTendency.ConsensusBuilder => "Seeks alignment",
            _ => "Balanced"
        };
    }

    private string GetAssertivenessLevel(int assertiveness)
    {
        return assertiveness switch
        {
            >= 80 => "Very assertive (speaks first)",
            >= 60 => "Assertive",
            >= 40 => "Moderate",
            >= 20 => "Reserved",
            _ => "Very reserved (waits to be asked)"
        };
    }

    private string GetAgentExpertise(AiAgent agent)
    {
        if (!string.IsNullOrEmpty(agent.ExpertiseAreas))
        {
            return agent.ExpertiseAreas;
        }

        // Extract from system prompt
        if (string.IsNullOrEmpty(agent.SystemPrompt)) return "General";

        var firstSentence = agent.SystemPrompt.Split(new[] { '.', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? agent.SystemPrompt;

        return TruncateContent(firstSentence, 100);
    }

    private string GetExpertiseMatchReason(AiAgent agent, string text)
    {
        if (!string.IsNullOrEmpty(agent.ExpertiseAreas))
        {
            var keywords = agent.ExpertiseAreas.Split(',').Select(k => k.Trim().ToLower());
            var matches = keywords.Where(k => text.ToLower().Contains(k)).Take(2);
            if (matches.Any())
                return $"Keywords matched: {string.Join(", ", matches)}";
        }
        return $"Role: {agent.RoleTitle}";
    }

    private string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content)) return "";
        if (content.Length <= maxLength) return content;
        return content.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Quick evaluation for a single agent - used in reaction rounds
    /// </summary>
    public async Task<RelevanceResult> EvaluateSingleAgentRelevanceAsync(
        AiAgent agent,
        List<ProviderMessage> conversationHistory,
        string newResponsesContext,
        EmergentModeSettings settings,
        CancellationToken cancellationToken)
    {
        var lastMessage = conversationHistory.LastOrDefault()?.Content ?? "";
        var expertiseBoost = DetectDomainRelevance(agent, lastMessage.ToLower());

        var scoringPrompt = $@"Given the conversation and NEW responses, should {agent.Name} ({agent.RoleTitle}) add a follow-up?

{agent.Name}'s profile:
- Style: {GetCommunicationStyleDescription(agent.CommunicationStyle)}
- Tendency: {GetReactionTendencyDescription(agent.ReactionTendency)}
- Expertise: {GetAgentExpertise(agent)}
- Assertiveness: {GetAssertivenessLevel(agent.Assertiveness)}

Recent conversation:
{string.Join("\n", conversationHistory.TakeLast(5).Select(m => $"[{m.Role}]: {TruncateContent(m.Content, 150)}"))}

NEW responses this round:
{newResponsesContext}

Evaluate:
1. Does {agent.Name} have something UNIQUE to add?
2. Would they likely agree, disagree, or build on what was said?
3. Given their personality, would they speak up here?

Return JSON: {{""score"": <0-100>, ""reasoning"": ""<brief>"", ""stance"": ""<agree/disagree/neutral/buildOn>"", ""responseType"": ""<full/brief/question/acknowledgment>""}}";

        var scoringAgent = CreateScoringAgent(settings);

        try
        {
            var response = await _aiService.SendMessageAsync(
                scoringAgent,
                "You are a meeting dynamics analyzer. Return only valid JSON.",
                new List<ProviderMessage> { new() { Role = "user", Content = scoringPrompt } },
                cancellationToken);

            var parsed = ParseSingleEnhancedScore(response.Content);
            var finalScore = Math.Clamp(parsed.score + expertiseBoost + ((agent.Assertiveness - 50) / 5), 0, 100);

            return new RelevanceResult
            {
                AgentId = agent.Id,
                AgentName = agent.Name,
                RelevanceScore = finalScore,
                Reasoning = parsed.reasoning,
                ShouldRespond = finalScore >= settings.RelevanceThreshold,
                SuggestedStance = parsed.stance,
                SuggestedResponseType = parsed.responseType
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to score agent {AgentName}, using fallback", agent.Name);

            var fallbackScore = 30 + expertiseBoost + ((agent.Assertiveness - 50) / 5);
            return new RelevanceResult
            {
                AgentId = agent.Id,
                AgentName = agent.Name,
                RelevanceScore = Math.Clamp(fallbackScore, 0, 100),
                Reasoning = "Scoring failed",
                ShouldRespond = fallbackScore >= settings.RelevanceThreshold,
                SuggestedResponseType = agent.GivesBriefAcknowledgments ? ResponseType.Acknowledgment : ResponseType.Brief
            };
        }
    }

    private (int score, string? reasoning, Stance stance, ResponseType responseType) ParseSingleEnhancedScore(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<EnhancedScoringItem>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    return (
                        Math.Clamp(parsed.Score, 0, 100),
                        parsed.Reasoning,
                        ParseStance(parsed.Stance),
                        ParseResponseType(parsed.ResponseType)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse single enhanced score");
        }

        return (50, null, Stance.Neutral, ResponseType.Brief);
    }

    private AiAgent CreateScoringAgent(EmergentModeSettings settings)
    {
        var provider = settings.ScoringModelProvider ?? "google";
        var modelId = settings.ScoringModelId ?? "gemini-2.5-flash";

        return new AiAgent
        {
            Id = Guid.Empty,
            Name = "MeetingDynamicsAnalyzer",
            RoleTitle = "Meeting Facilitator",
            Provider = Enum.Parse<AiProvider>(provider, ignoreCase: true),
            ModelId = modelId,
            ModelDisplayName = "Scoring Model",
            SystemPrompt = "",
            MaxTokensPerResponse = 800,
            Temperature = 0.2m,
            IsActive = true,
            OrganizationId = Guid.Empty
        };
    }

    private class EnhancedScoringItem
    {
        public int AgentIndex { get; set; }
        public int Score { get; set; }
        public string? Reasoning { get; set; }
        public string? Stance { get; set; }
        public string? ResponseType { get; set; }
        public string? BuildOn { get; set; }
    }
}
