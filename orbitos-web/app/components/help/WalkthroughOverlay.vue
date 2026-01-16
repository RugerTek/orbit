<script setup lang="ts">
/**
 * WalkthroughOverlay - Interactive Step-by-Step Guide
 *
 * Highlights UI elements and guides users through features
 * Generated from spec acceptance criteria
 * Tracks progress and allows skipping
 */

const {
  currentWalkthrough,
  currentWalkthroughStep,
  getCurrentWalkthroughStep,
  nextWalkthroughStep,
  previousWalkthroughStep,
  endWalkthrough
} = useHelp()

// Current step data
const currentStep = computed(() => getCurrentWalkthroughStep())

// Progress percentage
const progress = computed(() => {
  if (!currentWalkthrough.value) return 0
  return ((currentWalkthroughStep.value + 1) / currentWalkthrough.value.totalSteps) * 100
})

// Is this the last step?
const isLastStep = computed(() => {
  if (!currentWalkthrough.value) return false
  return currentWalkthroughStep.value >= currentWalkthrough.value.totalSteps - 1
})

// Is this the first step?
const isFirstStep = computed(() => {
  return currentWalkthroughStep.value === 0
})

// Step type styling
const stepTypeIcon = computed(() => {
  switch (currentStep.value?.type) {
    case 'highlight':
      return 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z'
    case 'action':
      return 'M13 10V3L4 14h7v7l9-11h-7z'
    case 'completion':
      return 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z'
    default:
      return 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
  }
})

const stepTypeColor = computed(() => {
  switch (currentStep.value?.type) {
    case 'highlight':
      return 'from-blue-500 to-cyan-500'
    case 'action':
      return 'from-amber-500 to-orange-500'
    case 'completion':
      return 'from-emerald-500 to-green-500'
    default:
      return 'from-purple-500 to-blue-500'
  }
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-opacity duration-300"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-200"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="currentWalkthrough && currentStep"
        class="fixed inset-0 z-[200]"
      >
        <!-- Backdrop with spotlight hole (for future enhancement) -->
        <div class="absolute inset-0 bg-black/70" @click="endWalkthrough" />

        <!-- Step Card -->
        <div class="absolute bottom-8 left-1/2 -translate-x-1/2 w-full max-w-lg px-4">
          <div class="rounded-2xl border border-slate-700 bg-slate-800/98 shadow-2xl backdrop-blur-xl overflow-hidden">
            <!-- Progress Bar -->
            <div class="h-1 bg-slate-700">
              <div
                class="h-full bg-gradient-to-r from-purple-500 to-blue-500 transition-all duration-300"
                :style="{ width: `${progress}%` }"
              />
            </div>

            <!-- Header -->
            <div class="flex items-center justify-between px-6 py-4 border-b border-slate-700/50">
              <div class="flex items-center gap-3">
                <div
                  :class="[
                    'flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br',
                    stepTypeColor
                  ]"
                >
                  <svg class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="stepTypeIcon" />
                  </svg>
                </div>
                <div>
                  <h3 class="font-semibold text-white">{{ currentWalkthrough.featureName }}</h3>
                  <p class="text-xs text-slate-400">
                    Step {{ currentWalkthroughStep + 1 }} of {{ currentWalkthrough.totalSteps }}
                  </p>
                </div>
              </div>
              <button
                class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
                @click="endWalkthrough"
              >
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <!-- Content -->
            <div class="px-6 py-5">
              <h4 class="text-lg font-semibold text-white mb-2">{{ currentStep.title }}</h4>
              <p class="text-slate-300 leading-relaxed">{{ currentStep.content }}</p>

              <!-- Step Type Indicator -->
              <div class="mt-4 flex items-center gap-2">
                <span
                  :class="[
                    'inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium',
                    currentStep.type === 'highlight' ? 'bg-blue-500/20 text-blue-300' :
                    currentStep.type === 'action' ? 'bg-amber-500/20 text-amber-300' :
                    'bg-emerald-500/20 text-emerald-300'
                  ]"
                >
                  <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="stepTypeIcon" />
                  </svg>
                  {{ currentStep.type === 'highlight' ? 'Look here' :
                     currentStep.type === 'action' ? 'Take action' :
                     'Complete!' }}
                </span>
              </div>
            </div>

            <!-- Navigation -->
            <div class="flex items-center justify-between border-t border-slate-700/50 px-6 py-4">
              <button
                :disabled="isFirstStep"
                :class="[
                  'flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors',
                  isFirstStep
                    ? 'text-slate-600 cursor-not-allowed'
                    : 'text-slate-300 hover:bg-slate-700'
                ]"
                @click="previousWalkthroughStep"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                </svg>
                Back
              </button>

              <div class="flex items-center gap-2">
                <!-- Step dots -->
                <div class="flex items-center gap-1">
                  <span
                    v-for="i in currentWalkthrough.totalSteps"
                    :key="i"
                    :class="[
                      'h-2 w-2 rounded-full transition-colors',
                      i - 1 === currentWalkthroughStep
                        ? 'bg-purple-500'
                        : i - 1 < currentWalkthroughStep
                          ? 'bg-purple-500/50'
                          : 'bg-slate-600'
                    ]"
                  />
                </div>
              </div>

              <button
                :class="[
                  'flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors',
                  isLastStep
                    ? 'bg-gradient-to-r from-emerald-500 to-green-500 text-white'
                    : 'bg-gradient-to-r from-purple-500 to-blue-500 text-white'
                ]"
                @click="nextWalkthroughStep"
              >
                {{ isLastStep ? 'Finish' : 'Next' }}
                <svg v-if="!isLastStep" class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
                <svg v-else class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
              </button>
            </div>

            <!-- Keyboard hints -->
            <div class="bg-slate-900/50 px-6 py-2 text-center text-xs text-slate-500">
              <span class="flex items-center justify-center gap-4">
                <span class="flex items-center gap-1">
                  <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5">←</kbd>
                  <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5">→</kbd>
                  navigate
                </span>
                <span class="flex items-center gap-1">
                  <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5">ESC</kbd>
                  exit
                </span>
              </span>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
