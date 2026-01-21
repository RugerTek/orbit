<script setup lang="ts">
/**
 * HelpButton - Floating Help Access Button
 *
 * Provides quick access to:
 * - Help panel (click)
 * - Spotlight search (keyboard shortcut hint)
 */

const { openHelpPanel, openSpotlight, getContextualHelp } = useHelp()

const isExpanded = ref(false)

// Current context help
const contextHelp = computed(() => getContextualHelp())

// Toggle expanded state
const toggleExpand = () => {
  isExpanded.value = !isExpanded.value
}

// Handle main button click
const handleClick = () => {
  if (isExpanded.value) {
    isExpanded.value = false
  } else {
    openHelpPanel()
  }
}
</script>

<template>
  <div class="fixed bottom-6 left-6 z-40">
    <!-- Expanded Menu -->
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 scale-95 translate-y-2"
      enter-to-class="opacity-100 scale-100 translate-y-0"
      leave-active-class="transition-all duration-150 ease-in"
      leave-from-class="opacity-100 scale-100 translate-y-0"
      leave-to-class="opacity-0 scale-95 translate-y-2"
    >
      <div
        v-if="isExpanded"
        class="absolute bottom-16 left-0 w-64 rounded-xl border border-slate-700 bg-slate-800/95 p-3 shadow-xl backdrop-blur-xl"
      >
        <!-- Context Help -->
        <div v-if="contextHelp" class="mb-3 pb-3 border-b border-slate-700">
          <p class="text-xs text-slate-500 mb-1">Current page</p>
          <button
            class="w-full flex items-center gap-2 rounded-lg bg-purple-500/20 p-2 text-left text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
            @click="openHelpPanel(); isExpanded = false"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
            </svg>
            {{ contextHelp.name }} Guide
          </button>
        </div>

        <!-- Quick Actions -->
        <div class="space-y-1">
          <button
            class="w-full flex items-center justify-between rounded-lg px-3 py-2 text-sm text-slate-300 hover:bg-slate-700 transition-colors"
            @click="openSpotlight(); isExpanded = false"
          >
            <span class="flex items-center gap-2">
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              Search Help
            </span>
            <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5 py-0.5 text-[10px] text-slate-400">
              {{ navigator?.platform?.includes('Mac') ? '⌘K' : 'Ctrl+K' }}
            </kbd>
          </button>

          <NuxtLink
            to="/help"
            class="w-full flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-300 hover:bg-slate-700 transition-colors"
            @click="isExpanded = false"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            All Documentation
          </NuxtLink>

          <NuxtLink
            to="/help/concepts"
            class="w-full flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-300 hover:bg-slate-700 transition-colors"
            @click="isExpanded = false"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
            Concept Map
          </NuxtLink>
        </div>
      </div>
    </Transition>

    <!-- Main Button -->
    <div class="flex items-center gap-2">
      <!-- Expand button -->
      <button
        class="flex h-10 w-10 items-center justify-center rounded-full border border-slate-700 bg-slate-800/95 text-slate-400 shadow-lg hover:bg-slate-700 hover:text-white transition-all"
        @click="toggleExpand"
      >
        <svg
          :class="['h-5 w-5 transition-transform', isExpanded ? 'rotate-45' : '']"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
      </button>

      <!-- Help button -->
      <button
        class="flex h-12 w-12 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-cyan-600 text-white shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all hover:scale-105"
        @click="handleClick"
      >
        <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </button>
    </div>

    <!-- Keyboard hint -->
    <div class="absolute -top-8 left-1/2 -translate-x-1/2 whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity">
      <span class="rounded bg-slate-900 px-2 py-1 text-xs text-slate-400">
        Press <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1">?</kbd> for help
      </span>
    </div>
  </div>
</template>
