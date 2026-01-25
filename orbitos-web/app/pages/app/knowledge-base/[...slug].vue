<script setup lang="ts">
import type { KnowledgeBaseArticle } from '~/composables/useKnowledgeBase'

definePageMeta({
  layout: 'app'
})

const route = useRoute()
const { fetchArticle, fetchIndex, getCategoryStyle } = useKnowledgeBase()

const article = ref<KnowledgeBaseArticle | null>(null)
const isLoading = ref(true)
const error = ref<string | null>(null)

// Get article ID from slug array
const articleId = computed(() => {
  const slug = route.params.slug
  if (Array.isArray(slug)) {
    return slug.join('/')
  }
  return slug || ''
})

// Fetch article on mount and when route changes
watch(articleId, async (newId) => {
  if (newId) {
    isLoading.value = true
    error.value = null
    article.value = await fetchArticle(newId)
    if (!article.value) {
      error.value = 'Article not found'
    }
    isLoading.value = false
  }
}, { immediate: true })

// Get category info
const categoryStyle = computed(() => {
  if (!article.value) return null
  return getCategoryStyle(article.value.category)
})

// Related articles
const relatedArticles = ref<KnowledgeBaseArticle[]>([])

watch(article, async (newArticle) => {
  if (newArticle?.relatedArticles?.length) {
    const articles = await Promise.all(
      newArticle.relatedArticles.map(id => fetchArticle(id))
    )
    relatedArticles.value = articles.filter((a): a is KnowledgeBaseArticle => a !== null)
  } else {
    relatedArticles.value = []
  }
})

// Simple markdown to HTML conversion
function renderMarkdown(content: string): string {
  if (!content) return ''

  let html = content

  // Headers
  html = html.replace(/^### (.+)$/gm, '<h3 class="text-lg font-semibold text-white mt-6 mb-3">$1</h3>')
  html = html.replace(/^## (.+)$/gm, '<h2 class="text-xl font-bold text-white mt-8 mb-4">$1</h2>')
  html = html.replace(/^# (.+)$/gm, '<h1 class="text-2xl font-bold text-white mt-8 mb-4">$1</h1>')

  // Bold and italic
  html = html.replace(/\*\*\*(.+?)\*\*\*/g, '<strong><em>$1</em></strong>')
  html = html.replace(/\*\*(.+?)\*\*/g, '<strong class="text-white">$1</strong>')
  html = html.replace(/\*(.+?)\*/g, '<em>$1</em>')

  // Code blocks
  html = html.replace(/```(\w*)\n([\s\S]*?)```/g, (_, lang, code) => {
    return `<pre class="bg-gray-900 rounded-lg p-4 overflow-x-auto my-4 border border-white/10"><code class="text-sm text-green-400 font-mono">${escapeHtml(code.trim())}</code></pre>`
  })

  // Inline code
  html = html.replace(/`([^`]+)`/g, '<code class="bg-gray-800 text-purple-300 px-1.5 py-0.5 rounded text-sm font-mono">$1</code>')

  // Tables
  html = html.replace(/^\|(.+)\|$/gm, (match) => {
    const cells = match.split('|').filter(c => c.trim())
    const isHeader = cells.some(c => c.includes('---'))
    if (isHeader) return ''
    return `<tr>${cells.map(c => `<td class="border border-white/20 px-4 py-2 text-white/90">${c.trim()}</td>`).join('')}</tr>`
  })

  // Wrap consecutive table rows
  html = html.replace(/(<tr>[\s\S]*?<\/tr>\n?)+/g, (match) => {
    const rows = match.trim().split('\n').filter(r => r.trim())
    if (rows.length > 0) {
      const headerRow = rows[0].replace(/<td/g, '<th').replace(/<\/td>/g, '</th>').replace(/text-white\/90/g, 'text-white font-semibold')
      const bodyRows = rows.slice(1).join('\n')
      return `<div class="overflow-x-auto my-4"><table class="w-full border-collapse border border-white/20"><thead class="bg-white/10">${headerRow}</thead><tbody>${bodyRows}</tbody></table></div>`
    }
    return match
  })

  // Lists
  html = html.replace(/^- (.+)$/gm, '<li class="text-white/90 ml-4">$1</li>')
  html = html.replace(/^(\d+)\. (.+)$/gm, '<li class="text-white/90 ml-4" value="$1">$2</li>')

  // Wrap consecutive list items
  html = html.replace(/(<li class="text-white\/90 ml-4">[\s\S]*?<\/li>\n?)+/g, (match) => {
    return `<ul class="list-disc list-inside space-y-1 my-3">${match}</ul>`
  })

  // Paragraphs (lines that aren't already wrapped)
  html = html.split('\n\n').map(para => {
    if (para.startsWith('<') || para.trim() === '') return para
    return `<p class="text-white/90 leading-relaxed my-3">${para}</p>`
  }).join('\n')

  return html
}

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;')
}
</script>

<template>
  <div class="min-h-screen">
    <!-- Header -->
    <div class="border-b border-white/10 bg-gray-900/50">
      <div class="max-w-4xl mx-auto px-6 py-6">
        <div class="flex items-center gap-3 mb-4">
          <NuxtLink
            to="/app/knowledge-base"
            class="flex items-center gap-2 text-white/60 hover:text-white transition-colors"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
            <span>Knowledge Base</span>
          </NuxtLink>
          <span v-if="article" class="text-white/40">/</span>
          <span v-if="article && categoryStyle" :class="categoryStyle.color" class="text-sm">
            {{ article.category.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ') }}
          </span>
        </div>

        <div v-if="article" class="flex items-start gap-4">
          <div v-if="categoryStyle" :class="[categoryStyle.bgColor, 'w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0']">
            <svg :class="['w-6 h-6', categoryStyle.color]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="categoryStyle.icon" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-white">{{ article.title }}</h1>
            <p class="text-white/80 mt-1">{{ article.summary }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="max-w-4xl mx-auto px-6 py-8">
      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-20">
        <div class="animate-spin rounded-full h-8 w-8 border-2 border-purple-500 border-t-transparent"></div>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="text-center py-20">
        <svg class="w-16 h-16 mx-auto text-red-400/50 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <p class="text-white/50">{{ error }}</p>
        <NuxtLink
          to="/app/knowledge-base"
          class="inline-flex items-center gap-2 mt-4 text-purple-400 hover:text-purple-300"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
          Back to Knowledge Base
        </NuxtLink>
      </div>

      <!-- Article Content -->
      <div v-else-if="article" class="space-y-8">
        <!-- Keywords -->
        <div v-if="article.keywords?.length" class="flex flex-wrap gap-2">
          <span
            v-for="keyword in article.keywords"
            :key="keyword"
            class="px-2.5 py-1 bg-white/5 border border-white/10 rounded-full text-xs text-white/60"
          >
            {{ keyword }}
          </span>
        </div>

        <!-- Main Content -->
        <div
          class="prose prose-invert max-w-none"
          v-html="renderMarkdown(article.content)"
        />

        <!-- Related Articles -->
        <div v-if="relatedArticles.length > 0" class="border-t border-white/10 pt-8 mt-12">
          <h3 class="text-lg font-semibold text-white mb-4">Related Articles</h3>
          <div class="grid md:grid-cols-2 gap-4">
            <NuxtLink
              v-for="related in relatedArticles"
              :key="related.id"
              :to="`/app/knowledge-base/${related.id}`"
              class="group p-4 rounded-lg bg-white/5 border border-white/10 hover:border-purple-500/30 hover:bg-purple-500/5 transition-all"
            >
              <h4 class="font-medium text-white group-hover:text-purple-300 transition-colors">
                {{ related.title }}
              </h4>
              <p class="text-sm text-white/70 mt-1 line-clamp-2">
                {{ related.summary }}
              </p>
            </NuxtLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.prose :deep(h2) {
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  padding-bottom: 0.5rem;
}

.prose :deep(table) {
  width: 100%;
}

.prose :deep(th),
.prose :deep(td) {
  text-align: left;
}

.prose :deep(pre) {
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
