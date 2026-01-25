<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { DisplayMetadata } from '~/types/activity-metadata'

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
const metadata = computed<DisplayMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as DisplayMetadata
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
      class="!w-3 !h-3 !bg-sky-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-sky-500 !border-2 !border-white/30"
    />

    <!-- Display/Monitor shape (angled sides like CRT) for Display (IE symbol) -->
    <svg width="160" height="100" viewBox="0 0 160 100">
      <!-- Monitor shape with angled sides -->
      <polygon
        points="20,15 140,15 150,85 10,85"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-sky-500' : 'stroke-sky-500/60'
        ]"
        stroke-width="2"
        stroke-linejoin="round"
      />
      <!-- Screen area -->
      <rect x="35" y="25" width="90" height="45" rx="2" fill="none" stroke="currentColor" stroke-width="1" class="text-sky-400/40" />
      <!-- Screen content lines -->
      <line x1="45" y1="35" x2="100" y2="35" stroke="currentColor" stroke-width="2" class="text-sky-400/50" />
      <line x1="45" y1="48" x2="85" y2="48" stroke="currentColor" stroke-width="2" class="text-sky-400/50" />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-end pb-2 pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[130px] truncate">
        {{ activity.name }}
      </div>
      <div v-if="metadata?.displayLocation" class="mt-0.5 text-[10px] text-sky-300/80 truncate max-w-[100px]">
        {{ metadata.displayLocation }}
      </div>
      <div v-if="activity.assignedResource" class="mt-0.5 flex items-center gap-1 text-[10px] text-white/50">
        <span class="w-3 h-3 rounded-full bg-white/20 flex items-center justify-center text-[8px]">
          {{ activity.assignedResource.name.charAt(0) }}
        </span>
        <span class="truncate max-w-[70px]">{{ activity.assignedResource.name }}</span>
      </div>
    </div>
  </div>
  <!-- Fallback -->
  <div v-else class="relative">
    <Handle type="target" :position="Position.Top" class="!w-3 !h-3 !bg-red-500" />
    <Handle type="source" :position="Position.Bottom" class="!w-3 !h-3 !bg-red-500" />
    <svg width="160" height="100" viewBox="0 0 160 100">
      <polygon points="20,15 140,15 150,85 10,85" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
