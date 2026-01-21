<script setup lang="ts">
import type { OrgChartResource } from '~/types/operations'

defineProps<{
  people: OrgChartResource[]
}>()

defineEmits<{
  select: [person: OrgChartResource]
}>()

const getInitials = (person: OrgChartResource) => {
  if (person.isVacant) return '?'
  const name = person.name
  const parts = name.split(' ')
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return name.substring(0, 2).toUpperCase()
}
</script>

<template>
  <div class="orbitos-glass-subtle rounded-xl overflow-hidden">
    <table class="w-full text-left text-sm">
      <thead class="bg-black/20 text-xs uppercase text-white/40">
        <tr>
          <th class="px-6 py-3">Person</th>
          <th class="px-6 py-3">Reports To</th>
          <th class="px-6 py-3 text-center">Direct Reports</th>
          <th class="px-6 py-3 text-center">Total Reports</th>
          <th class="px-6 py-3 text-center">Level</th>
        </tr>
      </thead>
      <tbody class="divide-y divide-white/10">
        <tr
          v-for="person in people"
          :key="person.id"
          @click="$emit('select', person)"
          class="hover:bg-white/5 transition-colors cursor-pointer"
        >
          <td class="px-6 py-4">
            <div class="flex items-center gap-3">
              <div
                :class="[
                  'w-8 h-8 rounded-full flex items-center justify-center text-xs font-medium',
                  person.isVacant ? 'bg-amber-500/20 text-amber-300' : 'bg-purple-500/20 text-purple-300'
                ]"
              >
                {{ getInitials(person) }}
              </div>
              <div>
                <div class="font-medium text-white" :class="{ 'text-amber-300': person.isVacant }">
                  {{ person.isVacant ? person.vacantPositionTitle : person.name }}
                </div>
                <div class="text-xs text-white/40">
                  {{ person.description || person.resourceSubtypeName }}
                </div>
              </div>
            </div>
          </td>
          <td class="px-6 py-4 text-white/70">
            {{ person.managerName || '—' }}
          </td>
          <td class="px-6 py-4 text-white/70 text-center">
            {{ person.directReportsCount }}
          </td>
          <td class="px-6 py-4 text-white/70 text-center">
            {{ person.indirectReportsCount }}
          </td>
          <td class="px-6 py-4 text-center">
            <span class="rounded-full bg-purple-500/20 px-2 py-0.5 text-xs text-purple-300">
              Level {{ person.managementDepth }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Empty state -->
    <div v-if="people.length === 0" class="p-8 text-center text-white/40">
      No people found in the organization.
    </div>
  </div>
</template>
