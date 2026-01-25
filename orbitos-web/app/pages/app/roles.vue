<script setup lang="ts">
import type { AssignableItem } from '~/components/SearchableAssigner.vue'
import type { RoleFunction } from '~/composables/useOperations'

definePageMeta({
  layout: 'app'
})

const {
  roles,
  functions,
  isLoading,
  fetchRoles,
  fetchFunctions,
  createRole,
  updateRole,
  deleteRole,
  fetchRoleFunctions,
  assignFunctionToRole,
  unassignFunctionFromRole,
} = useOperations()

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const isSubmitting = ref(false)

// Search and filter
const searchQuery = ref('')
const departmentFilter = ref('')

const newRole = ref({
  name: '',
  purpose: '',
  department: '',
  description: ''
})

const editingRole = ref<{
  id: string
  name: string
  purpose: string
  department: string
  description: string
} | null>(null)

// Function assignment state (for edit mode)
const assignedFunctions = ref<RoleFunction[]>([])
const loadingFunctions = ref(false)

// Fetch roles on mount
onMounted(async () => {
  await fetchRoles()
  await fetchFunctions()
})

// Get unique departments for filter
const departments = computed(() => {
  const depts = new Set(roles.value.map(r => r.department).filter(Boolean))
  return Array.from(depts).sort()
})

// Filtered roles
const filteredRoles = computed(() => {
  let result = roles.value

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(r =>
      r.name.toLowerCase().includes(query) ||
      r.purpose?.toLowerCase().includes(query) ||
      r.department?.toLowerCase().includes(query)
    )
  }

  if (departmentFilter.value) {
    result = result.filter(r => r.department === departmentFilter.value)
  }

  return result
})

// Stats
const stats = computed(() => ({
  total: roles.value.length,
  covered: roles.value.filter(r => r.coverageStatus === 'covered').length,
  atRisk: roles.value.filter(r => r.coverageStatus === 'at_risk').length,
  uncovered: roles.value.filter(r => r.coverageStatus === 'uncovered' || !r.coverageStatus).length
}))

// Add role
const handleAddRole = async () => {
  if (!newRole.value.name) return

  isSubmitting.value = true
  try {
    await createRole({
      name: newRole.value.name,
      purpose: newRole.value.purpose,
      department: newRole.value.department,
      description: newRole.value.description
    })

    // Reset form
    newRole.value = { name: '', purpose: '', department: '', description: '' }
    showAddDialog.value = false
  } catch (e) {
    console.error('Failed to add role:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Open edit dialog
const openEditDialog = async (role: { id: string; name: string; purpose?: string; department?: string; description?: string }) => {
  editingRole.value = {
    id: role.id,
    name: role.name,
    purpose: role.purpose || '',
    department: role.department || '',
    description: role.description || ''
  }
  assignedFunctions.value = []
  showEditDialog.value = true

  // Load assigned functions for this role
  await loadAssignedFunctions(role.id)
}

// Edit role
const handleEditRole = async () => {
  if (!editingRole.value || !editingRole.value.name) return

  isSubmitting.value = true
  try {
    await updateRole(editingRole.value.id, {
      name: editingRole.value.name,
      purpose: editingRole.value.purpose,
      department: editingRole.value.department,
      description: editingRole.value.description
    })

    editingRole.value = null
    showEditDialog.value = false
  } catch (e) {
    console.error('Failed to update role:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Delete role
const handleDeleteRole = async (id: string) => {
  if (confirm('Are you sure you want to delete this role?')) {
    try {
      await deleteRole(id)
    } catch (e) {
      console.error('Failed to delete role:', e)
    }
  }
}

// Delete from edit dialog
const handleDeleteFromEdit = async () => {
  if (!editingRole.value) return

  if (confirm('Are you sure you want to delete this role?')) {
    isSubmitting.value = true
    try {
      await deleteRole(editingRole.value.id)
      editingRole.value = null
      showEditDialog.value = false
    } catch (e) {
      console.error('Failed to delete role:', e)
    } finally {
      isSubmitting.value = false
    }
  }
}

// Handle Enter key in dialog forms (also handles Escape)
const handleAddFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && newRole.value.name && !isSubmitting.value) {
    e.preventDefault()
    handleAddRole()
  } else if (e.key === 'Escape') {
    showAddDialog.value = false
  }
}

const handleEditFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && editingRole.value?.name && !isSubmitting.value) {
    e.preventDefault()
    handleEditRole()
  } else if (e.key === 'Escape') {
    showEditDialog.value = false
  }
}

// Handle Escape key on dialog backdrop
const handleDialogKeydown = (e: KeyboardEvent, closeDialog: () => void) => {
  if (e.key === 'Escape') {
    closeDialog()
  }
}

// Computed properties for SearchableAssigner
const assignedFunctionsForAssigner = computed<AssignableItem[]>(() =>
  assignedFunctions.value.map(rf => ({
    id: rf.functionId,
    name: rf.functionName,
    subtitle: rf.functionCategory || undefined,
  }))
)

const availableFunctionsForAssigner = computed<AssignableItem[]>(() => {
  const assignedIds = new Set(assignedFunctions.value.map(rf => rf.functionId))
  return functions.value
    .filter(f => !assignedIds.has(f.id))
    .map(f => ({
      id: f.id,
      name: f.name,
      subtitle: f.category || undefined,
    }))
})

// Load assigned functions for a role
const loadAssignedFunctions = async (roleId: string) => {
  loadingFunctions.value = true
  try {
    assignedFunctions.value = await fetchRoleFunctions(roleId)
  } catch (e) {
    console.error('Failed to load assigned functions:', e)
    assignedFunctions.value = []
  } finally {
    loadingFunctions.value = false
  }
}

// Handle adding a function to the role
const handleAddFunction = async (functionId: string) => {
  if (!editingRole.value) return
  try {
    const newAssignment = await assignFunctionToRole(editingRole.value.id, functionId)
    assignedFunctions.value.push(newAssignment)
    // Update the function count in the local role data
    const roleIndex = roles.value.findIndex(r => r.id === editingRole.value?.id)
    if (roleIndex !== -1) {
      roles.value[roleIndex].functionCount = (roles.value[roleIndex].functionCount || 0) + 1
    }
  } catch (e) {
    console.error('Failed to assign function:', e)
  }
}

// Handle removing a function from the role
const handleRemoveFunction = async (functionId: string) => {
  if (!editingRole.value) return
  try {
    await unassignFunctionFromRole(editingRole.value.id, functionId)
    assignedFunctions.value = assignedFunctions.value.filter(rf => rf.functionId !== functionId)
    // Update the function count in the local role data
    const roleIndex = roles.value.findIndex(r => r.id === editingRole.value?.id)
    if (roleIndex !== -1 && (roles.value[roleIndex].functionCount || 0) > 0) {
      roles.value[roleIndex].functionCount = (roles.value[roleIndex].functionCount || 1) - 1
    }
  } catch (e) {
    console.error('Failed to unassign function:', e)
  }
}

// Coverage status colors
const statusColor = (status: string) => {
  switch (status) {
    case 'covered': return 'bg-emerald-500/20 text-emerald-300'
    case 'at_risk': return 'bg-amber-500/20 text-amber-300'
    case 'uncovered': return 'bg-red-500/20 text-red-300'
    default: return 'bg-slate-500/20 text-slate-300'
  }
}

const statusLabel = (status: string) => {
  switch (status) {
    case 'covered': return 'Covered'
    case 'at_risk': return 'At Risk'
    case 'uncovered': return 'Uncovered'
    default: return status
  }
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Roles</h1>
        <p class="orbitos-text">Operational roles and their coverage status.</p>
      </div>
      <div class="flex items-center gap-3">
        <KnowledgeBaseGuideButton
          article-id="roles/role-design"
          label="Role Design Guide"
        />
        <button
          type="button"
          @click="showAddDialog = true"
          class="orbitos-btn-primary py-2 px-4 text-sm"
        >
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Add Role
        </button>
      </div>
    </div>

    <!-- Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total Roles</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ stats.total }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Covered</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-300">{{ stats.covered }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">At Risk</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">{{ stats.atRisk }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Uncovered</div>
        <div class="mt-1 text-2xl font-semibold text-red-300">{{ stats.uncovered }}</div>
      </div>
    </div>

    <!-- Search and Filters -->
    <div class="flex flex-col gap-3 md:flex-row md:items-center">
      <div class="relative flex-1 max-w-md">
        <svg class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          v-model="searchQuery"
          type="text"
          class="orbitos-input pl-10"
          placeholder="Search roles..."
        />
      </div>
      <select
        v-model="departmentFilter"
        class="orbitos-input w-auto min-w-[150px]"
      >
        <option value="">All Departments</option>
        <option v-for="dept in departments" :key="dept" :value="dept">{{ dept }}</option>
      </select>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="roles.length === 0" class="orbitos-card-static p-12 text-center">
      <div class="orbitos-text">No roles defined yet.</div>
      <button
        type="button"
        @click="showAddDialog = true"
        class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
      >
        Add your first role
      </button>
    </div>

    <!-- Roles Table -->
    <div v-else class="orbitos-glass-subtle overflow-hidden">
      <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <div class="text-sm text-white/70">{{ filteredRoles.length }} roles</div>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-black/20 text-xs uppercase text-white/40">
            <tr>
              <th class="px-6 py-3">Role</th>
              <th class="px-6 py-3">Department</th>
              <th class="px-6 py-3">Assigned People</th>
              <th class="px-6 py-3">Functions</th>
              <th class="px-6 py-3">Status</th>
              <th class="px-6 py-3 w-24">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr
              v-for="role in filteredRoles"
              :key="role.id"
              class="hover:bg-white/5 transition-colors cursor-pointer"
              @click="navigateTo(`/app/roles/${role.id}`)"
            >
              <td class="px-6 py-4">
                <NuxtLink :to="`/app/roles/${role.id}`" class="block hover:text-purple-300 transition-colors" @click.stop>
                  <div class="font-semibold text-white">{{ role.name }}</div>
                  <div v-if="role.purpose" class="text-xs text-white/40 mt-0.5 line-clamp-1">{{ role.purpose }}</div>
                </NuxtLink>
              </td>
              <td class="px-6 py-4">
                <span v-if="role.department" class="rounded bg-white/10 px-2 py-1 text-xs text-white/70">
                  {{ role.department }}
                </span>
                <span v-else class="text-white/30 text-xs">—</span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                  <span class="text-white">{{ role.assignmentCount || 0 }}</span>
                  <span class="text-white/40 text-xs">people</span>
                </div>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                  <span class="text-white">{{ role.functionCount || 0 }}</span>
                  <span class="text-white/40 text-xs">functions</span>
                </div>
              </td>
              <td class="px-6 py-4">
                <span :class="['rounded-full px-2.5 py-1 text-xs font-medium', statusColor(role.coverageStatus || 'uncovered')]">
                  {{ statusLabel(role.coverageStatus || 'uncovered') }}
                </span>
              </td>
              <td class="px-6 py-4" @click.stop>
                <div class="flex gap-1">
                  <button
                    type="button"
                    @click="openEditDialog(role)"
                    class="rounded-lg p-2 text-white/40 hover:text-white hover:bg-white/10 transition-colors"
                    title="Edit role"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    @click="handleDeleteRole(role.id)"
                    class="rounded-lg p-2 text-white/40 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                    title="Delete role"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Add Role Dialog -->
    <div
      v-if="showAddDialog"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      @click.self="showAddDialog = false"
      @keydown="handleDialogKeydown($event, () => showAddDialog = false)"
    >
      <div class="w-full max-w-lg orbitos-glass p-6" @keydown="handleAddFormKeydown">
        <h2 class="orbitos-heading-sm">Add New Role</h2>
        <p class="text-sm orbitos-text mt-1">Define an operational role for your organization.</p>

        <div class="mt-6 space-y-4">
          <div>
            <label class="orbitos-label">Role Name *</label>
            <input
              v-model="newRole.name"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Sales Lead"
              autofocus
            />
          </div>

          <div>
            <label class="orbitos-label">Purpose</label>
            <input
              v-model="newRole.purpose"
              type="text"
              class="orbitos-input"
              placeholder="What is this role responsible for?"
            />
          </div>

          <div>
            <label class="orbitos-label">Department</label>
            <input
              v-model="newRole.department"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Sales, Finance, Operations"
            />
          </div>

          <div>
            <label class="orbitos-label">Description</label>
            <textarea
              v-model="newRole.description"
              rows="3"
              class="orbitos-input"
              placeholder="Additional details about this role..."
            ></textarea>
          </div>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            @click="showAddDialog = false; newRole = { name: '', purpose: '', department: '', description: '' }"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleAddRole"
            :disabled="!newRole.name || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Adding...
            </span>
            <span v-else>Add Role</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Edit Role Dialog -->
    <div
      v-if="showEditDialog && editingRole"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      @click.self="showEditDialog = false"
      @keydown="handleDialogKeydown($event, () => showEditDialog = false)"
    >
      <div class="w-full max-w-lg orbitos-glass p-6" @keydown="handleEditFormKeydown">
        <h2 class="orbitos-heading-sm">Edit Role</h2>
        <p class="text-sm orbitos-text mt-1">Update role information.</p>

        <div class="mt-6 space-y-4">
          <div>
            <label class="orbitos-label">Role Name *</label>
            <input
              v-model="editingRole.name"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Sales Lead"
              autofocus
            />
          </div>

          <div>
            <label class="orbitos-label">Purpose</label>
            <input
              v-model="editingRole.purpose"
              type="text"
              class="orbitos-input"
              placeholder="What is this role responsible for?"
            />
          </div>

          <div>
            <label class="orbitos-label">Department</label>
            <input
              v-model="editingRole.department"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Sales, Finance, Operations"
            />
          </div>

          <div>
            <label class="orbitos-label">Description</label>
            <textarea
              v-model="editingRole.description"
              rows="3"
              class="orbitos-input"
              placeholder="Additional details about this role..."
            ></textarea>
          </div>

          <!-- Function Assignment -->
          <div class="pt-2 border-t border-white/10">
            <SearchableAssigner
              :assigned="assignedFunctionsForAssigner"
              :available="availableFunctionsForAssigner"
              label="Assigned Functions"
              search-placeholder="Search functions to add..."
              :loading="loadingFunctions"
              empty-assigned-text="No functions assigned to this role"
              empty-available-text="All functions are already assigned"
              no-results-text="No matching functions found"
              @add="handleAddFunction"
              @remove="handleRemoveFunction"
            />
          </div>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            @click="handleDeleteFromEdit"
            :disabled="isSubmitting"
            class="orbitos-btn-secondary text-red-400 hover:text-red-300 hover:border-red-500/50"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
          <button
            type="button"
            @click="showEditDialog = false; editingRole = null"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleEditRole"
            :disabled="!editingRole.name || isSubmitting"
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
