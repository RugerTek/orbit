/**
 * Knowledge Base Composable
 *
 * Provides access to the knowledge base of best practices and guidelines.
 */

export interface KnowledgeBaseCategory {
  id: string
  name: string
  description: string
  articles: KnowledgeBaseArticleSummary[]
}

export interface KnowledgeBaseArticleSummary {
  id: string
  title: string
  summary: string
}

export interface KnowledgeBaseArticle {
  id: string
  title: string
  category: string
  summary: string
  keywords: string[]
  content: string
  relatedArticles: string[]
}

export interface KnowledgeBaseIndex {
  categories: KnowledgeBaseCategory[]
}

// Cache for articles to avoid refetching
const articleCache = ref<Map<string, KnowledgeBaseArticle>>(new Map())
const indexCache = ref<KnowledgeBaseIndex | null>(null)

export function useKnowledgeBase() {
  const { get } = useApi()
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Fetch the knowledge base index
  const fetchIndex = async (): Promise<KnowledgeBaseIndex | null> => {
    if (indexCache.value) {
      return indexCache.value
    }

    isLoading.value = true
    error.value = null

    try {
      const response = await get<KnowledgeBaseIndex>('/api/help/knowledge-base')
      indexCache.value = response
      return response
    } catch (err: any) {
      error.value = err.message || 'Failed to load knowledge base index'
      console.error('Error fetching knowledge base index:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  // Fetch a specific article
  const fetchArticle = async (articleId: string): Promise<KnowledgeBaseArticle | null> => {
    // Check cache first
    if (articleCache.value.has(articleId)) {
      return articleCache.value.get(articleId)!
    }

    isLoading.value = true
    error.value = null

    try {
      const response = await get<KnowledgeBaseArticle>(`/api/help/knowledge-base/articles/${articleId}`)
      articleCache.value.set(articleId, response)
      return response
    } catch (err: any) {
      error.value = err.message || 'Failed to load article'
      console.error('Error fetching article:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }

  // Fetch all articles in a category
  const fetchCategoryArticles = async (categoryId: string): Promise<KnowledgeBaseArticle[]> => {
    isLoading.value = true
    error.value = null

    try {
      const response = await get<KnowledgeBaseArticle[]>(`/api/help/knowledge-base/categories/${categoryId}`)
      // Cache each article
      response.forEach(article => {
        articleCache.value.set(article.id, article)
      })
      return response
    } catch (err: any) {
      error.value = err.message || 'Failed to load category articles'
      console.error('Error fetching category articles:', err)
      return []
    } finally {
      isLoading.value = false
    }
  }

  // Search articles
  const searchArticles = async (query: string, limit = 5): Promise<KnowledgeBaseArticle[]> => {
    if (!query.trim()) return []

    isLoading.value = true
    error.value = null

    try {
      const response = await get<KnowledgeBaseArticle[]>(`/api/help/knowledge-base/search?q=${encodeURIComponent(query)}&limit=${limit}`)
      return response
    } catch (err: any) {
      error.value = err.message || 'Search failed'
      console.error('Error searching knowledge base:', err)
      return []
    } finally {
      isLoading.value = false
    }
  }

  // Get category info by ID
  const getCategoryById = (categoryId: string): KnowledgeBaseCategory | null => {
    if (!indexCache.value) return null
    return indexCache.value.categories.find(c => c.id === categoryId) || null
  }

  // Map category ID to icon and color
  const getCategoryStyle = (categoryId: string): { icon: string; color: string; bgColor: string } => {
    const styles: Record<string, { icon: string; color: string; bgColor: string }> = {
      'process-mapping': {
        icon: 'M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z',
        color: 'text-cyan-400',
        bgColor: 'bg-cyan-500/20'
      },
      'roles': {
        icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z',
        color: 'text-purple-400',
        bgColor: 'bg-purple-500/20'
      },
      'functions': {
        icon: 'M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z',
        color: 'text-emerald-400',
        bgColor: 'bg-emerald-500/20'
      },
      'canvas': {
        icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z',
        color: 'text-blue-400',
        bgColor: 'bg-blue-500/20'
      },
      'goals': {
        icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
        color: 'text-amber-400',
        bgColor: 'bg-amber-500/20'
      },
      'org-design': {
        icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
        color: 'text-rose-400',
        bgColor: 'bg-rose-500/20'
      }
    }
    return styles[categoryId] || {
      icon: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253',
      color: 'text-gray-400',
      bgColor: 'bg-gray-500/20'
    }
  }

  return {
    // State
    isLoading: readonly(isLoading),
    error: readonly(error),

    // Methods
    fetchIndex,
    fetchArticle,
    fetchCategoryArticles,
    searchArticles,
    getCategoryById,
    getCategoryStyle,

    // Cache access
    indexCache: readonly(indexCache)
  }
}
