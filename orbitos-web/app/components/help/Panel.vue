<script setup lang="ts">
/**
 * HelpPanel - Contextual Slide-out Guide
 *
 * Shows help content relevant to the current page
 * Can be pinned or dismissed
 * Auto-loads content based on detected feature
 */

const {
  isHelpPanelOpen,
  closeHelpPanel,
  currentFeatureId,
  helpIndex,
  getContextualHelp,
  startWalkthrough,
  loadHelpIndex
} = useHelp()

const isPinned = ref(false)
const activeTab = ref<'guide' | 'concepts' | 'walkthrough'>('guide')
const featureContent = ref<string>('')
const isLoadingContent = ref(false)

// Get current feature info
const currentFeature = computed(() => {
  if (!helpIndex.value) return null
  const featureId = currentFeatureId.value || getContextualHelp()?.id
  return helpIndex.value.features.find(f => f.id === featureId) || helpIndex.value.features[0]
})

// Related concepts for current feature
const relatedConcepts = computed(() => {
  if (!helpIndex.value) return []
  // Return first 3 concepts as related
  return helpIndex.value.concepts.slice(0, 3)
})

// Load feature content when panel opens or feature changes
watch([isHelpPanelOpen, currentFeature], async ([open, feature]) => {
  if (open && feature) {
    await loadFeatureContent(feature.id)
  }
}, { immediate: true })

// Load markdown content for a feature
const loadFeatureContent = async (featureId: string) => {
  isLoadingContent.value = true
  try {
    const response = await fetch(`/docs/user-manual/features/${featureId}.md`)
    if (response.ok) {
      featureContent.value = await response.text()
    } else {
      featureContent.value = `# ${currentFeature.value?.name || 'Help'}\n\nContent is being generated...`
    }
  } catch {
    featureContent.value = 'Unable to load help content.'
  } finally {
    isLoadingContent.value = false
  }
}

// Simple markdown to HTML (basic conversion)
const renderedContent = computed(() => {
  let html = featureContent.value
    // Headers
    .replace(/^### (.*$)/gim, '<h3 class="text-lg font-semibold text-white mt-6 mb-2">$1</h3>')
    .replace(/^## (.*$)/gim, '<h2 class="text-xl font-bold text-white mt-8 mb-3">$1</h2>')
    .replace(/^# (.*$)/gim, '<h1 class="text-2xl font-bold text-white mb-4">$1</h1>')
    // Bold
    .replace(/\*\*(.*?)\*\*/g, '<strong class="font-semibold text-white">$1</strong>')
    // Lists
    .replace(/^\- (.*$)/gim, '<li class="ml-4 text-slate-300">$1</li>')
    .replace(/^(\d+)\. (.*$)/gim, '<li class="ml-4 text-slate-300">$2</li>')
    // Blockquotes
    .replace(/^> (.*$)/gim, '<blockquote class="border-l-4 border-purple-500 pl-4 italic text-slate-400 my-4">$1</blockquote>')
    // Code
    .replace(/`([^`]+)`/g, '<code class="bg-slate-700 px-1.5 py-0.5 rounded text-purple-300 text-sm">$1</code>')
    // Tables (basic)
    .replace(/\|([^|]+)\|/g, '<span class="text-slate-300">$1</span>')
    // Paragraphs
    .replace(/\n\n/g, '</p><p class="text-slate-300 my-3">')
    // Line breaks
    .replace(/\n/g, '<br>')

  return `<div class="prose-dark">${html}</div>`
})

// Handle starting walkthrough
const handleStartWalkthrough = () => {
  if (currentFeature.value) {
    startWalkthrough(currentFeature.value.id)
    closeHelpPanel()
  }
}

// Toggle pin state
const togglePin = () => {
  isPinned.value = !isPinned.value
}

// Initialize
onMounted(() => {
  loadHelpIndex()
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-transform duration-300 ease-out"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition-transform duration-200 ease-in"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="isHelpPanelOpen"
        class="fixed right-0 top-0 z-50 h-full w-full max-w-md border-l border-slate-700 bg-slate-800/98 shadow-2xl backdrop-blur-xl"
      >
        <!-- Header -->
        <div class="flex items-center justify-between border-b border-slate-700 bg-gradient-to-r from-purple-500/10 to-blue-500/10 px-4 py-3">
          <div class="flex items-center gap-3">
            <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-purple-500 to-blue-600">
              <svg class="h-4 w-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
              </svg>
            </div>
            <div>
              <h3 class="text-sm font-semibold text-white">{{ currentFeature?.name || 'Help Guide' }}</h3>
              <p class="text-xs text-slate-400">{{ currentFeature?.id || 'Documentation' }}</p>
            </div>
          </div>
          <div class="flex items-center gap-1">
            <button
              :class="[
                'rounded-lg p-1.5 transition-colors',
                isPinned ? 'bg-purple-500/30 text-purple-300' : 'text-slate-400 hover:bg-slate-700 hover:text-white'
              ]"
              :title="isPinned ? 'Unpin panel' : 'Pin panel'"
              @click="togglePin"
            >
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path v-if="isPinned" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
              </svg>
            </button>
            <button
              class="rounded-lg p-1.5 text-slate-400 hover:bg-slate-700 hover:text-white"
              @click="closeHelpPanel"
            >
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <!-- Tabs -->
        <div class="flex border-b border-slate-700">
          <button
            v-for="tab in ['guide', 'concepts', 'walkthrough'] as const"
            :key="tab"
            :class="[
              'flex-1 px-4 py-3 text-sm font-medium transition-colors',
              activeTab === tab
                ? 'border-b-2 border-purple-500 text-purple-300'
                : 'text-slate-400 hover:text-white'
            ]"
            @click="activeTab = tab"
          >
            {{ tab === 'guide' ? 'Guide' : tab === 'concepts' ? 'Concepts' : 'Walkthrough' }}
          </button>
        </div>

        <!-- Content -->
        <div class="h-[calc(100%-120px)] overflow-y-auto">
          <!-- Loading -->
          <div v-if="isLoadingContent" class="flex items-center justify-center py-12">
            <div class="orbitos-spinner orbitos-spinner-md"></div>
          </div>

          <!-- Guide Tab -->
          <div v-else-if="activeTab === 'guide'" class="p-4">
            <div v-html="renderedContent" class="text-sm leading-relaxed"></div>
          </div>

          <!-- Concepts Tab -->
          <div v-else-if="activeTab === 'concepts'" class="p-4 space-y-3">
            <p class="text-sm text-slate-400 mb-4">Related concepts to understand:</p>

            <NuxtLink
              v-for="concept in relatedConcepts"
              :key="concept.id"
              :to="`/help/concepts/${concept.id}`"
              class="flex items-center gap-3 rounded-lg bg-slate-700/40 p-3 hover:bg-slate-700/60 transition-colors"
              @click="closeHelpPanel"
            >
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-500/20 text-blue-400">
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
                </svg>
              </div>
              <div>
                <h4 class="font-medium text-white">{{ concept.name }}</h4>
                <p class="text-xs text-slate-400">{{ concept.id }}</p>
              </div>
            </NuxtLink>
          </div>

          <!-- Walkthrough Tab -->
          <div v-else-if="activeTab === 'walkthrough'" class="p-4">
            <div class="rounded-lg bg-gradient-to-br from-purple-500/20 to-blue-500/20 p-6 text-center">
              <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-purple-500/30">
                <svg class="h-8 w-8 text-purple-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <h3 class="text-lg font-semibold text-white mb-2">Interactive Walkthrough</h3>
              <p class="text-sm text-slate-400 mb-6">
                Learn {{ currentFeature?.name }} step by step with guided highlights and actions.
              </p>
              <button
                class="rounded-lg bg-gradient-to-r from-purple-500 to-blue-600 px-6 py-2.5 text-sm font-medium text-white hover:opacity-90 transition-opacity"
                @click="handleStartWalkthrough"
              >
                Start Walkthrough
              </button>
            </div>

            <div class="mt-6 space-y-3">
              <h4 class="text-sm font-medium text-slate-400">What you'll learn:</h4>
              <ul class="space-y-2">
                <li class="flex items-center gap-2 text-sm text-slate-300">
                  <svg class="h-4 w-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  Navigate the interface
                </li>
                <li class="flex items-center gap-2 text-sm text-slate-300">
                  <svg class="h-4 w-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  Complete key actions
                </li>
                <li class="flex items-center gap-2 text-sm text-slate-300">
                  <svg class="h-4 w-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  Understand best practices
                </li>
              </ul>
            </div>
          </div>
        </div>

        <!-- Footer -->
        <div class="absolute bottom-0 left-0 right-0 border-t border-slate-700 bg-slate-800/95 px-4 py-3">
          <div class="flex items-center justify-between text-xs text-slate-500">
            <span>Press <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1">?</kbd> for help</span>
            <span>Auto-synced from specs</span>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
