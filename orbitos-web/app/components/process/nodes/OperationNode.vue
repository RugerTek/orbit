<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { OperationMetadata } from '~/types/activity-metadata'

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
const metadata = computed<OperationMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as OperationMetadata
  } catch {
    return null
  }
})

// Format time for display
const formattedTime = computed(() => {
  const duration = activity.value?.durationMinutes
  if (!duration) return null
  if (duration >= 60) {
    const hours = Math.floor(duration / 60)
    const mins = duration % 60
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`
  }
  return `${duration}m`
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
      class="!w-3 !h-3 !bg-emerald-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-emerald-500 !border-2 !border-white/30"
    />

    <!-- Large circle shape for Operation (ASME standard value-adding activity) -->
    <svg width="140" height="140" viewBox="0 0 140 140">
      <!-- Main circle -->
      <circle
        cx="70"
        cy="70"
        r="60"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-emerald-500' : 'stroke-emerald-500/60'
        ]"
        stroke-width="3"
      />
      <!-- Gear/cog icon inside to represent value-adding work -->
      <g class="text-emerald-400" fill="currentColor">
        <!-- Outer gear teeth -->
        <path
          d="M70 30 L73 38 L77 36 L80 28 L70 30Z
             M90 35 L87 43 L91 46 L98 41 L90 35Z
             M105 55 L97 58 L99 63 L107 65 L105 55Z
             M110 75 L102 75 L102 80 L110 82 L110 75Z
             M105 95 L97 92 L95 97 L100 105 L105 95Z
             M90 110 L87 102 L82 104 L80 112 L90 110Z
             M70 115 L70 107 L65 107 L63 115 L70 115Z
             M50 110 L53 102 L48 100 L42 107 L50 110Z
             M35 95 L43 92 L41 87 L33 85 L35 95Z
             M30 75 L38 75 L38 70 L30 68 L30 75Z
             M35 55 L43 58 L45 53 L38 45 L35 55Z
             M50 35 L53 43 L58 41 L60 33 L50 35Z"
          opacity="0.6"
        />
        <!-- Inner circle -->
        <circle cx="70" cy="70" r="25" fill="none" stroke="currentColor" stroke-width="2" opacity="0.8" />
        <!-- Center dot -->
        <circle cx="70" cy="70" r="8" opacity="0.9" />
      </g>
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-end p-4 pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[120px] truncate">
        {{ activity.name }}
      </div>
      <div class="mt-0.5 flex items-center gap-2 text-[10px] text-emerald-300/80">
        <span v-if="formattedTime">{{ formattedTime }}</span>
        <span v-if="metadata?.workstation" class="truncate max-w-[60px]">{{ metadata.workstation }}</span>
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
    <svg width="140" height="140" viewBox="0 0 140 140">
      <circle cx="70" cy="70" r="60" class="fill-red-900/20 stroke-red-500/50" stroke-width="3" />
    </svg>
  </div>
</template>
