<script setup lang="ts">
import type { CapabilityLevel } from '~/types/operations'

definePageMeta({
  layout: 'app'
})

const {
  people,
  functions,
  functionCapabilities,
  fetchPeople,
  fetchFunctions,
  fetchFunctionCapabilities,
  createFunctionCapability,
  updateFunctionCapability,
  deleteFunctionCapability,
  isLoading,
} = useOperations()

// Local state
const isLoadingData = ref(true)
const isSubmitting = ref(false)
const selectedLevel = ref<CapabilityLevel>('capable')
const searchPeople = ref('')
const searchFunctions = ref('')
const viewMode = ref<'checkbox' | 'dropdown'>('checkbox')

// Capability levels
const capabilityLevels: { value: CapabilityLevel; label: string; color: string; shortLabel: string }[] = [
  { value: 'learning', label: 'Learning', shortLabel: 'Learn', color: 'bg-blue-500' },
  { value: 'capable', label: 'Capable', shortLabel: 'Cap', color: 'bg-emerald-500' },
  { value: 'proficient', label: 'Proficient', shortLabel: 'Prof', color: 'bg-purple-500' },
  { value: 'expert', label: 'Expert', shortLabel: 'Exp', color: 'bg-amber-500' },
  { value: 'trainer', label: 'Trainer', shortLabel: 'Train', color: 'bg-rose-500' },
]

// Load data
onMounted(async () => {
  isLoadingData.value = true
  try {
    await Promise.all([
      fetchPeople(),
      fetchFunctions(),
      fetchFunctionCapabilities(),
    ])
  } catch (e) {
    console.error('Failed to load data:', e)
  } finally {
    isLoadingData.value = false
  }
})

// Filtered people
const filteredPeople = computed(() => {
  if (!searchPeople.value) return people.value
  const query = searchPeople.value.toLowerCase()
  return people.value.filter(p =>
    p.name.toLowerCase().includes(query) ||
    p.description?.toLowerCase().includes(query)
  )
})

// Filtered functions
const filteredFunctions = computed(() => {
  if (!searchFunctions.value) return functions.value
  const query = searchFunctions.value.toLowerCase()
  return functions.value.filter(f =>
    f.name.toLowerCase().includes(query) ||
    f.category?.toLowerCase().includes(query)
  )
})

// Get capability for a person-function pair
const getCapability = (personId: string, functionId: string) => {
  return functionCapabilities.value.find(
    fc => fc.resourceId === personId && fc.functionId === functionId
  )
}

// Toggle assignment (checkbox mode)
const toggleAssignment = async (personId: string, functionId: string) => {
  const existing = getCapability(personId, functionId)
  isSubmitting.value = true

  try {
    if (existing) {
      await deleteFunctionCapability(existing.id)
    } else {
      await createFunctionCapability({
        resourceId: personId,
        functionId,
        level: selectedLevel.value,
      })
    }
  } catch (e) {
    console.error('Failed to toggle assignment:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Handle dropdown level change
const handleLevelChange = async (personId: string, functionId: string, newLevel: CapabilityLevel | '') => {
  const existing = getCapability(personId, functionId)
  isSubmitting.value = true

  try {
    if (newLevel === '') {
      // Remove assignment
      if (existing) {
        await deleteFunctionCapability(existing.id)
      }
    } else if (existing) {
      // Update existing assignment
      if (existing.level !== newLevel) {
        await updateFunctionCapability(existing.id, { level: newLevel })
      }
    } else {
      // Create new assignment
      await createFunctionCapability({
        resourceId: personId,
        functionId,
        level: newLevel,
      })
    }
  } catch (e) {
    console.error('Failed to update assignment:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Get level color
const getLevelColor = (level: CapabilityLevel) => {
  const found = capabilityLevels.find(l => l.value === level)
  return found?.color || 'bg-slate-500'
}

// Get border color for dropdown
const getLevelBorderColor = (level: CapabilityLevel | undefined) => {
  if (!level) return 'border-white/10'
  const colors: Record<CapabilityLevel, string> = {
    learning: 'border-blue-500',
    capable: 'border-emerald-500',
    proficient: 'border-purple-500',
    expert: 'border-amber-500',
    trainer: 'border-rose-500',
  }
  return colors[level] || 'border-white/10'
}

// Stats
const stats = computed(() => {
  const totalAssignments = functionCapabilities.value.length
  const uncoveredFunctions = functions.value.filter(f =>
    !functionCapabilities.value.some(fc => fc.functionId === f.id)
  ).length
  const spofFunctions = functions.value.filter(f => {
    const caps = functionCapabilities.value.filter(fc => fc.functionId === f.id)
    return caps.length === 1
  }).length

  return { totalAssignments, uncoveredFunctions, spofFunctions }
})

// Get initials
const getInitials = (name: string) => {
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Assignment Matrix</h1>
        <p class="orbitos-text">Manage who can perform which functions at a glance.</p>
      </div>
      <div class="flex items-center gap-4">
        <!-- View mode toggle -->
        <div class="flex items-center gap-2">
          <button
            @click="viewMode = 'checkbox'"
            class="px-3 py-1.5 text-sm rounded-lg transition-all"
            :class="viewMode === 'checkbox'
              ? 'bg-purple-500/20 text-purple-300 border border-purple-500/50'
              : 'text-white/60 hover:text-white/80 border border-transparent'"
          >
            Checkbox
          </button>
          <button
            @click="viewMode = 'dropdown'"
            class="px-3 py-1.5 text-sm rounded-lg transition-all"
            :class="viewMode === 'dropdown'
              ? 'bg-purple-500/20 text-purple-300 border border-purple-500/50'
              : 'text-white/60 hover:text-white/80 border border-transparent'"
          >
            Dropdown
          </button>
        </div>
        <!-- Default level (only shown in checkbox mode) -->
        <div v-if="viewMode === 'checkbox'" class="flex items-center gap-3">
          <label class="text-sm text-white/60">Default level:</label>
          <select v-model="selectedLevel" class="orbitos-input w-auto py-1.5 text-sm">
            <option v-for="level in capabilityLevels" :key="level.value" :value="level.value">
              {{ level.label }}
            </option>
          </select>
        </div>
      </div>
    </div>

    <!-- Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total Assignments</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ stats.totalAssignments }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">People</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ people.length }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Uncovered Functions</div>
        <div class="mt-1 text-2xl font-semibold" :class="stats.uncoveredFunctions > 0 ? 'text-red-300' : 'text-emerald-300'">
          {{ stats.uncoveredFunctions }}
        </div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">SPOF Functions</div>
        <div class="mt-1 text-2xl font-semibold" :class="stats.spofFunctions > 0 ? 'text-amber-300' : 'text-emerald-300'">
          {{ stats.spofFunctions }}
        </div>
      </div>
    </div>

    <!-- Legend -->
    <div class="flex items-center gap-4 text-sm">
      <span class="text-white/40">Capability levels:</span>
      <div v-for="level in capabilityLevels" :key="level.value" class="flex items-center gap-1.5">
        <div :class="['w-3 h-3 rounded', level.color]"></div>
        <span class="text-white/60">{{ level.label }}</span>
      </div>
    </div>

    <!-- Loading state -->
    <div v-if="isLoadingData || isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Matrix -->
    <div v-else class="orbitos-glass-subtle overflow-hidden">
      <!-- Filters -->
      <div class="border-b border-white/10 px-4 py-3 flex flex-col md:flex-row gap-3">
        <div class="flex-1">
          <input
            v-model="searchPeople"
            type="text"
            class="orbitos-input py-1.5 text-sm"
            placeholder="Filter people..."
          />
        </div>
        <div class="flex-1">
          <input
            v-model="searchFunctions"
            type="text"
            class="orbitos-input py-1.5 text-sm"
            placeholder="Filter functions..."
          />
        </div>
      </div>

      <!-- Empty state -->
      <div v-if="filteredPeople.length === 0 || filteredFunctions.length === 0" class="p-12 text-center">
        <p class="text-white/40">
          {{ filteredPeople.length === 0 ? 'No people found' : 'No functions found' }}
        </p>
      </div>

      <!-- Matrix table -->
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-black/20">
              <th class="sticky left-0 z-10 bg-slate-800 px-4 py-3 text-left text-xs uppercase text-white/40 min-w-48">
                Person / Function
              </th>
              <th
                v-for="func in filteredFunctions"
                :key="func.id"
                class="px-2 py-3 text-center"
                :class="viewMode === 'dropdown' ? 'min-w-28' : 'min-w-24'"
              >
                <NuxtLink
                  :to="`/app/functions/${func.id}`"
                  class="text-xs text-white/60 hover:text-white block truncate"
                  :class="viewMode === 'dropdown' ? 'max-w-28' : 'max-w-24'"
                  :title="func.name"
                >
                  {{ func.name }}
                </NuxtLink>
                <div class="text-[10px] text-white/30 truncate">{{ func.category || 'Uncategorized' }}</div>
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/5">
            <tr v-for="person in filteredPeople" :key="person.id" class="hover:bg-white/5">
              <td class="sticky left-0 z-10 bg-slate-800/95 px-4 py-2">
                <NuxtLink :to="`/app/people/${person.id}`" class="flex items-center gap-3 group">
                  <div class="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-xs font-medium text-white">
                    {{ getInitials(person.name) }}
                  </div>
                  <div>
                    <div class="text-white group-hover:text-purple-300 text-sm">{{ person.name }}</div>
                    <div class="text-[10px] text-white/40">{{ person.description || 'No role' }}</div>
                  </div>
                </NuxtLink>
              </td>
              <td
                v-for="func in filteredFunctions"
                :key="func.id"
                class="px-2 py-2 text-center"
              >
                <!-- Checkbox mode -->
                <button
                  v-if="viewMode === 'checkbox'"
                  @click="toggleAssignment(person.id, func.id)"
                  :disabled="isSubmitting"
                  class="w-8 h-8 rounded-lg border transition-all flex items-center justify-center mx-auto disabled:opacity-50"
                  :class="getCapability(person.id, func.id)
                    ? `${getLevelColor(getCapability(person.id, func.id)!.level)} border-transparent`
                    : 'border-white/10 hover:border-white/30 bg-white/5 hover:bg-white/10'"
                  :title="getCapability(person.id, func.id)
                    ? `${getCapability(person.id, func.id)!.level} - Click to remove`
                    : `Click to assign as ${selectedLevel}`"
                >
                  <svg
                    v-if="getCapability(person.id, func.id)"
                    class="w-4 h-4 text-white"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                </button>

                <!-- Dropdown mode -->
                <select
                  v-else
                  :value="getCapability(person.id, func.id)?.level || ''"
                  @change="handleLevelChange(person.id, func.id, ($event.target as HTMLSelectElement).value as CapabilityLevel | '')"
                  :disabled="isSubmitting"
                  class="w-full text-xs py-1.5 px-1 rounded-lg bg-white/5 border-2 transition-all cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-1 focus:ring-purple-500"
                  :class="getLevelBorderColor(getCapability(person.id, func.id)?.level)"
                >
                  <option value="">—</option>
                  <option v-for="level in capabilityLevels" :key="level.value" :value="level.value">
                    {{ level.shortLabel }}
                  </option>
                </select>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Tips -->
    <div class="orbitos-glass-subtle p-4">
      <div class="flex items-start gap-3">
        <svg class="w-5 h-5 text-purple-400 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <div class="text-sm text-white/60">
          <p class="font-medium text-white/80 mb-1">Quick tips:</p>
          <ul v-if="viewMode === 'checkbox'" class="list-disc list-inside space-y-1">
            <li>Click an empty cell to assign the function with the selected capability level</li>
            <li>Click a filled cell to remove the assignment</li>
            <li>Click person or function names to see their full detail page</li>
          </ul>
          <ul v-else class="list-disc list-inside space-y-1">
            <li>Use the dropdown to select a capability level directly</li>
            <li>Select "—" to remove an assignment</li>
            <li>Border color indicates current capability level</li>
            <li>Click person or function names to see their full detail page</li>
          </ul>
        </div>
      </div>
    </div>
  </div>
</template>
