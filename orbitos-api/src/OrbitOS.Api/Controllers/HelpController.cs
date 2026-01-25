using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OrbitOS.Api.Services;

namespace OrbitOS.Api.Controllers;

/// <summary>
/// Help System API
/// Serves auto-generated user documentation from specs
/// SPEC: Hard Gate - User Manual requirement
/// </summary>
[ApiController]
[Route("api/help")]
public class HelpController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<HelpController> _logger;

    public HelpController(
        IWebHostEnvironment environment,
        IKnowledgeBaseService knowledgeBaseService,
        ILogger<HelpController> logger)
    {
        _environment = environment;
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get help index containing all features and concepts
    /// </summary>
    [HttpGet("index")]
    [ProducesResponseType(typeof(HelpIndexResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIndex()
    {
        try
        {
            var indexPath = Path.Combine(_environment.ContentRootPath, "..", "..", "docs", "user-manual", "index.json");

            if (!System.IO.File.Exists(indexPath))
            {
                return NotFound(new { error = "Help index not found. Run 'node scripts/generate-user-manual.js' to generate." });
            }

            var json = await System.IO.File.ReadAllTextAsync(indexPath);
            var index = JsonSerializer.Deserialize<HelpIndexResponse>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Ok(index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading help index");
            return StatusCode(500, new { error = "Failed to load help index" });
        }
    }

    /// <summary>
    /// Get field-level help tooltips
    /// </summary>
    [HttpGet("fields")]
    [ProducesResponseType(typeof(Dictionary<string, FieldHelpResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFieldHelp()
    {
        try
        {
            var fieldHelpPath = Path.Combine(_environment.ContentRootPath, "..", "..", "docs", "user-manual", "field-help.json");

            if (!System.IO.File.Exists(fieldHelpPath))
            {
                return NotFound(new { error = "Field help not found" });
            }

            var json = await System.IO.File.ReadAllTextAsync(fieldHelpPath);
            var fieldHelp = JsonSerializer.Deserialize<Dictionary<string, FieldHelpResponse>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Ok(fieldHelp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading field help");
            return StatusCode(500, new { error = "Failed to load field help" });
        }
    }

    /// <summary>
    /// Get feature guide markdown content
    /// </summary>
    [HttpGet("features/{featureId}")]
    [ProducesResponseType(typeof(FeatureGuideResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatureGuide(string featureId)
    {
        try
        {
            var guidePath = Path.Combine(_environment.ContentRootPath, "..", "..", "docs", "user-manual", "features", $"{featureId}.md");

            if (!System.IO.File.Exists(guidePath))
            {
                return NotFound(new { error = $"Feature guide for {featureId} not found" });
            }

            var content = await System.IO.File.ReadAllTextAsync(guidePath);

            return Ok(new FeatureGuideResponse
            {
                FeatureId = featureId,
                Content = content,
                LastUpdated = System.IO.File.GetLastWriteTimeUtc(guidePath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading feature guide for {FeatureId}", featureId);
            return StatusCode(500, new { error = "Failed to load feature guide" });
        }
    }

    /// <summary>
    /// Get concept documentation
    /// </summary>
    [HttpGet("concepts/{conceptId}")]
    [ProducesResponseType(typeof(ConceptDocResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConceptDoc(string conceptId)
    {
        try
        {
            var docPath = Path.Combine(_environment.ContentRootPath, "..", "..", "docs", "user-manual", "concepts", $"{conceptId}.md");

            if (!System.IO.File.Exists(docPath))
            {
                return NotFound(new { error = $"Concept documentation for {conceptId} not found" });
            }

            var content = await System.IO.File.ReadAllTextAsync(docPath);

            return Ok(new ConceptDocResponse
            {
                ConceptId = conceptId,
                Content = content,
                LastUpdated = System.IO.File.GetLastWriteTimeUtc(docPath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading concept doc for {ConceptId}", conceptId);
            return StatusCode(500, new { error = "Failed to load concept documentation" });
        }
    }

    /// <summary>
    /// Get walkthrough steps for a feature
    /// </summary>
    [HttpGet("walkthroughs/{featureId}")]
    [ProducesResponseType(typeof(WalkthroughResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWalkthrough(string featureId)
    {
        try
        {
            var walkthroughPath = Path.Combine(_environment.ContentRootPath, "..", "..", "docs", "user-manual", "walkthroughs", $"{featureId}.json");

            if (!System.IO.File.Exists(walkthroughPath))
            {
                return NotFound(new { error = $"Walkthrough for {featureId} not found" });
            }

            var json = await System.IO.File.ReadAllTextAsync(walkthroughPath);
            var walkthrough = JsonSerializer.Deserialize<WalkthroughResponse>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Ok(walkthrough);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading walkthrough for {FeatureId}", featureId);
            return StatusCode(500, new { error = "Failed to load walkthrough" });
        }
    }

    /// <summary>
    /// Search help content
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<SearchResultResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query is required" });
        }

        try
        {
            var results = new List<SearchResultResponse>();
            var query = q.ToLowerInvariant();

            // Search in index
            var indexPath = Path.Combine(_environment.ContentRootPath, "..", "..", "docs", "user-manual", "index.json");
            if (System.IO.File.Exists(indexPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(indexPath);
                var index = JsonSerializer.Deserialize<HelpIndexResponse>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (index?.Features != null)
                {
                    foreach (var feature in index.Features)
                    {
                        if (feature.Name.ToLowerInvariant().Contains(query) ||
                            feature.Id.ToLowerInvariant().Contains(query))
                        {
                            results.Add(new SearchResultResponse
                            {
                                Type = "feature",
                                Id = feature.Id,
                                Title = feature.Name,
                                Description = $"Feature guide for {feature.Name}",
                                Path = $"/help/features/{feature.Id}",
                                Relevance = feature.Name.ToLowerInvariant().Contains(query) ? 1.0 : 0.8
                            });
                        }
                    }
                }

                if (index?.Concepts != null)
                {
                    foreach (var concept in index.Concepts)
                    {
                        if (concept.Name.ToLowerInvariant().Contains(query) ||
                            concept.Id.ToLowerInvariant().Contains(query))
                        {
                            results.Add(new SearchResultResponse
                            {
                                Type = "concept",
                                Id = concept.Id,
                                Title = concept.Name,
                                Description = $"Learn about {concept.Name}",
                                Path = $"/help/concepts/{concept.Id}",
                                Relevance = concept.Name.ToLowerInvariant().Contains(query) ? 0.9 : 0.7
                            });
                        }
                    }
                }
            }

            return Ok(results.OrderByDescending(r => r.Relevance).Take(10).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching help content");
            return StatusCode(500, new { error = "Search failed" });
        }
    }

    // ============================================
    // Knowledge Base Endpoints
    // ============================================

    /// <summary>
    /// Get knowledge base index with all categories and article summaries
    /// </summary>
    [HttpGet("knowledge-base")]
    [ProducesResponseType(typeof(KnowledgeBaseIndex), StatusCodes.Status200OK)]
    public IActionResult GetKnowledgeBaseIndex()
    {
        try
        {
            var index = _knowledgeBaseService.GetIndex();
            return Ok(index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading knowledge base index");
            return StatusCode(500, new { error = "Failed to load knowledge base index" });
        }
    }

    /// <summary>
    /// Get a specific knowledge base article by ID
    /// </summary>
    [HttpGet("knowledge-base/articles/{*articleId}")]
    [ProducesResponseType(typeof(KnowledgeBaseArticle), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetKnowledgeBaseArticle(string articleId)
    {
        try
        {
            var article = _knowledgeBaseService.GetArticle(articleId);
            if (article == null)
            {
                return NotFound(new { error = $"Article '{articleId}' not found" });
            }
            return Ok(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading knowledge base article {ArticleId}", articleId);
            return StatusCode(500, new { error = "Failed to load article" });
        }
    }

    /// <summary>
    /// Search knowledge base articles
    /// </summary>
    [HttpGet("knowledge-base/search")]
    [ProducesResponseType(typeof(List<KnowledgeBaseArticle>), StatusCodes.Status200OK)]
    public IActionResult SearchKnowledgeBase([FromQuery] string q, [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query is required" });
        }

        try
        {
            var results = _knowledgeBaseService.SearchArticles(q, limit);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching knowledge base");
            return StatusCode(500, new { error = "Search failed" });
        }
    }

    /// <summary>
    /// Get all articles in a specific category
    /// </summary>
    [HttpGet("knowledge-base/categories/{categoryId}")]
    [ProducesResponseType(typeof(List<KnowledgeBaseArticle>), StatusCodes.Status200OK)]
    public IActionResult GetCategoryArticles(string categoryId)
    {
        try
        {
            var index = _knowledgeBaseService.GetIndex();
            var category = index.Categories.FirstOrDefault(c => c.Id == categoryId);

            if (category == null)
            {
                return NotFound(new { error = $"Category '{categoryId}' not found" });
            }

            var articles = new List<KnowledgeBaseArticle>();
            foreach (var summary in category.Articles)
            {
                var article = _knowledgeBaseService.GetArticle(summary.Id);
                if (article != null)
                {
                    articles.Add(article);
                }
            }

            return Ok(articles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category articles for {CategoryId}", categoryId);
            return StatusCode(500, new { error = "Failed to load category articles" });
        }
    }
}

// Response DTOs
public class HelpIndexResponse
{
    public string GeneratedAt { get; set; } = string.Empty;
    public List<FeatureIndexItem> Features { get; set; } = new();
    public List<ConceptIndexItem> Concepts { get; set; } = new();
}

public class FeatureIndexItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Guide { get; set; } = string.Empty;
    public string Walkthrough { get; set; } = string.Empty;
}

public class ConceptIndexItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Doc { get; set; } = string.Empty;
}

public class FieldHelpResponse
{
    public string Entity { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? Validation { get; set; }
    public List<string> Examples { get; set; } = new();
    public string HelpText { get; set; } = string.Empty;
}

public class FeatureGuideResponse
{
    public string FeatureId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class ConceptDocResponse
{
    public string ConceptId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class WalkthroughResponse
{
    public string FeatureId { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public int TotalSteps { get; set; }
    public int EstimatedMinutes { get; set; }
    public List<WalkthroughStep> Steps { get; set; } = new();
}

public class WalkthroughStep
{
    public int Step { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

public class SearchResultResponse
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Path { get; set; }
    public double Relevance { get; set; }
}
