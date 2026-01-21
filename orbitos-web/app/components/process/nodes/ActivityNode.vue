<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'

const props = defineProps<{
  id: string
  data: {
    activity?: ActivityWithDetails
    isSelected?: boolean
  }
  selected?: boolean
}>()

// Computed to safely access activity data
const activity = computed(() => props.data?.activity)

const typeColors: Record<string, string> = {
  manual: 'bg-blue-500',
  automated: 'bg-emerald-500',
  hybrid: 'bg-cyan-500',
  decision: 'bg-amber-500',
  handoff: 'bg-purple-500',
}

const typeIcons: Record<string, string> = {
  manual: 'M',
  automated: 'A',
  hybrid: 'H',
  decision: 'D',
  handoff: 'H',
}
</script>

<template>
  <div
    v-if="activity"
    :class="[
      'relative rounded-xl border-2 p-4 min-w-[200px] max-w-[250px] transition-all cursor-pointer',
      selected ? 'border-purple-500 bg-purple-900/40 shadow-lg shadow-purple-500/20' : 'border-white/20 bg-white/5',
      'hover:border-white/30 hover:bg-white/10'
    ]"
  >
    <!-- Target handle (top) -->
    <Handle
      type="target"
      :position="Position.Top"
      class="!w-3 !h-3 !bg-purple-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-purple-500 !border-2 !border-white/30"
    />

    <!-- Type indicator -->
    <div class="absolute -left-3 top-1/2 -translate-y-1/2">
      <div
        :class="[
          'h-6 w-6 rounded-full flex items-center justify-center text-xs font-bold text-white',
          typeColors[activity.activityType] || 'bg-gray-500'
        ]"
      >
        {{ typeIcons[activity.activityType] || '?' }}
      </div>
    </div>

    <!-- Content -->
    <div class="ml-2">
      <div class="font-medium text-white text-sm leading-tight">
        {{ activity.name }}
      </div>

      <div v-if="activity.assignedResource || activity.estimatedDuration" class="mt-2 flex flex-wrap items-center gap-2 text-xs text-white/50">
        <span v-if="activity.assignedResource" class="flex items-center gap-1">
          <span class="w-4 h-4 rounded-full bg-white/20 flex items-center justify-center text-[10px]">
            {{ activity.assignedResource.name.charAt(0) }}
          </span>
          {{ activity.assignedResource.name }}
        </span>
        <span v-if="activity.estimatedDuration" class="text-white/40">
          {{ activity.estimatedDuration }}m
        </span>
      </div>

      <!-- Function badge -->
      <div v-if="activity.function" class="mt-2">
        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] bg-blue-500/20 text-blue-300 border border-blue-500/30">
          {{ activity.function.name }}
        </span>
      </div>

      <!-- Subprocess link -->
      <div v-if="activity.linkedProcess" class="mt-1">
        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] bg-purple-500/20 text-purple-300 border border-purple-500/30">
          &rarr; {{ activity.linkedProcess.name }}
        </span>
      </div>
    </div>
  </div>
  <!-- Fallback if activity data is missing -->
  <div v-else class="relative rounded-xl border-2 border-red-500/50 bg-red-900/20 p-4 min-w-[200px]">
    <Handle type="target" :position="Position.Top" class="!w-3 !h-3 !bg-red-500" />
    <Handle type="source" :position="Position.Bottom" class="!w-3 !h-3 !bg-red-500" />
    <div class="text-red-300 text-sm">Loading...</div>
  </div>
</template>
