<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'

const props = defineProps<{
  id: string
  data: {
    trigger?: string
  }
}>()
</script>

<template>
  <div
    class="relative rounded-full border-2 border-emerald-500/50 bg-emerald-500/20 px-6 py-3 min-w-[100px] text-center"
  >
    <!-- Source handle (bottom) -->
    <Handle
      type="source"
      :position="Position.Bottom"
      class="!w-3 !h-3 !bg-emerald-500 !border-2 !border-white/30"
    />

    <div class="text-emerald-400 font-semibold text-sm">Start</div>
    <div v-if="data.trigger" class="text-white/50 text-xs mt-1 max-w-[150px] truncate">
      {{ data.trigger }}
    </div>
  </div>
</template>
