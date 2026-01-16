<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { ActivityWithDetails } from '~/types/operations'

const props = defineProps<{
  id: string
  data: {
    activity: ActivityWithDetails
    isSelected?: boolean
  }
  selected?: boolean
}>()
</script>

<template>
  <div class="relative">
    <!-- Target handle (top) -->
    <Handle
      type="target"
      :position="Position.Top"
      class="!w-3 !h-3 !bg-amber-500 !border-2 !border-white/30"
      style="top: -6px;"
    />

    <!-- Diamond shape container -->
    <div
      :class="[
        'w-32 h-32 rotate-45 rounded-lg border-2 flex items-center justify-center transition-all cursor-pointer',
        selected ? 'border-amber-500 bg-amber-900/40 shadow-lg shadow-amber-500/20' : 'border-amber-500/40 bg-amber-500/10',
        'hover:border-amber-500/60 hover:bg-amber-500/20'
      ]"
    >
      <!-- Content (rotated back) -->
      <div class="-rotate-45 text-center p-2 max-w-[100px]">
        <div class="font-medium text-white text-xs leading-tight">
          {{ data.activity.name }}
        </div>
      </div>
    </div>

    <!-- Yes handle (right) - green -->
    <Handle
      id="yes"
      type="source"
      :position="Position.Right"
      class="!w-3 !h-3 !bg-emerald-500 !border-2 !border-white/30"
      style="right: -6px; top: 50%;"
    />

    <!-- No handle (bottom) - red -->
    <Handle
      id="no"
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-red-500 !border-2 !border-white/30"
      style="bottom: -6px;"
    />

    <!-- Labels for handles -->
    <div class="absolute -right-8 top-1/2 -translate-y-1/2 text-xs text-emerald-400 font-medium">
      Yes
    </div>
    <div class="absolute -bottom-6 left-1/2 -translate-x-1/2 text-xs text-red-400 font-medium">
      No
    </div>
  </div>
</template>
