<script setup lang="ts">
import type { RoleFunction } from '~/composables/useOperations'
import type { RoleWithAssignments, OpsResource, ProcessWithActivities } from '~/types/operations'

definePageMeta({
  layout: 'app'
})

const route = useRoute()
const roleId = computed(() => route.params.id as string)

const {
  roles,
  functions,
  people,
  processes,
  roleAssignments,
  fetchRoles,
  fetchFunctions,
  fetchPeople,
  fetchProcesses,
  fetchRoleAssignments,
  fetchRoleFunctions,
  createRoleAssignment,
  deleteRoleAssignment,
  assignFunctionToRole,
  unassignFunctionFromRole,
  isLoading,
} = useOperations()

// Local state
const isLoadingRole = ref(true)
const showAddPersonDialog = ref(false)
const showAddFunctionDialog = ref(false)
const isSubmitting = ref(false)
const personSearchQuery = ref('')
const functionSearchQuery = ref('')

// Role's assigned functions
const assignedFunctions = ref<RoleFunction[]>([])
const loadingFunctions = ref(false)

// Get current role
const role = computed<RoleWithAssignments | undefined>(() =>
  roles.value.find(r => r.id === roleId.value)
)

// Get people assigned to this role
const assignedPeople = computed(() => {
  return roleAssignments.value
    .filter(ra => ra.roleId === roleId.value)
    .map(ra => {
      const person = people.value.find(p => p.id === ra.resourceId)
      return {
        ...ra,
        person,
      }
    })
})

// Get people not yet assigned to this role
const availablePeople = computed(() => {
  const assignedIds = new Set(assignedPeople.value.map(ap => ap.resourceId))
  return people.value.filter(p => !assignedIds.has(p.id))
})

// Get functions not yet assigned to this role
const availableFunctions = computed(() => {
  const assignedIds = new Set(assignedFunctions.value.map(rf => rf.functionId))
  return functions.value.filter(f => !assignedIds.has(f.id))
})

// Filter available people by search
const filteredPeople = computed(() => {
  if (!personSearchQuery.value) return availablePeople.value
  const query = personSearchQuery.value.toLowerCase()
  return availablePeople.value.filter(p =>
    p.name.toLowerCase().includes(query) ||
    p.title?.toLowerCase().includes(query) ||
    p.department?.toLowerCase().includes(query)
  )
})

// Filter available functions by search
const filteredFunctions = computed(() => {
  if (!functionSearchQuery.value) return availableFunctions.value
  const query = functionSearchQuery.value.toLowerCase()
  return availableFunctions.value.filter(f =>
    f.name.toLowerCase().includes(query) ||
    f.category?.toLowerCase().includes(query) ||
    f.purpose?.toLowerCase().includes(query)
  )
})

// Get processes that use any of the role's functions
const relatedProcesses = computed(() => {
  const functionIds = new Set(assignedFunctions.value.map(rf => rf.functionId))
  return processes.value.filter(p =>
    p.activities?.some(a => a.functionId && functionIds.has(a.functionId))
  )
})

// Coverage status based on assigned people
const coverageStatus = computed(() => {
  const count = assignedPeople.value.length
  if (count === 0) return { label: 'Uncovered', color: 'bg-red-500/20 text-red-300', description: 'No one assigned' }
  if (count === 1) return { label: 'At Risk', color: 'bg-amber-500/20 text-amber-300', description: 'Single person coverage' }
  if (count === 2) return { label: 'Covered', color: 'bg-emerald-500/20 text-emerald-300', description: 'Adequate coverage' }
  return { label: 'Well Covered', color: 'bg-cyan-500/20 text-cyan-300', description: 'Strong team' }
})

// Load assigned functions for this role
const loadAssignedFunctions = async () => {
  loadingFunctions.value = true
  try {
    assignedFunctions.value = await fetchRoleFunctions(roleId.value)
  } catch (e) {
    console.error('Failed to load assigned functions:', e)
    assignedFunctions.value = []
  } finally {
    loadingFunctions.value = false
  }
}

// Load all data
onMounted(async () => {
  isLoadingRole.value = true
  try {
    await Promise.all([
      fetchRoles(),
      fetchFunctions(),
      fetchPeople(),
      fetchProcesses(),
      fetchRoleAssignments(),
    ])
    await loadAssignedFunctions()
  } catch (e) {
    console.error('Failed to load role data:', e)
  } finally {
    isLoadingRole.value = false
  }
})

// Add person to role
const handleAddPerson = async (resourceId: string) => {
  isSubmitting.value = true
  try {
    await createRoleAssignment({
      resourceId,
      roleId: roleId.value,
      isPrimary: assignedPeople.value.length === 0, // First person is primary
    })
    personSearchQuery.value = ''
    await fetchRoleAssignments()
  } catch (e) {
    console.error('Failed to add person:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Remove person from role
const handleRemovePerson = async (assignmentId: string) => {
  try {
    await deleteRoleAssignment(assignmentId)
    await fetchRoleAssignments()
  } catch (e) {
    console.error('Failed to remove person:', e)
  }
}

// Add function to role
const handleAddFunction = async (functionId: string) => {
  isSubmitting.value = true
  try {
    const newAssignment = await assignFunctionToRole(roleId.value, functionId)
    assignedFunctions.value.push(newAssignment)
    functionSearchQuery.value = ''
  } catch (e) {
    console.error('Failed to add function:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Remove function from role
const handleRemoveFunction = async (functionId: string) => {
  try {
    await unassignFunctionFromRole(roleId.value, functionId)
    assignedFunctions.value = assignedFunctions.value.filter(rf => rf.functionId !== functionId)
  } catch (e) {
    console.error('Failed to remove function:', e)
  }
}

// Get initials for avatar
const getInitials = (name: string) => {
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}

// Get category color
const getCategoryColor = (category: string | undefined) => {
  if (!category) return 'bg-slate-500/20 text-slate-300'
  const colors: Record<string, string> = {
    sales: 'bg-blue-500/20 text-blue-300',
    marketing: 'bg-pink-500/20 text-pink-300',
    engineering: 'bg-purple-500/20 text-purple-300',
    operations: 'bg-emerald-500/20 text-emerald-300',
    finance: 'bg-amber-500/20 text-amber-300',
    hr: 'bg-cyan-500/20 text-cyan-300',
    support: 'bg-orange-500/20 text-orange-300',
  }
  return colors[category.toLowerCase()] || 'bg-slate-500/20 text-slate-300'
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
      <div class="flex items-start gap-4">
        <NuxtLink
          to="/app/roles"
          class="mt-1 flex items-center justify-center h-10 w-10 rounded-lg border border-slate-700 bg-slate-800/60 text-slate-400 hover:text-white hover:border-slate-600 transition-colors"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </NuxtLink>
        <div v-if="role">
          <div class="flex items-center gap-3 flex-wrap">
            <h1 class="text-2xl font-bold text-white">{{ role.name }}</h1>
            <span :class="['rounded-full px-3 py-1 text-xs font-medium', coverageStatus.color]">
              {{ coverageStatus.label }}
            </span>
            <span v-if="role.department" class="rounded-lg bg-white/10 px-3 py-1 text-xs text-white/60">
              {{ role.department }}
            </span>
          </div>
          <p class="mt-1 text-slate-400 max-w-2xl">{{ role.purpose || 'No purpose defined' }}</p>
        </div>
        <div v-else-if="isLoadingRole">
          <div class="h-8 w-48 bg-slate-700/50 rounded animate-pulse"></div>
          <div class="h-4 w-64 bg-slate-700/50 rounded animate-pulse mt-2"></div>
        </div>
      </div>
    </div>

    <!-- Loading state -->
    <div v-if="isLoadingRole || isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <template v-else-if="role">
      <!-- Hero Stats Cards -->
      <div class="grid gap-4 md:grid-cols-4">
        <div class="relative overflow-hidden rounded-2xl border border-slate-700 bg-gradient-to-br from-slate-800/80 to-slate-900/80 p-5">
          <div class="absolute -right-4 -top-4 h-24 w-24 rounded-full bg-blue-500/10 blur-2xl"></div>
          <div class="relative">
            <div class="flex items-center gap-2">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-500/20">
                <svg class="h-5 w-5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </div>
            </div>
            <div class="mt-3 text-3xl font-bold text-white">{{ assignedPeople.length }}</div>
            <div class="text-sm text-slate-400">People Assigned</div>
          </div>
        </div>

        <div class="relative overflow-hidden rounded-2xl border border-slate-700 bg-gradient-to-br from-slate-800/80 to-slate-900/80 p-5">
          <div class="absolute -right-4 -top-4 h-24 w-24 rounded-full bg-purple-500/10 blur-2xl"></div>
          <div class="relative">
            <div class="flex items-center gap-2">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-purple-500/20">
                <svg class="h-5 w-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
                </svg>
              </div>
            </div>
            <div class="mt-3 text-3xl font-bold text-white">{{ assignedFunctions.length }}</div>
            <div class="text-sm text-slate-400">Functions</div>
          </div>
        </div>

        <div class="relative overflow-hidden rounded-2xl border border-slate-700 bg-gradient-to-br from-slate-800/80 to-slate-900/80 p-5">
          <div class="absolute -right-4 -top-4 h-24 w-24 rounded-full bg-emerald-500/10 blur-2xl"></div>
          <div class="relative">
            <div class="flex items-center gap-2">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-500/20">
                <svg class="h-5 w-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                </svg>
              </div>
            </div>
            <div class="mt-3 text-3xl font-bold text-white">{{ relatedProcesses.length }}</div>
            <div class="text-sm text-slate-400">Processes</div>
          </div>
        </div>

        <div class="relative overflow-hidden rounded-2xl border border-slate-700 bg-gradient-to-br from-slate-800/80 to-slate-900/80 p-5">
          <div class="absolute -right-4 -top-4 h-24 w-24 rounded-full bg-amber-500/10 blur-2xl"></div>
          <div class="relative">
            <div class="flex items-center gap-2">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl" :class="coverageStatus.color.replace('text-', 'bg-').replace('300', '500/20')">
                <svg class="h-5 w-5" :class="coverageStatus.color.replace('bg-', 'text-').replace('/20', '')" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                </svg>
              </div>
            </div>
            <div class="mt-3 text-lg font-bold text-white">{{ coverageStatus.label }}</div>
            <div class="text-sm text-slate-400">{{ coverageStatus.description }}</div>
          </div>
        </div>
      </div>

      <div class="grid gap-6 lg:grid-cols-3">
        <!-- Main Content - 2 columns -->
        <div class="lg:col-span-2 space-y-6">
          <!-- Assigned People Section -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 overflow-hidden">
            <div class="flex items-center justify-between p-6 border-b border-slate-700">
              <div>
                <h2 class="text-lg font-semibold text-white">Team Members</h2>
                <p class="text-sm text-slate-400">People currently assigned to this role</p>
              </div>
              <button
                @click="showAddPersonDialog = true"
                class="orbitos-btn-primary py-2 px-4 text-sm"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                Add Person
              </button>
            </div>

            <!-- Alert if coverage is low -->
            <div v-if="coverageStatus.label === 'Uncovered' || coverageStatus.label === 'At Risk'" class="mx-6 mt-4 rounded-lg p-3" :class="coverageStatus.label === 'Uncovered' ? 'bg-red-500/10 border border-red-500/20' : 'bg-amber-500/10 border border-amber-500/20'">
              <div class="flex items-center gap-2 text-sm" :class="coverageStatus.label === 'Uncovered' ? 'text-red-300' : 'text-amber-300'">
                <svg class="h-4 w-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
                <span>{{ coverageStatus.label === 'Uncovered' ? 'This role has no one assigned. Add team members to ensure coverage.' : 'Single person coverage. Consider adding a backup for continuity.' }}</span>
              </div>
            </div>

            <div class="p-6">
              <div v-if="assignedPeople.length === 0" class="text-center py-12">
                <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-slate-500/20 flex items-center justify-center">
                  <svg class="w-8 h-8 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                </div>
                <h3 class="text-white font-medium mb-1">No team members yet</h3>
                <p class="text-slate-400 text-sm mb-4">Add people who will perform this role</p>
                <button
                  @click="showAddPersonDialog = true"
                  class="text-purple-400 hover:text-purple-300 text-sm font-medium"
                >
                  Add first person →
                </button>
              </div>

              <div v-else class="grid gap-3 sm:grid-cols-2">
                <div
                  v-for="assignment in assignedPeople"
                  :key="assignment.id"
                  class="group relative rounded-xl border border-slate-600/50 bg-slate-900/50 p-4 hover:border-slate-500 transition-colors"
                >
                  <div class="flex items-start gap-3">
                    <NuxtLink
                      :to="`/app/people/${assignment.resourceId}`"
                      class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white hover:ring-2 hover:ring-purple-400 transition-all"
                    >
                      {{ getInitials(assignment.resourceName) }}
                    </NuxtLink>
                    <div class="flex-1 min-w-0">
                      <NuxtLink :to="`/app/people/${assignment.resourceId}`" class="text-white font-medium hover:text-purple-300 transition-colors">
                        {{ assignment.resourceName }}
                      </NuxtLink>
                      <div class="flex items-center gap-2 mt-1">
                        <span v-if="assignment.isPrimary" class="rounded-full bg-amber-500/20 border border-amber-500/30 px-2 py-0.5 text-xs text-amber-300">
                          Primary
                        </span>
                        <span v-if="assignment.allocationPercentage" class="text-xs text-slate-400">
                          {{ assignment.allocationPercentage }}% allocated
                        </span>
                      </div>
                    </div>
                    <button
                      @click="handleRemovePerson(assignment.id)"
                      class="rounded-lg p-1.5 text-white/20 hover:text-red-400 hover:bg-red-500/10 transition-colors opacity-0 group-hover:opacity-100"
                      title="Remove from role"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Description Card -->
          <div v-if="role.description" class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <h2 class="text-lg font-semibold text-white mb-3">Description</h2>
            <p class="text-slate-300 whitespace-pre-wrap">{{ role.description }}</p>
          </div>

          <!-- Related Processes -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-lg font-semibold text-white">Related Processes</h2>
              <span class="text-xs text-slate-400">{{ relatedProcesses.length }} processes</span>
            </div>

            <div v-if="relatedProcesses.length === 0" class="text-center py-8">
              <div class="w-12 h-12 mx-auto mb-3 rounded-xl bg-slate-500/20 flex items-center justify-center">
                <svg class="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                </svg>
              </div>
              <p class="text-slate-400 text-sm">No processes use this role's functions yet</p>
            </div>

            <div v-else class="grid gap-2">
              <NuxtLink
                v-for="process in relatedProcesses"
                :key="process.id"
                :to="`/app/processes/${process.id}`"
                class="flex items-center gap-4 rounded-xl bg-white/5 hover:bg-white/10 px-4 py-3 transition-colors"
              >
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-emerald-500/20 text-emerald-300">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                  </svg>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="text-white font-medium truncate">{{ process.name }}</div>
                  <div class="text-xs text-slate-400">{{ process.activityCount }} activities</div>
                </div>
                <svg class="w-5 h-5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
              </NuxtLink>
            </div>
          </div>
        </div>

        <!-- Sidebar -->
        <div class="space-y-6">
          <!-- Assigned Functions -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-lg font-semibold text-white">Functions</h2>
              <button
                @click="showAddFunctionDialog = true"
                class="rounded-lg p-1.5 text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
                title="Add function"
              >
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
              </button>
            </div>

            <div v-if="loadingFunctions" class="flex items-center justify-center py-8">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
            </div>

            <div v-else-if="assignedFunctions.length === 0" class="text-center py-8">
              <div class="w-12 h-12 mx-auto mb-3 rounded-xl bg-slate-500/20 flex items-center justify-center">
                <svg class="w-6 h-6 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
                </svg>
              </div>
              <p class="text-slate-400 text-sm mb-2">No functions assigned</p>
              <button
                @click="showAddFunctionDialog = true"
                class="text-purple-400 hover:text-purple-300 text-sm"
              >
                Add functions →
              </button>
            </div>

            <div v-else class="space-y-2">
              <div
                v-for="rf in assignedFunctions"
                :key="rf.id"
                class="group flex items-center gap-3 rounded-lg bg-white/5 hover:bg-white/10 px-3 py-2 transition-colors"
              >
                <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-purple-500/20 text-purple-300">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
                  </svg>
                </div>
                <div class="flex-1 min-w-0">
                  <NuxtLink :to="`/app/functions/${rf.functionId}`" class="text-sm text-white hover:text-purple-300 truncate block">
                    {{ rf.functionName }}
                  </NuxtLink>
                  <div v-if="rf.functionCategory" class="text-xs text-slate-400 truncate">{{ rf.functionCategory }}</div>
                </div>
                <button
                  @click="handleRemoveFunction(rf.functionId)"
                  class="rounded p-1 text-white/20 hover:text-red-400 hover:bg-red-500/10 transition-colors opacity-0 group-hover:opacity-100"
                  title="Remove function"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <!-- AI Insights -->
          <div class="rounded-2xl border border-purple-500/30 bg-gradient-to-br from-purple-500/10 to-blue-500/10 p-6">
            <div class="flex items-center gap-2 mb-3">
              <svg class="h-5 w-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
              </svg>
              <span class="font-medium text-purple-300">AI Insights</span>
            </div>
            <p class="text-sm text-slate-300">
              {{ coverageStatus.label === 'Uncovered'
                ? 'This role has no one assigned. Consider identifying team members who can take on this responsibility.'
                : coverageStatus.label === 'At Risk'
                  ? 'Single-person coverage creates risk. Train a backup to ensure business continuity.'
                  : assignedFunctions.length === 0
                    ? 'Define what functions this role is responsible for to clarify expectations.'
                    : 'Good coverage! Consider documenting standard procedures for new team members.'
              }}
            </p>
          </div>

          <!-- Quick Actions -->
          <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
            <h3 class="text-sm font-medium text-white mb-3">Quick Actions</h3>
            <div class="space-y-2">
              <button
                @click="showAddPersonDialog = true"
                class="w-full flex items-center gap-3 rounded-lg bg-white/5 hover:bg-white/10 px-3 py-2 text-left text-sm text-slate-300 hover:text-white transition-colors"
              >
                <svg class="w-4 h-4 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
                </svg>
                Add team member
              </button>
              <button
                @click="showAddFunctionDialog = true"
                class="w-full flex items-center gap-3 rounded-lg bg-white/5 hover:bg-white/10 px-3 py-2 text-left text-sm text-slate-300 hover:text-white transition-colors"
              >
                <svg class="w-4 h-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                Assign function
              </button>
              <NuxtLink
                to="/app/processes"
                class="w-full flex items-center gap-3 rounded-lg bg-white/5 hover:bg-white/10 px-3 py-2 text-left text-sm text-slate-300 hover:text-white transition-colors"
              >
                <svg class="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                </svg>
                View all processes
              </NuxtLink>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Role not found -->
    <div v-else class="orbitos-card-static p-12 text-center">
      <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-slate-500/20 flex items-center justify-center">
        <svg class="w-8 h-8 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </div>
      <h2 class="text-white font-medium mb-1">Role not found</h2>
      <p class="text-slate-400 text-sm mb-4">The role you're looking for doesn't exist or has been removed.</p>
      <NuxtLink to="/app/roles" class="inline-block text-purple-400 hover:text-purple-300">
        ← Back to Roles
      </NuxtLink>
    </div>

    <!-- Add Person Dialog -->
    <BaseDialog
      v-model="showAddPersonDialog"
      size="lg"
      title="Add Team Member"
      subtitle="Assign people to this role"
      :submit-on-enter="false"
    >
      <div class="space-y-4">
        <input
          v-model="personSearchQuery"
          type="text"
          class="orbitos-input"
          placeholder="Search people..."
          autofocus
        />

        <div class="space-y-2 max-h-80 overflow-y-auto">
          <div v-if="filteredPeople.length === 0" class="text-center py-8 text-white/40">
            {{ personSearchQuery ? 'No matching people found' : 'All people already assigned' }}
          </div>
          <button
            v-for="person in filteredPeople"
            :key="person.id"
            @click="handleAddPerson(person.id)"
            :disabled="isSubmitting"
            class="w-full text-left rounded-lg bg-white/5 hover:bg-white/10 px-4 py-3 transition-colors disabled:opacity-50"
          >
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white">
                {{ getInitials(person.name) }}
              </div>
              <div class="flex-1 min-w-0">
                <div class="text-white font-medium">{{ person.name }}</div>
                <div class="text-xs text-white/40">{{ person.title || person.department || 'No title' }}</div>
              </div>
              <svg class="w-5 h-5 text-white/20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
            </div>
          </button>
        </div>
      </div>

      <template #footer="{ close }">
        <button type="button" @click="close" class="w-full orbitos-btn-secondary">
          Done
        </button>
      </template>
    </BaseDialog>

    <!-- Add Function Dialog -->
    <BaseDialog
      v-model="showAddFunctionDialog"
      size="lg"
      title="Assign Function"
      subtitle="Add functions this role is responsible for"
      :submit-on-enter="false"
    >
      <div class="space-y-4">
        <input
          v-model="functionSearchQuery"
          type="text"
          class="orbitos-input"
          placeholder="Search functions..."
          autofocus
        />

        <div class="space-y-2 max-h-80 overflow-y-auto">
          <div v-if="filteredFunctions.length === 0" class="text-center py-8 text-white/40">
            {{ functionSearchQuery ? 'No matching functions found' : 'All functions already assigned' }}
          </div>
          <button
            v-for="func in filteredFunctions"
            :key="func.id"
            @click="handleAddFunction(func.id)"
            :disabled="isSubmitting"
            class="w-full text-left rounded-lg bg-white/5 hover:bg-white/10 px-4 py-3 transition-colors disabled:opacity-50"
          >
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-purple-500/20 text-purple-300">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
                </svg>
              </div>
              <div class="flex-1 min-w-0">
                <div class="text-white font-medium">{{ func.name }}</div>
                <div class="text-xs text-white/40">{{ func.category || 'Uncategorized' }}</div>
              </div>
              <svg class="w-5 h-5 text-white/20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
            </div>
          </button>
        </div>
      </div>

      <template #footer="{ close }">
        <button type="button" @click="close" class="w-full orbitos-btn-secondary">
          Done
        </button>
      </template>
    </BaseDialog>
  </div>
</template>
