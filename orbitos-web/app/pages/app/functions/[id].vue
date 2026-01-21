<script setup lang="ts">
import type { CapabilityLevel, FunctionCapability, FunctionWithUsage } from '~/types/operations'

definePageMeta({
  layout: 'app'
})

const route = useRoute()
const functionId = computed(() => route.params.id as string)

const {
  functions,
  people,
  processes,
  functionCapabilities,
  fetchFunctions,
  fetchPeople,
  fetchProcesses,
  fetchFunctionCapabilities,
  fetchFunctionRoles,
  createFunctionCapability,
  deleteFunctionCapability,
  isLoading,
} = useOperations()

import type { RoleFunction } from '~/composables/useOperations'

// Local state
const isLoadingFunction = ref(true)
const showAddPersonDialog = ref(false)
const isSubmitting = ref(false)
const searchQuery = ref('')
const selectedLevel = ref<CapabilityLevel>('capable')

// Roles assigned to this function
const assignedRoles = ref<RoleFunction[]>([])
const loadingRoles = ref(false)

// Capability levels for dropdown
const capabilityLevels: { value: CapabilityLevel; label: string; description: string }[] = [
  { value: 'learning', label: 'Learning', description: 'Currently being trained' },
  { value: 'capable', label: 'Capable', description: 'Can perform independently' },
  { value: 'proficient', label: 'Proficient', description: 'Performs efficiently' },
  { value: 'expert', label: 'Expert', description: 'Deep expertise' },
  { value: 'trainer', label: 'Trainer', description: 'Can train others' },
]

// Get current function
const func = computed<FunctionWithUsage | undefined>(() =>
  functions.value.find(f => f.id === functionId.value)
)

// Get function's capable people
const capablePeople = computed<FunctionCapability[]>(() =>
  functionCapabilities.value.filter(fc => fc.functionId === functionId.value)
)

// Get people not yet assigned to this function
const availablePeople = computed(() => {
  const assignedResourceIds = new Set(capablePeople.value.map(fc => fc.resourceId))
  return people.value.filter(p => !assignedResourceIds.has(p.id))
})

// Get processes that use this function (through their activities)
const relatedProcesses = computed(() => {
  return processes.value.filter(p =>
    p.activities?.some(a => a.functionId === functionId.value)
  )
})

// Filtered available people based on search
const filteredPeople = computed(() => {
  if (!searchQuery.value) return availablePeople.value
  const query = searchQuery.value.toLowerCase()
  return availablePeople.value.filter(p =>
    p.name.toLowerCase().includes(query) ||
    p.description?.toLowerCase().includes(query)
  )
})

// Coverage status
const coverageStatus = computed(() => {
  const count = capablePeople.value.length
  if (count === 0) return { label: 'Uncovered', color: 'bg-slate-500/20 text-slate-300' }
  if (count === 1) return { label: 'SPOF', color: 'bg-red-500/20 text-red-300' }
  if (count === 2) return { label: 'At Risk', color: 'bg-amber-500/20 text-amber-300' }
  return { label: 'Covered', color: 'bg-emerald-500/20 text-emerald-300' }
})

// Load assigned roles for this function
const loadAssignedRoles = async () => {
  loadingRoles.value = true
  try {
    assignedRoles.value = await fetchFunctionRoles(functionId.value)
  } catch (e) {
    console.error('Failed to load assigned roles:', e)
    assignedRoles.value = []
  } finally {
    loadingRoles.value = false
  }
}

// Load data
onMounted(async () => {
  isLoadingFunction.value = true
  try {
    await Promise.all([
      fetchFunctions(),
      fetchPeople(),
      fetchProcesses(),
      fetchFunctionCapabilities(),
    ])
    // Load roles after we have the function ID
    await loadAssignedRoles()
  } catch (e) {
    console.error('Failed to load function data:', e)
  } finally {
    isLoadingFunction.value = false
  }
})

// Add person capability
const handleAddPerson = async (resourceId: string) => {
  isSubmitting.value = true
  try {
    await createFunctionCapability({
      resourceId,
      functionId: functionId.value,
      level: selectedLevel.value,
    })
    searchQuery.value = ''
  } catch (e) {
    console.error('Failed to add person:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Remove person capability
const handleRemoveCapability = async (capabilityId: string) => {
  try {
    await deleteFunctionCapability(capabilityId)
  } catch (e) {
    console.error('Failed to remove capability:', e)
  }
}

// Get level badge color
const getLevelColor = (level: CapabilityLevel) => {
  const colors: Record<CapabilityLevel, string> = {
    learning: 'bg-blue-500/20 text-blue-300 border-blue-500/30',
    capable: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30',
    proficient: 'bg-purple-500/20 text-purple-300 border-purple-500/30',
    expert: 'bg-amber-500/20 text-amber-300 border-amber-500/30',
    trainer: 'bg-rose-500/20 text-rose-300 border-rose-500/30',
  }
  return colors[level]
}

// Get complexity color
const getComplexityColor = (complexity: string | undefined) => {
  switch (complexity) {
    case 'simple': return 'bg-emerald-500/20 text-emerald-300'
    case 'moderate': return 'bg-blue-500/20 text-blue-300'
    case 'complex': return 'bg-purple-500/20 text-purple-300'
    default: return 'bg-slate-500/20 text-slate-300'
  }
}

// Get initials for avatar
const getInitials = (name: string) => {
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
      <div class="flex items-start gap-4">
        <NuxtLink
          to="/app/functions"
          class="mt-1 flex items-center justify-center h-10 w-10 rounded-lg border border-slate-700 bg-slate-800/60 text-slate-400 hover:text-white hover:border-slate-600 transition-colors"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </NuxtLink>
        <div v-if="func">
          <div class="flex items-center gap-3">
            <h1 class="text-2xl font-bold text-white">{{ func.name }}</h1>
            <span :class="['rounded-full px-3 py-1 text-xs font-medium', coverageStatus.color]">
              {{ coverageStatus.label }}
            </span>
          </div>
          <p class="mt-1 text-slate-400">{{ func.purpose || 'No purpose defined' }}</p>
        </div>
        <div v-else>
          <h1 class="text-2xl font-bold text-white">Loading...</h1>
        </div>
      </div>
    </div>

    <!-- Loading state -->
    <div v-if="isLoadingFunction || isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <template v-else-if="func">
      <div class="grid gap-6 lg:grid-cols-3">
        <!-- Main Content -->
        <div class="lg:col-span-2 space-y-6">
          <!-- Overview Card -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <h2 class="text-lg font-semibold text-white mb-4">Overview</h2>
            <p class="text-slate-300 mb-4">{{ func.description || 'No description provided' }}</p>

            <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <div>
                <div class="text-xs uppercase text-slate-500">Category</div>
                <div class="mt-1 text-white">{{ func.category || 'Uncategorized' }}</div>
              </div>
              <div>
                <div class="text-xs uppercase text-slate-500">Complexity</div>
                <span :class="['mt-1 inline-block rounded-full px-2 py-0.5 text-xs', getComplexityColor(func.complexity)]">
                  {{ func.complexity || 'Not set' }}
                </span>
              </div>
              <div>
                <div class="text-xs uppercase text-slate-500">Est. Duration</div>
                <div class="mt-1 text-white">{{ func.estimatedDuration ? `${func.estimatedDuration} min` : 'Not set' }}</div>
              </div>
            </div>

            <div v-if="func.requiresApproval" class="mt-4 flex items-center gap-2 rounded-lg bg-amber-500/10 border border-amber-500/20 px-3 py-2">
              <svg class="h-4 w-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span class="text-sm text-amber-300">This function requires approval</span>
            </div>
          </div>

          <!-- Instructions -->
          <div v-if="func.instructions" class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <h2 class="text-lg font-semibold text-white mb-4">Instructions (SOP)</h2>
            <div class="prose prose-invert prose-sm max-w-none">
              <pre class="whitespace-pre-wrap text-slate-300 text-sm font-sans bg-slate-900/50 rounded-lg p-4">{{ func.instructions }}</pre>
            </div>
          </div>
        </div>

        <!-- Sidebar -->
        <div class="space-y-6">
          <!-- Capable People - Primary focus -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-lg font-semibold text-white">Capable People</h2>
              <button
                @click="showAddPersonDialog = true"
                class="orbitos-btn-primary py-1.5 px-3 text-sm"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                Add
              </button>
            </div>

            <div v-if="coverageStatus.label === 'SPOF'" class="mb-4 rounded-lg bg-red-500/10 border border-red-500/20 p-3">
              <div class="flex items-center gap-2 text-red-300 text-sm">
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
                <span>Single point of failure - add backup</span>
              </div>
            </div>

            <div v-if="capablePeople.length === 0" class="text-center py-8">
              <div class="w-12 h-12 mx-auto mb-3 rounded-xl bg-slate-500/20 flex items-center justify-center">
                <svg class="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </div>
              <p class="text-slate-400 mb-3">No one assigned yet</p>
              <button
                @click="showAddPersonDialog = true"
                class="text-purple-400 hover:text-purple-300 text-sm"
              >
                Add first person
              </button>
            </div>

            <div v-else class="space-y-3">
              <div
                v-for="cap in capablePeople"
                :key="cap.id"
                class="flex items-center gap-3 group"
              >
                <NuxtLink
                  :to="`/app/people/${cap.resourceId}`"
                  class="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white hover:ring-2 hover:ring-purple-400 transition-all"
                >
                  {{ getInitials(cap.resourceName) }}
                </NuxtLink>
                <div class="flex-1">
                  <NuxtLink :to="`/app/people/${cap.resourceId}`" class="text-white hover:text-purple-300">
                    {{ cap.resourceName }}
                  </NuxtLink>
                  <div class="flex items-center gap-2 mt-0.5">
                    <span :class="['rounded-full border px-2 py-0.5 text-xs', getLevelColor(cap.level)]">
                      {{ cap.level }}
                    </span>
                  </div>
                </div>
                <button
                  @click="handleRemoveCapability(cap.id)"
                  class="rounded-lg p-2 text-white/20 hover:text-red-400 hover:bg-red-500/10 transition-colors opacity-0 group-hover:opacity-100"
                  title="Remove capability"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <!-- Assigned Roles -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-lg font-semibold text-white">Assigned Roles</h2>
              <span class="text-xs text-slate-400">{{ assignedRoles.length }} roles</span>
            </div>

            <div v-if="loadingRoles" class="flex items-center justify-center py-6">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
            </div>

            <div v-else-if="assignedRoles.length === 0" class="text-center py-6">
              <div class="w-10 h-10 mx-auto mb-2 rounded-xl bg-slate-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
              <p class="text-sm text-slate-400">No roles assigned</p>
              <NuxtLink to="/app/roles" class="text-xs text-purple-400 hover:text-purple-300 mt-1 inline-block">
                Manage roles →
              </NuxtLink>
            </div>

            <div v-else class="space-y-2">
              <NuxtLink
                v-for="role in assignedRoles"
                :key="role.id"
                :to="`/app/roles`"
                class="flex items-center gap-3 rounded-lg bg-white/5 hover:bg-white/10 px-3 py-2 transition-colors"
              >
                <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-500/20 text-blue-300">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                  </svg>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="text-sm text-white truncate">{{ role.roleName }}</div>
                  <div v-if="role.roleDepartment" class="text-xs text-slate-400 truncate">{{ role.roleDepartment }}</div>
                </div>
                <svg class="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
              </NuxtLink>
            </div>
          </div>

          <!-- Used in Processes -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-lg font-semibold text-white">Used in Processes</h2>
              <span class="text-xs text-slate-400">{{ relatedProcesses.length }} processes</span>
            </div>

            <div v-if="relatedProcesses.length === 0" class="text-center py-6">
              <div class="w-10 h-10 mx-auto mb-2 rounded-xl bg-slate-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                </svg>
              </div>
              <p class="text-sm text-slate-400">Not used in any process yet</p>
              <NuxtLink to="/app/processes" class="text-xs text-purple-400 hover:text-purple-300 mt-1 inline-block">
                View processes →
              </NuxtLink>
            </div>

            <div v-else class="space-y-2">
              <NuxtLink
                v-for="process in relatedProcesses"
                :key="process.id"
                :to="`/app/processes/${process.id}`"
                class="flex items-center gap-3 rounded-lg bg-white/5 hover:bg-white/10 px-3 py-2 transition-colors"
              >
                <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-500/20 text-emerald-300">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                  </svg>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="text-sm text-white truncate">{{ process.name }}</div>
                  <div class="text-xs text-slate-400">
                    {{ process.activities?.filter(a => a.functionId === functionId).length || 0 }} activities
                  </div>
                </div>
                <svg class="w-4 h-4 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
              </NuxtLink>
            </div>
          </div>

          <!-- AI Insights -->
          <div class="rounded-2xl border border-purple-500/30 bg-purple-500/10 p-6">
            <div class="flex items-center gap-2 mb-3">
              <svg class="h-5 w-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
              </svg>
              <span class="font-medium text-purple-300">AI Insights</span>
            </div>
            <p class="text-sm text-slate-300 mb-3">
              {{ coverageStatus.label === 'SPOF'
                ? 'This function is a single point of failure. Training a backup person could reduce risk.'
                : coverageStatus.label === 'At Risk'
                  ? 'Low coverage detected. Consider cross-training team members.'
                  : coverageStatus.label === 'Uncovered'
                    ? 'This function has no one assigned. Assign team members to ensure coverage.'
                    : 'Coverage looks healthy. Consider documenting edge cases for knowledge sharing.'
              }}
            </p>
          </div>
        </div>
      </div>
    </template>

    <!-- Function not found -->
    <div v-else class="orbitos-card-static p-12 text-center">
      <div class="text-white/40">Function not found</div>
      <NuxtLink to="/app/functions" class="mt-4 inline-block text-purple-400 hover:text-purple-300">
        Back to Functions
      </NuxtLink>
    </div>

    <!-- Add Person Dialog -->
    <BaseDialog
      v-model="showAddPersonDialog"
      size="lg"
      title="Add Capable Person"
      subtitle="Select people who can perform this function"
      :submit-on-enter="false"
    >
      <div class="space-y-4">
        <!-- Search -->
        <div>
          <input
            v-model="searchQuery"
            type="text"
            class="orbitos-input"
            placeholder="Search people..."
            autofocus
          />
        </div>

        <!-- Capability Level -->
        <div>
          <label class="orbitos-label">Capability Level</label>
          <select v-model="selectedLevel" class="orbitos-input">
            <option v-for="level in capabilityLevels" :key="level.value" :value="level.value">
              {{ level.label }} - {{ level.description }}
            </option>
          </select>
        </div>

        <!-- People list -->
        <div class="space-y-2 max-h-64 overflow-y-auto">
          <div v-if="filteredPeople.length === 0" class="text-center py-8 text-white/40">
            {{ searchQuery ? 'No matching people found' : 'All people already assigned' }}
          </div>
          <button
            v-for="person in filteredPeople"
            :key="person.id"
            @click="handleAddPerson(person.id)"
            :disabled="isSubmitting"
            class="w-full text-left rounded-lg bg-white/5 hover:bg-white/10 px-4 py-3 transition-colors disabled:opacity-50"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white">
                  {{ getInitials(person.name) }}
                </div>
                <div>
                  <div class="text-white font-medium">{{ person.name }}</div>
                  <div class="text-xs text-white/40">{{ person.description || 'No role' }}</div>
                </div>
              </div>
              <svg class="w-5 h-5 text-white/20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
            </div>
          </button>
        </div>
      </div>

      <template #footer="{ close }">
        <button
          type="button"
          @click="close"
          class="w-full orbitos-btn-secondary"
        >
          Done
        </button>
      </template>
    </BaseDialog>
  </div>
</template>
