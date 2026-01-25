<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'
import type { ManualInputMetadata } from '~/types/activity-metadata'

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
const metadata = computed<ManualInputMetadata | null>(() => {
  if (!activity.value?.metadataJson) return null
  try {
    return JSON.parse(activity.value.metadataJson) as ManualInputMetadata
  } catch {
    return null
  }
})

// Get display text for input device
const inputDeviceText = computed(() => {
  if (!metadata.value?.inputDevice) return null
  const devices: Record<string, string> = {
    keyboard: 'Keyboard',
    scanner: 'Scanner',
    touchscreen: 'Touch',
    voice: 'Voice'
  }
  return devices[metadata.value.inputDevice] || metadata.value.inputDevice
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
      class="!w-3 !h-3 !bg-pink-500 !border-2 !border-white/30"
    />

    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-pink-500 !border-2 !border-white/30"
    />

    <!-- Parallelogram shape for Manual Input (IE symbol) -->
    <svg width="160" height="80" viewBox="0 0 160 80">
      <!-- Parallelogram (slanted top edge) -->
      <polygon
        points="30,10 150,10 130,70 10,70"
        :class="[
          'fill-white/5 stroke-2',
          selected ? 'stroke-pink-500' : 'stroke-pink-500/60'
        ]"
        stroke-width="2"
        stroke-linejoin="round"
      />
      <!-- Keyboard/input icon -->
      <rect x="55" y="30" width="50" height="20" rx="2" fill="none" stroke="currentColor" stroke-width="2" class="text-pink-400" />
      <line x1="65" y1="35" x2="65" y2="45" stroke="currentColor" stroke-width="1" class="text-pink-400/60" />
      <line x1="75" y1="35" x2="75" y2="45" stroke="currentColor" stroke-width="1" class="text-pink-400/60" />
      <line x1="85" y1="35" x2="85" y2="45" stroke="currentColor" stroke-width="1" class="text-pink-400/60" />
      <line x1="95" y1="35" x2="95" y2="45" stroke="currentColor" stroke-width="1" class="text-pink-400/60" />
    </svg>

    <!-- Content overlay -->
    <div class="absolute inset-0 flex flex-col items-center justify-end pb-2 pointer-events-none">
      <div class="text-white text-xs font-medium text-center leading-tight max-w-[120px] truncate">
        {{ activity.name }}
      </div>
      <div v-if="inputDeviceText" class="mt-0.5 text-[10px] text-pink-300/80">
        {{ inputDeviceText }}
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
    <svg width="160" height="80" viewBox="0 0 160 80">
      <polygon points="30,10 150,10 130,70 10,70" class="fill-red-900/20 stroke-red-500/50" stroke-width="2" />
    </svg>
  </div>
</template>
