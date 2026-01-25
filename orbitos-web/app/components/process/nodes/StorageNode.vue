<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { StorageMetadata } from '~/types/activity-metadata'

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
const metadata = computed<StorageMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as StorageMetadata
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
      class="!w-3 !h-3 !bg-yellow-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-yellow-500 !border-2 !border-white/30"
    />

    <!-- Inverted triangle for Storage (IE symbol) -->
    <svg width="120" height="100" viewBox="0 0 120 100">
      <!-- Inverted triangle pointing down -->
      <polygon
        points="60,90 10,15 110,15"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-yellow-500' : 'stroke-yellow-500/60'
        ]"
        stroke-width="2"
        stroke-linejoin="round"
      />
      <!-- Storage/box icon inside -->
      <rect x="45" y="30" width="30" height="20" rx="2" fill="none" stroke="currentColor" stroke-width="2" class="text-yellow-400" />
      <line x1="45" y1="38" x2="75" y2="38" stroke="currentColor" stroke-width="2" class="text-yellow-400" />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-start pt-4 pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[90px] truncate">
        {{ activity.name }}
      </div>
      <div v-if="metadata?.location" class="mt-0.5 text-[10px] text-yellow-300/80 truncate max-w-[80px]">
        {{ metadata.location }}
      </div>
      <div v-if="activity.assignedResource" class="mt-0.5 flex items-center gap-1 text-[10px] text-white/50">
        <span class="w-3 h-3 rounded-full bg-white/20 flex items-center justify-center text-[8px]">
          {{ activity.assignedResource.name.charAt(0) }}
        </span>
        <span class="truncate max-w-[50px]">{{ activity.assignedResource.name }}</span>
      </div>
    </div>
  </div>
  <!-- Fallback -->
  <div v-else class="relative">
    <Handle type="target" :position="Position.Top" class="!w-3 !h-3 !bg-red-500" />
    <Handle type="source" :position="Position.Bottom" class="!w-3 !h-3 !bg-red-500" />
    <svg width="120" height="100" viewBox="0 0 120 100">
      <polygon points="60,90 10,15 110,15" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
