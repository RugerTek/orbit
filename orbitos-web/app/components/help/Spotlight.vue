<script setup lang="ts">
/**
 * HelpSpotlight - Command Palette for Help Search
 *
 * Opens with Cmd+K (Mac) or Ctrl+K (Windows/Linux)
 * Searches features, concepts, and field help
 * Provides quick actions and navigation
 */

import type { HelpSearchResult } from '~/types/help'

const { isSpotlightOpen, closeSpotlight, searchHelp, startWalkthrough } = useHelp()

const searchQuery = ref('')
const selectedIndex = ref(0)
const searchInput = ref<HTMLInputElement | null>(null)

// Search results
const searchResults = computed(() => {
  if (!searchQuery.value.trim()) {
    return defaultSuggestions.value
  }
  return searchHelp(searchQuery.value)
})

// Default suggestions when no query
const defaultSuggestions = computed((): HelpSearchResult[] => [
  {
    type: 'feature',
    id: 'F001',
    title: 'Getting Started',
    description: 'Learn how to sign in and navigate OrbitOS',
    path: '/help/features/F001',
    relevance: 1
  },
  {
    type: 'feature',
    id: 'F002',
    title: 'Managing People & Roles',
    description: 'Add team members and assign roles',
    path: '/help/features/F002',
    relevance: 0.9
  },
  {
    type: 'concept',
    id: 'ENT004',
    title: 'Understanding Functions',
    description: 'Learn about atomic work units',
    path: '/help/concepts/ENT004',
    relevance: 0.8
  }
])

// Focus input when opened
watch(isSpotlightOpen, (open) => {
  if (open) {
    searchQuery.value = ''
    selectedIndex.value = 0
    nextTick(() => {
      searchInput.value?.focus()
    })
  }
})

// Keyboard navigation
const handleKeydown = (e: KeyboardEvent) => {
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    selectedIndex.value = Math.min(selectedIndex.value + 1, searchResults.value.length - 1)
  }
  if (e.key === 'ArrowUp') {
    e.preventDefault()
    selectedIndex.value = Math.max(selectedIndex.value - 1, 0)
  }
  if (e.key === 'Enter') {
    e.preventDefault()
    selectResult(searchResults.value[selectedIndex.value])
  }
}

// Select a result
const selectResult = (result: HelpSearchResult) => {
  if (!result) return

  closeSpotlight()

  if (result.type === 'feature') {
    // Start walkthrough for features
    startWalkthrough(result.id)
  } else if (result.path) {
    // Navigate to help page
    navigateTo(result.path)
  }
}

// Icon based on result type
const getIcon = (type: string) => {
  switch (type) {
    case 'feature':
      return 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z'
    case 'concept':
      return 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253'
    case 'field':
      return 'M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z'
    default:
      return 'M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
  }
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isSpotlightOpen"
        class="fixed inset-0 z-[100] flex items-start justify-center pt-[15vh] bg-black/60 backdrop-blur-sm"
        @click.self="closeSpotlight"
      >
        <div
          class="w-full max-w-2xl overflow-hidden rounded-2xl border border-slate-700 bg-slate-800/95 shadow-2xl"
          @keydown="handleKeydown"
        >
          <!-- Search Input -->
          <div class="flex items-center gap-3 border-b border-slate-700 px-4 py-4">
            <svg class="h-5 w-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <input
              ref="searchInput"
              v-model="searchQuery"
              type="text"
              placeholder="Search help, features, concepts..."
              class="flex-1 bg-transparent text-white placeholder-slate-500 outline-none text-lg"
            />
            <kbd class="hidden sm:inline-flex items-center gap-1 rounded border border-slate-600 bg-slate-700/50 px-2 py-1 text-xs text-slate-400">
              ESC
            </kbd>
          </div>

          <!-- Results -->
          <div class="max-h-[400px] overflow-y-auto">
            <div v-if="searchResults.length === 0" class="px-4 py-8 text-center text-slate-400">
              No results found for "{{ searchQuery }}"
            </div>

            <div v-else class="py-2">
              <div
                v-if="!searchQuery.trim()"
                class="px-4 py-2 text-xs font-semibold uppercase text-slate-500"
              >
                Suggested
              </div>

              <button
                v-for="(result, index) in searchResults"
                :key="result.id"
                :class="[
                  'flex w-full items-center gap-3 px-4 py-3 text-left transition-colors',
                  selectedIndex === index
                    ? 'bg-purple-500/20 text-white'
                    : 'text-slate-300 hover:bg-slate-700/50'
                ]"
                @click="selectResult(result)"
                @mouseenter="selectedIndex = index"
              >
                <!-- Icon -->
                <div
                  :class="[
                    'flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg',
                    result.type === 'feature' ? 'bg-purple-500/20 text-purple-400' :
                    result.type === 'concept' ? 'bg-blue-500/20 text-blue-400' :
                    'bg-emerald-500/20 text-emerald-400'
                  ]"
                >
                  <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getIcon(result.type)" />
                  </svg>
                </div>

                <!-- Content -->
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="font-medium truncate">{{ result.title }}</span>
                    <span
                      :class="[
                        'rounded-full px-2 py-0.5 text-[10px] font-medium uppercase',
                        result.type === 'feature' ? 'bg-purple-500/30 text-purple-300' :
                        result.type === 'concept' ? 'bg-blue-500/30 text-blue-300' :
                        'bg-emerald-500/30 text-emerald-300'
                      ]"
                    >
                      {{ result.type }}
                    </span>
                  </div>
                  <p class="text-sm text-slate-400 truncate">{{ result.description }}</p>
                </div>

                <!-- Action hint -->
                <div v-if="selectedIndex === index" class="flex-shrink-0 text-xs text-slate-500">
                  <span v-if="result.type === 'feature'">Start guide</span>
                  <span v-else>View</span>
                </div>
              </button>
            </div>
          </div>

          <!-- Footer -->
          <div class="flex items-center justify-between border-t border-slate-700 px-4 py-3 text-xs text-slate-500">
            <div class="flex items-center gap-4">
              <span class="flex items-center gap-1">
                <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5 py-0.5">↑</kbd>
                <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5 py-0.5">↓</kbd>
                to navigate
              </span>
              <span class="flex items-center gap-1">
                <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5 py-0.5">↵</kbd>
                to select
              </span>
            </div>
            <span class="text-slate-600">
              Powered by OrbitOS Help
            </span>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
