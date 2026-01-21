<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'
import type { OrgChartResource } from '~/types/operations'

const props = defineProps<{
  data: {
    person: OrgChartResource
  }
}>()

const person = computed(() => props.data.person)

const initials = computed(() => {
  if (person.value.isVacant) return '?'
  const name = person.value.name
  const parts = name.split(' ')
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return name.substring(0, 2).toUpperCase()
})
</script>

<template>
  <div
    :class="[
      'w-[150px] rounded-lg border p-2.5 transition-all cursor-pointer',
      person.isVacant
        ? 'bg-amber-500/10 border-amber-500/30 border-dashed hover:border-amber-400/50'
        : 'bg-purple-500/10 border-purple-500/30 hover:border-purple-400/50'
    ]"
  >
    <!-- Top handle (for incoming edge from manager) -->
    <Handle
      v-if="person.reportsToResourceId"
      type="target"
      :position="Position.Top"
      class="!bg-purple-400 !w-2 !h-2"
    />

    <div class="flex items-center gap-2">
      <!-- Avatar -->
      <div
        :class="[
          'w-8 h-8 rounded-full flex items-center justify-center text-xs font-medium flex-shrink-0',
          person.isVacant ? 'bg-amber-500/20 text-amber-300' : 'bg-purple-500/20 text-purple-300'
        ]"
      >
        {{ initials }}
      </div>

      <div class="flex-1 min-w-0">
        <div class="font-medium text-white text-xs truncate">
          {{ person.isVacant ? person.vacantPositionTitle : person.name }}
        </div>
        <div class="text-[10px] text-white/50 truncate">
          {{ person.description || (person.isVacant ? 'Vacant Position' : person.resourceSubtypeName) }}
        </div>
      </div>
    </div>

    <!-- Metrics -->
    <div
      v-if="person.directReportsCount > 0"
      class="mt-1.5 pt-1.5 border-t border-white/10 flex gap-2 text-[10px] text-white/40"
    >
      <span>{{ person.directReportsCount }} direct</span>
      <span v-if="person.indirectReportsCount > person.directReportsCount">
        {{ person.indirectReportsCount }} total
      </span>
    </div>

    <!-- Bottom handle (for outgoing edges to reports) -->
    <Handle
      v-if="person.directReportsCount > 0"
      type="source"
      :position="Position.Bottom"
      class="!bg-purple-400 !w-2 !h-2"
    />
  </div>
</template>
