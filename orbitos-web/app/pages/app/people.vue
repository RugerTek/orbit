<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { people, resources, isLoading, fetchPeople, fetchResources, fetchRoleAssignments, roleAssignments } = useOperations()
const { get, post, put, delete: del } = useApi()

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const isSubmitting = ref(false)
const newPerson = ref({
  name: '',
  role: '',
  email: ''
})
const editingPerson = ref<{
  id: string
  name: string
  role: string
  email: string
} | null>(null)

// Fetch people on mount
onMounted(async () => {
  await fetchPeople()
  await fetchRoleAssignments()
})

// Employee subtype ID from seed data
const EMPLOYEE_SUBTYPE_ID = '77777777-7777-7777-7777-777777777701'

// Helper to parse metadata JSON
const parseMetadata = (metadata?: string): { email?: string } => {
  if (!metadata) return {}
  try {
    return JSON.parse(metadata)
  } catch {
    return {}
  }
}

// Add person handler
const handleAddPerson = async () => {
  if (!newPerson.value.name) return

  isSubmitting.value = true
  try {
    // Create resource with Employee subtype (Person type)
    // Store role in description, email in metadata as JSON
    await post('/api/organizations/11111111-1111-1111-1111-111111111111/operations/resources', {
      name: newPerson.value.name,
      description: newPerson.value.role || undefined,
      metadata: newPerson.value.email ? JSON.stringify({ email: newPerson.value.email }) : undefined,
      resourceSubtypeId: EMPLOYEE_SUBTYPE_ID,
      status: 1 // Active = 1
    })

    // Refresh the list
    await fetchResources()

    // Reset form and close dialog
    newPerson.value = { name: '', role: '', email: '' }
    showAddDialog.value = false
  } catch (e) {
    console.error('Failed to add person:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Edit person handler - fetch full resource data to get metadata
const openEditDialog = async (person: { id: string; name: string; description?: string }) => {
  try {
    // Fetch full resource data to get metadata (email)
    const fullData = await get<{ id: string; name: string; description?: string; metadata?: string }>(
      `/api/organizations/11111111-1111-1111-1111-111111111111/operations/resources/${person.id}`
    )
    const meta = parseMetadata(fullData.metadata)

    editingPerson.value = {
      id: fullData.id,
      name: fullData.name,
      role: fullData.description || '',
      email: meta.email || ''
    }
    showEditDialog.value = true
  } catch (e) {
    console.error('Failed to load person data:', e)
    // Fallback to basic data if fetch fails
    editingPerson.value = {
      id: person.id,
      name: person.name,
      role: person.description || '',
      email: ''
    }
    showEditDialog.value = true
  }
}

const handleEditPerson = async () => {
  if (!editingPerson.value || !editingPerson.value.name) return

  isSubmitting.value = true
  try {
    await put(`/api/organizations/11111111-1111-1111-1111-111111111111/operations/resources/${editingPerson.value.id}`, {
      name: editingPerson.value.name,
      description: editingPerson.value.role || undefined,
      metadata: editingPerson.value.email ? JSON.stringify({ email: editingPerson.value.email }) : undefined,
      status: 1 // Active = 1
    })

    // Refresh the list
    await fetchResources()

    // Reset and close dialog
    editingPerson.value = null
    showEditDialog.value = false
  } catch (e) {
    console.error('Failed to edit person:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Delete person handler
const handleDeletePerson = async () => {
  if (!editingPerson.value) return

  if (!confirm('Are you sure you want to delete this person?')) return

  isSubmitting.value = true
  try {
    await del(`/api/organizations/11111111-1111-1111-1111-111111111111/operations/resources/${editingPerson.value.id}`)

    // Refresh the list
    await fetchResources()

    // Reset and close dialog
    editingPerson.value = null
    showEditDialog.value = false
  } catch (e) {
    console.error('Failed to delete person:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Handle Enter key on forms
const handleAddFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && newPerson.value.name && !isSubmitting.value) {
    e.preventDefault()
    handleAddPerson()
  }
}

const handleEditFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && editingPerson.value?.name && !isSubmitting.value) {
    e.preventDefault()
    handleEditPerson()
  }
}

// Close dialog on Escape
const handleDialogKeydown = (e: KeyboardEvent, closeDialog: () => void) => {
  if (e.key === 'Escape') {
    closeDialog()
  }
}

// Get role assignments for a person
const getPersonRoles = (personId: string) => {
  return roleAssignments.value.filter(ra => ra.resourceId === personId)
}

// Calculate allocation
const getAllocation = (personId: string) => {
  const assignments = getPersonRoles(personId)
  const total = assignments.reduce((sum, ra) => sum + (ra.allocationPercentage || 0), 0)
  return total
}

// Status based on allocation
const getStatus = (personId: string) => {
  const allocation = getAllocation(personId)
  if (allocation > 100) return { label: 'Overloaded', color: 'bg-red-500/20 text-red-300' }
  if (allocation >= 90) return { label: 'Near Capacity', color: 'bg-amber-500/20 text-amber-300' }
  if (allocation >= 50) return { label: 'Stable', color: 'bg-slate-500/20 text-slate-300' }
  return { label: 'Available', color: 'bg-emerald-500/20 text-emerald-300' }
}

// Stats
const stats = computed(() => {
  const overloaded = people.value.filter(p => getAllocation(p.id) > 100).length
  const nearCapacity = people.value.filter(p => {
    const alloc = getAllocation(p.id)
    return alloc >= 90 && alloc <= 100
  }).length
  const available = people.value.filter(p => getAllocation(p.id) < 50).length
  return { overloaded, nearCapacity, available }
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">People</h1>
        <p class="orbitos-text">Capacity, coverage, and operational focus by person.</p>
      </div>
      <div class="flex items-center gap-3">
        <button type="button" class="orbitos-btn-secondary py-2 px-4 text-sm">Export</button>
        <button
          type="button"
          @click="showAddDialog = true"
          class="orbitos-btn-primary py-2 px-4 text-sm"
        >
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Add Person
        </button>
      </div>
    </div>

    <!-- Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total People</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ people.length }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Overloaded</div>
        <div class="mt-1 text-2xl font-semibold text-red-300">{{ stats.overloaded }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Near Capacity</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">{{ stats.nearCapacity }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Available</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-300">{{ stats.available }}</div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="people.length === 0" class="orbitos-card-static p-12 text-center">
      <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-purple-500/20 flex items-center justify-center">
        <svg class="w-8 h-8 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
      </div>
      <div class="orbitos-text">No people registered yet.</div>
      <button
        type="button"
        @click="showAddDialog = true"
        class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
      >
        Add your first person
      </button>
    </div>

    <!-- People Table -->
    <div v-else class="orbitos-glass-subtle overflow-hidden">
      <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <div class="text-sm text-white/70">Team Coverage</div>
        <div class="flex items-center gap-2 rounded-full bg-emerald-500/10 px-3 py-1 text-xs text-emerald-300">
          <span class="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
          {{ Math.round((people.length - stats.overloaded) / people.length * 100) || 0 }}% healthy capacity
        </div>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-black/20 text-xs uppercase text-white/40">
            <tr>
              <th class="px-6 py-3">Person</th>
              <th class="px-6 py-3">Roles</th>
              <th class="px-6 py-3">Allocation</th>
              <th class="px-6 py-3">Status</th>
              <th class="px-6 py-3 w-20">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr v-for="person in people" :key="person.id" class="hover:bg-white/5 transition-colors">
              <td class="px-6 py-4">
                <div class="text-white font-semibold">{{ person.name }}</div>
                <div class="text-xs text-white/40">{{ person.description || 'No description' }}</div>
              </td>
              <td class="px-6 py-4">
                <div class="flex flex-wrap gap-1">
                  <span
                    v-for="role in getPersonRoles(person.id)"
                    :key="role.id"
                    class="rounded-full bg-purple-500/20 border border-purple-500/30 px-2 py-0.5 text-xs text-purple-300"
                  >
                    {{ role.roleName }}
                  </span>
                  <span v-if="getPersonRoles(person.id).length === 0" class="text-white/40 text-xs">
                    No roles assigned
                  </span>
                </div>
              </td>
              <td class="px-6 py-4">
                <div class="w-32 rounded-full bg-white/10 overflow-hidden">
                  <div
                    class="h-2 rounded-full transition-all"
                    :class="getAllocation(person.id) > 100 ? 'bg-red-500' : 'bg-gradient-to-r from-purple-400 to-blue-500'"
                    :style="{ width: `${Math.min(getAllocation(person.id), 100)}%` }"
                  />
                </div>
                <div class="text-xs text-white/40 mt-1">{{ getAllocation(person.id) }}%</div>
              </td>
              <td class="px-6 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatus(person.id).color]">
                  {{ getStatus(person.id).label }}
                </span>
              </td>
              <td class="px-6 py-4">
                <button
                  type="button"
                  @click="openEditDialog(person)"
                  class="rounded-lg p-2 text-white/40 hover:text-white hover:bg-white/10 transition-colors"
                  title="Edit person"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Add Person Dialog -->
    <div
      v-if="showAddDialog"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      @keydown="handleDialogKeydown($event, () => showAddDialog = false)"
    >
      <div class="w-full max-w-lg orbitos-glass p-6" @keydown="handleAddFormKeydown">
        <h2 class="orbitos-heading-sm">Add New Person</h2>
        <p class="text-sm orbitos-text mt-1">Add a team member to track their roles and capacity.</p>

        <div class="mt-6 space-y-4">
          <div>
            <label class="orbitos-label">Full Name *</label>
            <input
              v-model="newPerson.name"
              type="text"
              class="orbitos-input"
              placeholder="e.g., John Smith"
              autofocus
            />
          </div>

          <div>
            <label class="orbitos-label">Email</label>
            <input
              v-model="newPerson.email"
              type="email"
              class="orbitos-input"
              placeholder="john@company.com"
            />
          </div>

          <div>
            <label class="orbitos-label">Role / Title</label>
            <input
              v-model="newPerson.role"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Senior Developer"
            />
          </div>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            @click="showAddDialog = false; newPerson = { name: '', role: '', email: '' }"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleAddPerson"
            :disabled="!newPerson.name || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Adding...
            </span>
            <span v-else>Add Person</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Edit Person Dialog -->
    <div
      v-if="showEditDialog && editingPerson"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      @keydown="handleDialogKeydown($event, () => showEditDialog = false)"
    >
      <div class="w-full max-w-lg orbitos-glass p-6" @keydown="handleEditFormKeydown">
        <h2 class="orbitos-heading-sm">Edit Person</h2>
        <p class="text-sm orbitos-text mt-1">Update team member information.</p>

        <div class="mt-6 space-y-4">
          <div>
            <label class="orbitos-label">Full Name *</label>
            <input
              v-model="editingPerson.name"
              type="text"
              class="orbitos-input"
              placeholder="e.g., John Smith"
              autofocus
            />
          </div>

          <div>
            <label class="orbitos-label">Email</label>
            <input
              v-model="editingPerson.email"
              type="email"
              class="orbitos-input"
              placeholder="john@company.com"
            />
          </div>

          <div>
            <label class="orbitos-label">Role / Title</label>
            <input
              v-model="editingPerson.role"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Senior Developer"
            />
          </div>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            @click="handleDeletePerson"
            :disabled="isSubmitting"
            class="orbitos-btn-secondary text-red-400 hover:text-red-300 hover:border-red-500/50"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
          <button
            type="button"
            @click="showEditDialog = false; editingPerson = null"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleEditPerson"
            :disabled="!editingPerson.name || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Saving...
            </span>
            <span v-else>Save Changes</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
