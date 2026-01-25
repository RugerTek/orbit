<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { DocumentMetadata } from '~/types/activity-metadata'

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
const metadata = computed<DocumentMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as DocumentMetadata
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
      class="!w-3 !h-3 !bg-indigo-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-indigo-500 !border-2 !border-white/30"
    />

    <!-- Document shape with wavy bottom (IE symbol) -->
    <svg width="160" height="100" viewBox="0 0 160 100">
      <!-- Document shape: rectangle with wavy bottom edge -->
      <path
        d="M10 10 L150 10 L150 75 Q130 85, 110 75 Q90 65, 70 75 Q50 85, 30 75 Q15 68, 10 75 Z"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-indigo-500' : 'stroke-indigo-500/60'
        ]"
        stroke-width="2"
      />
      <!-- Document lines (text representation) -->
      <line x1="30" y1="30" x2="130" y2="30" stroke="currentColor" stroke-width="2" class="text-indigo-400/60" />
      <line x1="30" y1="45" x2="110" y2="45" stroke="currentColor" stroke-width="2" class="text-indigo-400/60" />
      <line x1="30" y1="60" x2="90" y2="60" stroke="currentColor" stroke-width="2" class="text-indigo-400/60" />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[130px] truncate">
        {{ activity.name }}
      </div>
      <div v-if="metadata?.documentCode" class="mt-0.5 text-[10px] text-indigo-300/80">
        {{ metadata.documentCode }}
      </div>
      <div v-if="activity.assignedResource" class="mt-0.5 flex items-center gap-1 text-[10px] text-white/50">
        <span class="w-3 h-3 rounded-full bg-white/20 flex items-center justify-center text-[8px]">
          {{ activity.assignedResource.name.charAt(0) }}
        </span>
        <span class="truncate max-w-[80px]">{{ activity.assignedResource.name }}</span>
      </div>
    </div>
  </div>
  <!-- Fallback -->
  <div v-else class="relative">
    <Handle type="target" :position="Position.Top" class="!w-3 !h-3 !bg-red-500" />
    <Handle type="source" :position="Position.Bottom" class="!w-3 !h-3 !bg-red-500" />
    <svg width="160" height="100" viewBox="0 0 160 100">
      <path d="M10 10 L150 10 L150 75 Q130 85, 110 75 Q90 65, 70 75 Q50 85, 30 75 Q15 68, 10 75 Z" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
