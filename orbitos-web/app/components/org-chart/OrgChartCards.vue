<script setup lang="ts">
import type { OrgChartResource } from '~/types/operations'

const props = defineProps<{
  people: OrgChartResource[]
}>()

defineEmits<{
  select: [person: OrgChartResource]
}>()

// Group by management level
const groupedByLevel = computed(() => {
  const groups = new Map<number, OrgChartResource[]>()
  for (const person of props.people) {
    const level = person.managementDepth
    if (!groups.has(level)) groups.set(level, [])
    groups.get(level)!.push(person)
  }
  return Array.from(groups.entries()).sort((a, b) => a[0] - b[0])
})

const getInitials = (person: OrgChartResource) => {
  if (person.isVacant) return '?'
  const name = person.name
  const parts = name.split(' ')
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return name.substring(0, 2).toUpperCase()
}

const getLevelLabel = (level: number) => {
  if (level === 0) return 'Leadership'
  if (level === 1) return 'Directors'
  if (level === 2) return 'Managers'
  return `Level ${level}`
}
</script>

<template>
  <div class="space-y-8">
    <div v-for="[level, levelPeople] in groupedByLevel" :key="level">
      <h3 class="text-sm font-medium text-white/60 mb-3">
        {{ getLevelLabel(level) }}
        <span class="text-white/40">({{ levelPeople.length }} {{ levelPeople.length === 1 ? 'person' : 'people' }})</span>
      </h3>
      <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        <div
          v-for="person in levelPeople"
          :key="person.id"
          @click="$emit('select', person)"
          :class="[
            'orbitos-glass-subtle rounded-xl p-4 cursor-pointer transition-all hover:border-purple-500/50 border',
            person.isVacant ? 'border-dashed border-amber-500/30' : 'border-transparent'
          ]"
        >
          <div class="flex items-center gap-3">
            <div
              :class="[
                'w-12 h-12 rounded-full flex items-center justify-center text-lg font-medium flex-shrink-0',
                person.isVacant ? 'bg-amber-500/20 text-amber-300' : 'bg-purple-500/20 text-purple-300'
              ]"
            >
              {{ getInitials(person) }}
            </div>
            <div class="flex-1 min-w-0">
              <div class="font-medium text-white truncate" :class="{ 'text-amber-300': person.isVacant }">
                {{ person.isVacant ? person.vacantPositionTitle : person.name }}
              </div>
              <div class="text-sm text-white/50 truncate">
                {{ person.description || (person.isVacant ? 'Vacant Position' : person.resourceSubtypeName) }}
              </div>
            </div>
          </div>

          <div class="mt-4 pt-3 border-t border-white/10 grid grid-cols-2 gap-2 text-xs">
            <div>
              <span class="text-white/40">Reports to:</span>
              <div class="text-white/70 truncate">{{ person.managerName || 'None' }}</div>
            </div>
            <div>
              <span class="text-white/40">Direct reports:</span>
              <div class="text-white/70">{{ person.directReportsCount }}</div>
            </div>
          </div>

          <!-- Vacancy badge -->
          <div v-if="person.isVacant" class="mt-3">
            <span class="inline-flex items-center gap-1 rounded-full bg-amber-500/20 px-2 py-0.5 text-xs text-amber-300">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              Open Position
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div v-if="people.length === 0" class="orbitos-glass-subtle rounded-xl p-8 text-center text-white/40">
      No people found in the organization.
    </div>
  </div>
</template>
