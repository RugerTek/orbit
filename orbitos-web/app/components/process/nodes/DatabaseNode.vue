<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { DatabaseMetadata } from '~/types/activity-metadata'

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
const metadata = computed<DatabaseMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as DatabaseMetadata
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
      class="!w-3 !h-3 !bg-teal-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-teal-500 !border-2 !border-white/30"
    />

    <!-- Cylinder shape for Database (IE symbol) -->
    <svg width="100" height="130" viewBox="0 0 100 130">
      <!-- Top ellipse -->
      <ellipse
        cx="50"
        cy="25"
        rx="40"
        ry="15"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-teal-500' : 'stroke-teal-500/60'
        ]"
        stroke-width="2"
      />
      <!-- Cylinder body (two vertical lines + bottom ellipse) -->
      <path
        d="M10 25 L10 105 A40 15 0 0 0 90 105 L90 25"
        fill="none"
        :class="[
          'stroke-2',
          selected ? 'stroke-teal-500' : 'stroke-teal-500/60'
        ]"
        stroke-width="2"
      />
      <!-- Fill for cylinder body -->
      <path
        d="M10 25 L10 105 A40 15 0 0 0 90 105 L90 25 A40 15 0 0 1 10 25"
        class="fill-white/5"
      />
      <!-- Middle line to show depth -->
      <ellipse cx="50" cy="60" rx="40" ry="10" fill="none" stroke="currentColor" stroke-width="1" stroke-dasharray="4 4" class="text-teal-400/40" />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[80px] truncate mt-4">
        {{ activity.name }}
      </div>
      <div v-if="metadata?.systemName" class="mt-0.5 text-[10px] text-teal-300/80 truncate max-w-[70px]">
        {{ metadata.systemName }}
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
    <svg width="100" height="130" viewBox="0 0 100 130">
      <ellipse cx="50" cy="25" rx="40" ry="15" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
      <path d="M10 25 L10 105 A40 15 0 0 0 90 105 L90 25" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
