<script setup lang="ts">
import type { CapabilityLevel } from '~/types/operations'

definePageMeta({
  layout: 'app'
})

const route = useRoute()
const personId = computed(() => route.params.id as string)

const {
  people,
  functions,
  roleAssignments,
  functionCapabilities,
  fetchPeople,
  fetchFunctions,
  fetchRoleAssignments,
  fetchFunctionCapabilities,
  createFunctionCapability,
  deleteFunctionCapability,
  isLoading,
} = useOperations()

const { get } = useApi()

// Local state
const isLoadingPerson = ref(true)
const showAddFunctionDialog = ref(false)
const isSubmitting = ref(false)
const searchQuery = ref('')
const selectedLevel = ref<CapabilityLevel>('capable')

// Person data with metadata
const personData = ref<{
  id: string
  name: string
  description?: string
  email?: string
} | null>(null)

// Capability levels for dropdown
const capabilityLevels: { value: CapabilityLevel; label: string; description: string }[] = [
  { value: 'learning', label: 'Learning', description: 'Currently being trained' },
  { value: 'capable', label: 'Capable', description: 'Can perform independently' },
  { value: 'proficient', label: 'Proficient', description: 'Performs efficiently' },
  { value: 'expert', label: 'Expert', description: 'Deep expertise' },
  { value: 'trainer', label: 'Trainer', description: 'Can train others' },
]

// Get current person from either personData (API fetch) or people array (shared state)
const person = computed(() => {
  // First check the direct API fetch result
  if (personData.value) {
    const fromPeople = people.value.find(p => p.id === personId.value)
    // Return combined data - prefer fromPeople for status, but use personData for basics
    return fromPeople || {
      id: personData.value.id,
      name: personData.value.name,
      description: personData.value.description,
      status: 'active' // Default status
    }
  }
  // Fallback to people array
  return people.value.find(p => p.id === personId.value)
})

// Get person's role assignments
const personRoles = computed(() =>
  roleAssignments.value.filter(ra => ra.resourceId === personId.value)
)

// Get person's function capabilities
const personCapabilities = computed(() =>
  functionCapabilities.value.filter(fc => fc.resourceId === personId.value)
)

// Get functions not yet assigned to this person
const availableFunctions = computed(() => {
  const assignedFunctionIds = new Set(personCapabilities.value.map(fc => fc.functionId))
  return functions.value.filter(f => !assignedFunctionIds.has(f.id))
})

// Filtered available functions based on search
const filteredFunctions = computed(() => {
  if (!searchQuery.value) return availableFunctions.value
  const query = searchQuery.value.toLowerCase()
  return availableFunctions.value.filter(f =>
    f.name.toLowerCase().includes(query) ||
    f.category?.toLowerCase().includes(query) ||
    f.purpose?.toLowerCase().includes(query)
  )
})

// Load data
onMounted(async () => {
  isLoadingPerson.value = true
  try {
    await Promise.all([
      fetchPeople(),
      fetchFunctions(),
      fetchRoleAssignments(),
      fetchFunctionCapabilities(),
    ])

    // Fetch full person data with metadata
    const fullData = await get<{ id: string; name: string; description?: string; metadata?: string }>(
      `/api/organizations/11111111-1111-1111-1111-111111111111/operations/resources/${personId.value}`
    )
    const meta = fullData.metadata ? JSON.parse(fullData.metadata) : {}
    personData.value = {
      id: fullData.id,
      name: fullData.name,
      description: fullData.description,
      email: meta.email,
    }
  } catch (e) {
    console.error('Failed to load person data:', e)
  } finally {
    isLoadingPerson.value = false
  }
})

// Add function capability
const handleAddFunction = async (functionId: string) => {
  isSubmitting.value = true
  try {
    await createFunctionCapability({
      resourceId: personId.value,
      functionId,
      level: selectedLevel.value,
    })
    searchQuery.value = ''
  } catch (e) {
    console.error('Failed to add function:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Remove function capability
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

// Get initials for avatar
const getInitials = (name: string) => {
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}
</script>

<template>
  <div class="space-y-6">
    <!-- Back button and header -->
    <div class="flex items-center gap-4">
      <NuxtLink to="/app/people" class="rounded-lg p-2 text-white/40 hover:text-white hover:bg-white/10 transition-colors">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </NuxtLink>
      <div class="flex-1">
        <h1 class="orbitos-heading-lg">{{ personData?.name || 'Loading...' }}</h1>
        <p class="orbitos-text">{{ personData?.description || 'Team member' }}</p>
      </div>
    </div>

    <!-- Loading state -->
    <div v-if="isLoadingPerson || isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <template v-else-if="person">
      <!-- Person Info Card -->
      <div class="orbitos-glass p-6">
        <div class="flex items-start gap-6">
          <!-- Avatar -->
          <div class="w-20 h-20 rounded-2xl bg-gradient-to-br from-purple-500 to-blue-600 flex items-center justify-center text-2xl font-bold text-white">
            {{ getInitials(person.name) }}
          </div>

          <div class="flex-1">
            <div class="grid gap-4 md:grid-cols-3">
              <div>
                <div class="text-xs uppercase text-white/40 mb-1">Email</div>
                <div class="text-white">{{ personData?.email || 'Not set' }}</div>
              </div>
              <div>
                <div class="text-xs uppercase text-white/40 mb-1">Status</div>
                <span class="rounded-full px-3 py-1 text-xs font-medium bg-emerald-500/20 text-emerald-300">
                  {{ person.status }}
                </span>
              </div>
              <div>
                <div class="text-xs uppercase text-white/40 mb-1">Total Allocation</div>
                <div class="text-white">
                  {{ personRoles.reduce((sum, r) => sum + (r.allocationPercentage || 0), 0) }}%
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Two column layout -->
      <div class="grid gap-6 lg:grid-cols-2">
        <!-- Assigned Roles -->
        <div class="orbitos-glass-subtle">
          <div class="border-b border-white/10 px-6 py-4 flex items-center justify-between">
            <div>
              <h2 class="text-lg font-semibold text-white">Assigned Roles</h2>
              <p class="text-sm text-white/40">Roles this person fulfills</p>
            </div>
            <span class="rounded-full bg-white/10 px-3 py-1 text-sm text-white/60">
              {{ personRoles.length }}
            </span>
          </div>
          <div class="p-4">
            <div v-if="personRoles.length === 0" class="text-center py-8 text-white/40">
              No roles assigned yet
            </div>
            <div v-else class="space-y-2">
              <div
                v-for="role in personRoles"
                :key="role.id"
                class="flex items-center justify-between rounded-lg bg-white/5 px-4 py-3"
              >
                <div>
                  <div class="text-white font-medium">{{ role.roleName }}</div>
                  <div class="text-xs text-white/40">{{ role.allocationPercentage || 0 }}% allocation</div>
                </div>
                <span
                  v-if="role.isPrimary"
                  class="rounded-full bg-purple-500/20 border border-purple-500/30 px-2 py-0.5 text-xs text-purple-300"
                >
                  Primary
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Function Capabilities - Primary focus -->
        <div class="orbitos-glass-subtle">
          <div class="border-b border-white/10 px-6 py-4 flex items-center justify-between">
            <div>
              <h2 class="text-lg font-semibold text-white">Function Capabilities</h2>
              <p class="text-sm text-white/40">Functions this person can perform</p>
            </div>
            <button
              @click="showAddFunctionDialog = true"
              class="orbitos-btn-primary py-1.5 px-3 text-sm"
            >
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Add Function
            </button>
          </div>
          <div class="p-4">
            <div v-if="personCapabilities.length === 0" class="text-center py-8">
              <div class="w-12 h-12 mx-auto mb-3 rounded-xl bg-purple-500/20 flex items-center justify-center">
                <svg class="w-6 h-6 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                </svg>
              </div>
              <p class="text-white/40 mb-3">No functions assigned yet</p>
              <button
                @click="showAddFunctionDialog = true"
                class="text-purple-400 hover:text-purple-300 text-sm"
              >
                Add first function
              </button>
            </div>
            <div v-else class="space-y-2">
              <div
                v-for="cap in personCapabilities"
                :key="cap.id"
                class="flex items-center justify-between rounded-lg bg-white/5 px-4 py-3 group"
              >
                <div class="flex-1">
                  <div class="text-white font-medium">{{ cap.functionName }}</div>
                  <div class="flex items-center gap-2 mt-1">
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
        </div>
      </div>
    </template>

    <!-- Person not found -->
    <div v-else class="orbitos-card-static p-12 text-center">
      <div class="text-white/40">Person not found</div>
      <NuxtLink to="/app/people" class="mt-4 inline-block text-purple-400 hover:text-purple-300">
        Back to People
      </NuxtLink>
    </div>

    <!-- Add Function Dialog -->
    <BaseDialog
      v-model="showAddFunctionDialog"
      size="lg"
      title="Add Function Capability"
      subtitle="Select functions this person can perform"
      :submit-on-enter="false"
    >
      <div class="space-y-4">
        <!-- Search -->
        <div>
          <input
            v-model="searchQuery"
            type="text"
            class="orbitos-input"
            placeholder="Search functions..."
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

        <!-- Functions list -->
        <div class="space-y-2 max-h-64 overflow-y-auto">
          <div v-if="filteredFunctions.length === 0" class="text-center py-8 text-white/40">
            {{ searchQuery ? 'No matching functions found' : 'All functions already assigned' }}
          </div>
          <button
            v-for="func in filteredFunctions"
            :key="func.id"
            @click="handleAddFunction(func.id)"
            :disabled="isSubmitting"
            class="w-full text-left rounded-lg bg-white/5 hover:bg-white/10 px-4 py-3 transition-colors disabled:opacity-50"
          >
            <div class="flex items-center justify-between">
              <div>
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
