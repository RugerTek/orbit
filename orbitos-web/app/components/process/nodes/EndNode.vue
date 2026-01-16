<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'

const props = defineProps<{
  id: string
  data: {
    output?: string
  }
}>()
</script>

<template>
  <div
    class="relative rounded-full border-2 border-red-500/50 bg-red-500/20 px-6 py-3 min-w-[100px] text-center"
  >
    <!-- Target handle (top) -->
    <Handle
      type="target"
      :position="Position.Top"
      class="!w-3 !h-3 !bg-red-500 !border-2 !border-white/30"
    />

    <div class="text-red-400 font-semibold text-sm">End</div>
    <div v-if="data.output" class="text-white/50 text-xs mt-1 max-w-[150px] truncate">
      {{ data.output }}
    </div>
  </div>
</template>
