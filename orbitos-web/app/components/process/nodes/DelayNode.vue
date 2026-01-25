<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { DelayMetadata } from '~/types/activity-metadata'

const props = defineProps<{
  id: string
  data: {
    activity?: ActivityWithDetails
    isSelected?: boolean
  }
  selected?: boolean
}>()

const activity = computed(() => props.data?.activity)

// Parse metadata for display
const metadata = computed<DelayMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as DelayMetadata
  } catch {
    return null
  }
})

// Get display text for delay reason
const delayReasonText = computed(() => {
  if (!metadata.value?.delayReason) return null
  const reasons: Record<string, string> = {
    queue: 'Queue',
    batch: 'Batch',
    approval: 'Approval',
    curing: 'Curing',
    drying: 'Drying',
    cooling: 'Cooling',
    scheduling: 'Scheduling'
  }
  return reasons[metadata.value.delayReason] || metadata.value.delayReason
})
</script>

<template>
  <div
    v-if="activity"
    :class="[
      'relative transition-all cursor-pointer',
      selected ? 'drop-shadow-lg' : ''
    ]"
  >
    <!-- Target handle (top) -->
    <Handle
      type="target"
      :position="Position.Top"
      class="!w-3 !h-3 !bg-gray-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-gray-500 !border-2 !border-white/30"
    />

    <!-- D-shape (semicircle) for Delay (IE symbol) -->
    <svg width="120" height="80" viewBox="0 0 120 80">
      <!-- D-shape: left side is flat, right side is curved -->
      <path
        d="M20 10 L20 70 L60 70 A30 30 0 0 0 60 10 Z"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-gray-400' : 'stroke-gray-500/60'
        ]"
        stroke-width="2"
      />
      <!-- Clock/timer icon -->
      <circle cx="50" cy="40" r="12" fill="none" stroke="currentColor" stroke-width="2" class="text-gray-400" />
      <path d="M50 32 V40 H56" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" class="text-gray-400" />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-start justify-end p-2 pl-4 pointer-events-none">
      <div class="text-white text-xs font-medium leading-tight max-w-[70px] truncate">
        {{ activity.name }}
      </div>
      <div v-if="metadata?.averageWaitMinutes" class="mt-0.5 text-[10px] text-gray-300/80">
        ~{{ metadata.averageWaitMinutes }}m
      </div>
      <div v-else-if="activity.estimatedDuration" class="mt-0.5 text-[10px] text-white/40">
        {{ activity.estimatedDuration }}m wait
      </div>
      <div v-if="delayReasonText" class="text-[9px] text-gray-400/60">
        {{ delayReasonText }}
      </div>
    </div>
  </div>
  <!-- Fallback -->
  <div v-else class="relative">
    <Handle type="target" :position="Position.Top" class="!w-3 !h-3 !bg-red-500" />
    <Handle type="source" :position="Position.Bottom" class="!w-3 !h-3 !bg-red-500" />
    <svg width="120" height="80" viewBox="0 0 120 80">
      <path d="M20 10 L20 70 L60 70 A30 30 0 0 0 60 10 Z" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
