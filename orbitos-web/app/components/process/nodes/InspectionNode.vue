<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { InspectionMetadata } from '~/types/activity-metadata'

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
const metadata = computed<InspectionMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as InspectionMetadata
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
      class="!w-3 !h-3 !bg-rose-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-rose-500 !border-2 !border-white/30"
    />

    <!-- Square shape for Inspection (IE symbol) -->
    <svg width="120" height="120" viewBox="0 0 120 120">
      <!-- Main square -->
      <rect
        x="10"
        y="10"
        width="100"
        height="100"
        rx="4"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-rose-500' : 'stroke-rose-500/60'
        ]"
        stroke-width="2"
      />
      <!-- Checkmark icon inside -->
      <path
        d="M40 60 L55 75 L80 45"
        fill="none"
        stroke="currentColor"
        stroke-width="3"
        stroke-linecap="round"
        stroke-linejoin="round"
        class="text-rose-400"
      />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-end p-3 pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[100px] truncate">
        {{ activity.name }}
      </div>
      <div v-if="metadata?.passRate" class="mt-0.5 text-[10px] text-rose-300/80">
        {{ metadata.passRate }}% pass
      </div>
      <div v-if="activity.assignedResource" class="mt-0.5 flex items-center gap-1 text-[10px] text-white/50">
        <span class="w-3 h-3 rounded-full bg-white/20 flex items-center justify-center text-[8px]">
          {{ activity.assignedResource.name.charAt(0) }}
        </span>
        <span class="truncate max-w-[60px]">{{ activity.assignedResource.name }}</span>
      </div>
    </div>
  </div>
  <!-- Fallback -->
  <div v-else class="relative">
    <Handle type="target" :position="Position.Top" class="!w-3 !h-3 !bg-red-500" />
    <Handle type="source" :position="Position.Bottom" class="!w-3 !h-3 !bg-red-500" />
    <svg width="120" height="120" viewBox="0 0 120 120">
      <rect x="10" y="10" width="100" height="100" rx="4" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
