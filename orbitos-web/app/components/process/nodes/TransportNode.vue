<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { TransportMetadata } from '~/types/activity-metadata'

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
const metadata = computed<TransportMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as TransportMetadata
  } catch {
    return null
  }
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
      class="!w-3 !h-3 !bg-orange-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-orange-500 !border-2 !border-white/30"
    />

    <!-- Circle with arrow for Transport (IE symbol) -->
    <svg width="100" height="100" viewBox="0 0 100 100">
      <!-- Main circle -->
      <circle
        cx="50"
        cy="50"
        r="40"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-orange-500' : 'stroke-orange-500/60'
        ]"
        stroke-width="2"
      />
      <!-- Arrow pointing right -->
      <path
        d="M30 50 H65 M55 40 L65 50 L55 60"
        fill="none"
        stroke="currentColor"
        stroke-width="3"
        stroke-linecap="round"
        stroke-linejoin="round"
        class="text-orange-400"
      />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-end pb-2 pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[80px] truncate">
        {{ activity.name }}
      </div>
      <div v-if="metadata?.distance" class="mt-0.5 text-[10px] text-orange-300/80">
        {{ metadata.distance }}{{ metadata.distanceUnit?.charAt(0) || 'm' }}
      </div>
      <div v-if="activity.assignedResource" class="mt-0.5 flex items-center gap-1 text-[10px] text-white/50">
        <span class="w-3 h-3 rounded-full bg-white/20 flex items-center justify-center text-[8px]">
          {{ activity.assignedResource.name.charAt(0) }}
        </span>
      </div>
    </div>
  </div>
  <!-- Fallback -->
  <div v-else class="relative">
    <Handle type="target" :position="Position.Top" class="!w-3 !h-3 !bg-red-500" />
    <Handle type="source" :position="Position.Bottom" class="!w-3 !h-3 !bg-red-500" />
    <svg width="100" height="100" viewBox="0 0 100 100">
      <circle cx="50" cy="50" r="40" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
