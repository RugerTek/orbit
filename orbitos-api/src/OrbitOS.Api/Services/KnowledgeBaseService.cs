using System.Text.Json;

namespace OrbitOS.Api.Services;

public interface IKnowledgeBaseService
{
    /// <summary>
    /// Get the knowledge base index for AI prompt injection.
    /// This provides a summary of all available articles without full content.
    /// </summary>
    KnowledgeBaseIndex GetIndex();

    /// <summary>
    /// Get a specific article by ID.
    /// </summary>
    KnowledgeBaseArticle? GetArticle(string articleId);

    /// <summary>
    /// Search articles by query (searches title, summary, and keywords).
    /// </summary>
    List<KnowledgeBaseArticle> SearchArticles(string query, int maxResults = 5);
}

public class KnowledgeBaseIndex
{
    public List<KnowledgeBaseCategory> Categories { get; set; } = new();
}

public class KnowledgeBaseCategory
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public List<KnowledgeBaseArticleSummary> Articles { get; set; } = new();
}

public class KnowledgeBaseArticleSummary
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Summary { get; set; }
}

public class KnowledgeBaseArticle
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Category { get; set; }
    public required string Summary { get; set; }
    public List<string> Keywords { get; set; } = new();
    public required string Content { get; set; }
    public List<string> RelatedArticles { get; set; } = new();
}

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly ILogger<KnowledgeBaseService> _logger;
    private KnowledgeBaseIndex _index;
    private readonly Dictionary<string, KnowledgeBaseArticle> _articles;

    public KnowledgeBaseService(ILogger<KnowledgeBaseService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _articles = new Dictionary<string, KnowledgeBaseArticle>(StringComparer.OrdinalIgnoreCase);
        _index = new KnowledgeBaseIndex();

        LoadKnowledgeBase(environment.ContentRootPath);
    }

    private void LoadKnowledgeBase(string contentRootPath)
    {
        var knowledgeBasePath = Path.Combine(contentRootPath, "KnowledgeBase");

        if (!Directory.Exists(knowledgeBasePath))
        {
            _logger.LogWarning("KnowledgeBase directory not found at {Path}", knowledgeBasePath);
            return;
        }

        // Load index.json
        var indexPath = Path.Combine(knowledgeBasePath, "index.json");
        if (!File.Exists(indexPath))
        {
            _logger.LogWarning("KnowledgeBase index.json not found at {Path}", indexPath);
            return;
        }

        try
        {
            var indexJson = File.ReadAllText(indexPath);
            _index = JsonSerializer.Deserialize<KnowledgeBaseIndex>(indexJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new KnowledgeBaseIndex();

            _logger.LogInformation("Loaded knowledge base index with {Count} categories", _index.Categories.Count);

            // Load all articles from category directories
            foreach (var category in _index.Categories)
            {
                var categoryPath = Path.Combine(knowledgeBasePath, category.Id);
                if (!Directory.Exists(categoryPath))
                {
                    _logger.LogWarning("Category directory not found: {Path}", categoryPath);
                    continue;
                }

                var articleFiles = Directory.GetFiles(categoryPath, "*.json");
                foreach (var articleFile in articleFiles)
                {
                    try
                    {
                        var articleJson = File.ReadAllText(articleFile);
                        var article = JsonSerializer.Deserialize<KnowledgeBaseArticle>(articleJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (article != null)
                        {
                            _articles[article.Id] = article;
                            _logger.LogDebug("Loaded article: {ArticleId}", article.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load article from {File}", articleFile);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} knowledge base articles", _articles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load knowledge base index");
        }
    }

    public KnowledgeBaseIndex GetIndex()
    {
        return _index;
    }

    public KnowledgeBaseArticle? GetArticle(string articleId)
    {
        return _articles.TryGetValue(articleId, out var article) ? article : null;
    }

    public List<KnowledgeBaseArticle> SearchArticles(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<KnowledgeBaseArticle>();

        var searchTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Score each article based on matches
        var scoredArticles = _articles.Values
            .Select(article => new
            {
                Article = article,
                Score = CalculateSearchScore(article, searchTerms)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.Article)
            .ToList();

        return scoredArticles;
    }

    private int CalculateSearchScore(KnowledgeBaseArticle article, string[] searchTerms)
    {
        var score = 0;

        foreach (var term in searchTerms)
        {
            // Title match (highest weight)
            if (article.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 10;

            // Keyword exact match (high weight)
            if (article.Keywords.Any(k => k.Equals(term, StringComparison.OrdinalIgnoreCase)))
                score += 8;

            // Keyword partial match (medium weight)
            if (article.Keywords.Any(k => k.Contains(term, StringComparison.OrdinalIgnoreCase)))
                score += 4;

            // Summary match (medium weight)
            if (article.Summary.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 3;

            // Category match (lower weight)
            if (article.Category.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 2;
        }

        return score;
    }
}
