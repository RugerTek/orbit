<script setup lang="ts">
export interface InnerDialogueStep {
  stepNumber: number
  type: 'Routing' | 'Consulting' | 'AgentToAgent' | 'Synthesis' | 'Reasoning'
  title: string
  description?: string
  agentId?: string
  agentName?: string
  query?: string
  response?: string
  tokensUsed?: number
}

const props = defineProps<{
  steps: InnerDialogueStep[]
  isCollapsible?: boolean
}>()

const isExpanded = ref(true)

// Get icon for step type
function getStepIcon(type: string): string {
  switch (type) {
    case 'Routing':
      return '🎯'
    case 'Consulting':
      return '💬'
    case 'AgentToAgent':
      return '🔄'
    case 'Synthesis':
      return '🔗'
    case 'Reasoning':
      return '💭'
    default:
      return '⚡'
  }
}

// Get color class for step type
function getStepColorClass(type: string): string {
  switch (type) {
    case 'Routing':
      return 'text-amber-400 border-amber-500/30 bg-amber-500/10'
    case 'Consulting':
      return 'text-blue-400 border-blue-500/30 bg-blue-500/10'
    case 'AgentToAgent':
      return 'text-purple-400 border-purple-500/30 bg-purple-500/10'
    case 'Synthesis':
      return 'text-green-400 border-green-500/30 bg-green-500/10'
    case 'Reasoning':
      return 'text-white/60 border-white/20 bg-white/5'
    default:
      return 'text-white/60 border-white/20 bg-white/5'
  }
}

// Truncate long responses
function truncateResponse(text?: string, maxLength = 200): string {
  if (!text) return ''
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength) + '...'
}

// Track expanded states for individual step responses
const expandedResponses = ref<Set<number>>(new Set())

function toggleResponse(stepNumber: number) {
  if (expandedResponses.value.has(stepNumber)) {
    expandedResponses.value.delete(stepNumber)
  } else {
    expandedResponses.value.add(stepNumber)
  }
}
</script>

<template>
  <div class="inner-dialogue-display">
    <!-- Header with toggle -->
    <button
      v-if="isCollapsible"
      class="w-full flex items-center gap-2 px-3 py-2 rounded-lg bg-white/5 hover:bg-white/10 transition-colors text-left mb-2"
      @click="isExpanded = !isExpanded"
    >
      <span class="text-purple-400">💭</span>
      <span class="flex-1 text-sm font-medium text-white/70">Inner Dialogue</span>
      <span class="text-xs text-white/40">{{ steps.length }} steps</span>
      <svg
        :class="['w-4 h-4 text-white/40 transition-transform', isExpanded ? 'rotate-180' : '']"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <!-- Non-collapsible header -->
    <div v-else class="flex items-center gap-2 px-1 py-1 mb-2">
      <span class="text-purple-400">💭</span>
      <span class="text-sm font-medium text-white/70">Inner Dialogue</span>
      <span class="text-xs text-white/40 ml-auto">{{ steps.length }} steps</span>
    </div>

    <!-- Steps -->
    <div
      v-if="isExpanded || !isCollapsible"
      class="space-y-2 border-l-2 border-purple-500/30 pl-4 ml-2"
    >
      <div
        v-for="step in steps"
        :key="step.stepNumber"
        class="relative"
      >
        <!-- Timeline dot -->
        <div class="absolute -left-[21px] top-2 w-2.5 h-2.5 rounded-full bg-purple-500/50 border-2 border-purple-500/70" />

        <!-- Step card -->
        <div :class="['rounded-lg border p-3', getStepColorClass(step.type)]">
          <!-- Step header -->
          <div class="flex items-start gap-2">
            <span class="text-lg">{{ getStepIcon(step.type) }}</span>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 flex-wrap">
                <span class="font-medium text-sm">{{ step.title }}</span>
                <span
                  v-if="step.agentName"
                  class="text-xs px-1.5 py-0.5 rounded bg-white/10"
                >
                  {{ step.agentName }}
                </span>
                <span
                  v-if="step.tokensUsed"
                  class="text-xs text-white/40"
                >
                  {{ step.tokensUsed }} tokens
                </span>
              </div>

              <!-- Description -->
              <p v-if="step.description" class="text-sm text-white/60 mt-1">
                {{ step.description }}
              </p>

              <!-- Query (for A2A) -->
              <div
                v-if="step.query"
                class="mt-2 text-xs bg-black/20 rounded px-2 py-1"
              >
                <span class="text-white/40">Query:</span>
                <span class="text-white/70 ml-1">{{ step.query }}</span>
              </div>

              <!-- Response preview/full -->
              <div v-if="step.response" class="mt-2">
                <button
                  v-if="step.response.length > 200"
                  class="text-xs text-purple-400 hover:text-purple-300 transition-colors"
                  @click.stop="toggleResponse(step.stepNumber)"
                >
                  {{ expandedResponses.has(step.stepNumber) ? 'Show less' : 'Show more' }}
                </button>
                <div class="text-sm text-white/70 mt-1 bg-black/20 rounded p-2 whitespace-pre-wrap">
                  {{
                    expandedResponses.has(step.stepNumber)
                      ? step.response
                      : truncateResponse(step.response)
                  }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.inner-dialogue-display {
  font-size: 0.875rem;
}
</style>
