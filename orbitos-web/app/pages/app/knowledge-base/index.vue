<script setup lang="ts">
import type { KnowledgeBaseIndex, KnowledgeBaseCategory } from '~/composables/useKnowledgeBase'

definePageMeta({
  layout: 'app'
})

const { fetchIndex, getCategoryStyle } = useKnowledgeBase()

const index = ref<KnowledgeBaseIndex | null>(null)
const isLoading = ref(true)
const searchQuery = ref('')

onMounted(async () => {
  index.value = await fetchIndex()
  isLoading.value = false
})

// Filter categories based on search
const filteredCategories = computed(() => {
  if (!index.value) return []
  if (!searchQuery.value.trim()) return index.value.categories

  const query = searchQuery.value.toLowerCase()
  return index.value.categories.filter(category => {
    // Check category name
    if (category.name.toLowerCase().includes(query)) return true
    // Check article titles and summaries
    return category.articles.some(
      article =>
        article.title.toLowerCase().includes(query) ||
        article.summary.toLowerCase().includes(query)
    )
  })
})
</script>

<template>
  <div class="min-h-screen">
    <!-- Header -->
    <div class="border-b border-white/10 bg-gray-900/50">
      <div class="max-w-7xl mx-auto px-6 py-8">
        <div class="flex items-start justify-between">
          <div class="flex items-center gap-4">
            <div class="w-14 h-14 rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 flex items-center justify-center">
              <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
              </svg>
            </div>
            <div>
              <h1 class="text-2xl font-bold text-white">Knowledge Base</h1>
              <p class="text-white/60 mt-1">Best practices, methodologies, and guidelines for business operations</p>
            </div>
          </div>

          <!-- Search -->
          <div class="relative w-80">
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Search topics..."
              class="w-full pl-10 pr-4 py-2.5 bg-white/5 border border-white/10 rounded-lg text-white placeholder-white/40 focus:outline-none focus:border-purple-500/50 focus:ring-1 focus:ring-purple-500/50"
            />
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-white/40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="max-w-7xl mx-auto px-6 py-8">
      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-20">
        <div class="animate-spin rounded-full h-8 w-8 border-2 border-purple-500 border-t-transparent"></div>
      </div>

      <!-- Categories Grid -->
      <div v-else-if="filteredCategories.length > 0" class="space-y-8">
        <div
          v-for="category in filteredCategories"
          :key="category.id"
          class="orbitos-glass-subtle rounded-xl p-6"
        >
          <!-- Category Header -->
          <div class="flex items-center gap-3 mb-6">
            <div :class="[getCategoryStyle(category.id).bgColor, 'w-10 h-10 rounded-lg flex items-center justify-center']">
              <svg :class="['w-5 h-5', getCategoryStyle(category.id).color]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getCategoryStyle(category.id).icon" />
              </svg>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-white">{{ category.name }}</h2>
              <p class="text-sm text-white/50">{{ category.description }}</p>
            </div>
          </div>

          <!-- Articles Grid -->
          <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
            <NuxtLink
              v-for="article in category.articles"
              :key="article.id"
              :to="`/app/knowledge-base/${article.id}`"
              class="group p-4 rounded-lg bg-white/5 border border-white/10 hover:border-purple-500/30 hover:bg-purple-500/5 transition-all"
            >
              <h3 class="font-medium text-white group-hover:text-purple-300 transition-colors mb-2">
                {{ article.title }}
              </h3>
              <p class="text-sm text-white/50 line-clamp-2">
                {{ article.summary }}
              </p>
              <div class="mt-3 flex items-center gap-1 text-xs text-purple-400 opacity-0 group-hover:opacity-100 transition-opacity">
                <span>Read more</span>
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </NuxtLink>
          </div>
        </div>
      </div>

      <!-- No Results -->
      <div v-else class="text-center py-20">
        <svg class="w-16 h-16 mx-auto text-white/20 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M12 12h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="text-white/50">No topics found matching "{{ searchQuery }}"</p>
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
</style>
