<script setup lang="ts">
import type { AssignableItem } from '~/components/SearchableAssigner.vue'
import type { RoleFunction } from '~/composables/useOperations'

definePageMeta({
  layout: 'app'
})

const {
  functions,
  roles,
  isLoading,
  fetchFunctions,
  fetchRoles,
  createFunction,
  updateFunction,
  deleteFunction,
  fetchFunctionRoles,
  assignRoleToFunction,
  unassignRoleFromFunction,
} = useOperations()

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const isSubmitting = ref(false)

// View mode: 'grid' or 'table'
const viewMode = ref<'grid' | 'table'>('table')

// Filter state
const searchQuery = ref('')
const categoryFilter = ref('')

const newFunction = ref({
  name: '',
  purpose: '',
  category: '',
  description: ''
})

const editingFunction = ref<{
  id: string
  name: string
  purpose: string
  category: string
  description: string
} | null>(null)

// Role assignment state (for edit mode)
const assignedRoles = ref<RoleFunction[]>([])
const loadingRoles = ref(false)

// Fetch functions on mount
onMounted(async () => {
  await fetchFunctions()
  await fetchRoles()
})

// Get unique categories for filter
const categories = computed(() => {
  const cats = new Set(functions.value.map(f => f.category).filter(Boolean))
  return Array.from(cats).sort()
})

// Filtered functions
const filteredFunctions = computed(() => {
  let result = functions.value

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(f =>
      f.name.toLowerCase().includes(query) ||
      f.purpose?.toLowerCase().includes(query) ||
      f.category?.toLowerCase().includes(query)
    )
  }

  if (categoryFilter.value) {
    result = result.filter(f => f.category === categoryFilter.value)
  }

  return result
})

// Add function
const handleAddFunction = async () => {
  if (!newFunction.value.name) return

  isSubmitting.value = true
  try {
    await createFunction({
      name: newFunction.value.name,
      purpose: newFunction.value.purpose,
      category: newFunction.value.category,
      description: newFunction.value.description
    })

    // Reset form
    newFunction.value = { name: '', purpose: '', category: '', description: '' }
    showAddDialog.value = false
  } catch (e) {
    console.error('Failed to add function:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Open edit dialog
const openEditDialog = async (func: { id: string; name: string; purpose?: string; category?: string; description?: string }) => {
  editingFunction.value = {
    id: func.id,
    name: func.name,
    purpose: func.purpose || '',
    category: func.category || '',
    description: func.description || ''
  }
  assignedRoles.value = []
  showEditDialog.value = true

  // Load assigned roles for this function
  await loadAssignedRoles(func.id)
}

// Edit function
const handleEditFunction = async () => {
  if (!editingFunction.value || !editingFunction.value.name) return

  isSubmitting.value = true
  try {
    await updateFunction(editingFunction.value.id, {
      name: editingFunction.value.name,
      purpose: editingFunction.value.purpose,
      category: editingFunction.value.category,
      description: editingFunction.value.description
    })

    editingFunction.value = null
    showEditDialog.value = false
  } catch (e) {
    console.error('Failed to update function:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Delete function
const handleDeleteFunction = async (id: string) => {
  if (confirm('Are you sure you want to delete this function?')) {
    try {
      await deleteFunction(id)
    } catch (e) {
      console.error('Failed to delete function:', e)
    }
  }
}

// Delete from edit dialog
const handleDeleteFromEdit = async () => {
  if (!editingFunction.value) return

  if (confirm('Are you sure you want to delete this function?')) {
    isSubmitting.value = true
    try {
      await deleteFunction(editingFunction.value.id)
      editingFunction.value = null
      showEditDialog.value = false
    } catch (e) {
      console.error('Failed to delete function:', e)
    } finally {
      isSubmitting.value = false
    }
  }
}

// Handle Enter key in dialog forms
const handleAddFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && newFunction.value.name && !isSubmitting.value) {
    e.preventDefault()
    handleAddFunction()
  }
}

const handleEditFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && editingFunction.value?.name && !isSubmitting.value) {
    e.preventDefault()
    handleEditFunction()
  }
}

// Handle Escape key on dialog backdrop
const handleDialogKeydown = (e: KeyboardEvent, closeDialog: () => void) => {
  if (e.key === 'Escape') {
    closeDialog()
  }
}

// Stats
const stats = computed(() => ({
  total: functions.value.length,
  spof: functions.value.filter(f => f.coverageStatus === 'spof').length,
  atRisk: functions.value.filter(f => f.coverageStatus === 'at_risk' || f.coverageStatus === 'uncovered').length,
  covered: functions.value.filter(f => f.coverageStatus === 'covered').length
}))

// Computed properties for SearchableAssigner
const assignedRolesForAssigner = computed<AssignableItem[]>(() =>
  assignedRoles.value.map(rf => ({
    id: rf.roleId,
    name: rf.roleName,
    subtitle: rf.roleDepartment || undefined,
  }))
)

const availableRolesForAssigner = computed<AssignableItem[]>(() => {
  const assignedIds = new Set(assignedRoles.value.map(rf => rf.roleId))
  return roles.value
    .filter(r => !assignedIds.has(r.id))
    .map(r => ({
      id: r.id,
      name: r.name,
      subtitle: r.department || undefined,
    }))
})

// Load assigned roles for a function
const loadAssignedRoles = async (functionId: string) => {
  loadingRoles.value = true
  try {
    assignedRoles.value = await fetchFunctionRoles(functionId)
  } catch (e) {
    console.error('Failed to load assigned roles:', e)
    assignedRoles.value = []
  } finally {
    loadingRoles.value = false
  }
}

// Handle adding a role to the function
const handleAddRole = async (roleId: string) => {
  if (!editingFunction.value) return
  try {
    const newAssignment = await assignRoleToFunction(editingFunction.value.id, roleId)
    assignedRoles.value.push(newAssignment)
    // Update the role count in the local function data
    const funcIndex = functions.value.findIndex(f => f.id === editingFunction.value?.id)
    if (funcIndex !== -1) {
      functions.value[funcIndex].roleCount = (functions.value[funcIndex].roleCount || 0) + 1
    }
  } catch (e) {
    console.error('Failed to assign role:', e)
  }
}

// Handle removing a role from the function
const handleRemoveRole = async (roleId: string) => {
  if (!editingFunction.value) return
  try {
    await unassignRoleFromFunction(editingFunction.value.id, roleId)
    assignedRoles.value = assignedRoles.value.filter(rf => rf.roleId !== roleId)
    // Update the role count in the local function data
    const funcIndex = functions.value.findIndex(f => f.id === editingFunction.value?.id)
    if (funcIndex !== -1 && (functions.value[funcIndex].roleCount || 0) > 0) {
      functions.value[funcIndex].roleCount = (functions.value[funcIndex].roleCount || 1) - 1
    }
  } catch (e) {
    console.error('Failed to unassign role:', e)
  }
}

// Coverage status colors
const statusColor = (status: string) => {
  switch (status) {
    case 'covered': return 'bg-emerald-500/20 text-emerald-300'
    case 'spof': return 'bg-red-500/20 text-red-300'
    case 'at_risk': return 'bg-amber-500/20 text-amber-300'
    case 'uncovered': return 'bg-slate-500/20 text-slate-300'
    default: return 'bg-slate-500/20 text-slate-300'
  }
}

const statusLabel = (status: string) => {
  switch (status) {
    case 'covered': return 'Covered'
    case 'spof': return 'SPOF'
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
        <h1 class="orbitos-heading-lg">Functions</h1>
        <p class="orbitos-text">Atomic work units that can be performed by people with specific capabilities.</p>
      </div>
      <button
        type="button"
        @click="showAddDialog = true"
        class="orbitos-btn-primary py-2 px-4 text-sm"
      >
        <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Add Function
      </button>
    </div>

    <!-- Summary Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total Functions</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ stats.total }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Single Points of Failure</div>
        <div class="mt-1 text-2xl font-semibold text-red-300">{{ stats.spof }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">At Risk / Uncovered</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">{{ stats.atRisk }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Fully Covered</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-300">{{ stats.covered }}</div>
      </div>
    </div>

    <!-- Search and Filters -->
    <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
      <div class="flex flex-1 gap-3">
        <div class="relative flex-1 max-w-md">
          <svg class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            class="orbitos-input pl-10"
            placeholder="Search functions..."
          />
        </div>
        <select
          v-model="categoryFilter"
          class="orbitos-input w-auto min-w-[150px]"
        >
          <option value="">All Categories</option>
          <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
        </select>
      </div>
      <div class="flex gap-1 rounded-lg bg-white/5 p-1">
        <button
          type="button"
          @click="viewMode = 'table'"
          :class="[
            'rounded-md px-3 py-1.5 text-xs transition-colors',
            viewMode === 'table' ? 'bg-white/10 text-white' : 'text-white/50 hover:text-white'
          ]"
        >
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
          </svg>
        </button>
        <button
          type="button"
          @click="viewMode = 'grid'"
          :class="[
            'rounded-md px-3 py-1.5 text-xs transition-colors',
            viewMode === 'grid' ? 'bg-white/10 text-white' : 'text-white/50 hover:text-white'
          ]"
        >
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="functions.length === 0" class="orbitos-card-static p-12 text-center">
      <div class="orbitos-text">No functions defined yet.</div>
      <button
        type="button"
        @click="showAddDialog = true"
        class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
      >
        Add your first function
      </button>
    </div>

    <!-- Table View -->
    <div v-else-if="viewMode === 'table'" class="orbitos-glass-subtle overflow-hidden">
      <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <div class="text-sm text-white/70">{{ filteredFunctions.length }} functions</div>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-black/20 text-xs uppercase text-white/40">
            <tr>
              <th class="px-6 py-3">Function</th>
              <th class="px-6 py-3">Category</th>
              <th class="px-6 py-3">Capable People</th>
              <th class="px-6 py-3">Used In</th>
              <th class="px-6 py-3">Status</th>
              <th class="px-6 py-3 w-24">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr
              v-for="func in filteredFunctions"
              :key="func.id"
              class="hover:bg-white/5 transition-colors"
            >
              <td class="px-6 py-4">
                <NuxtLink :to="`/app/functions/${func.id}`" class="block hover:text-purple-300 transition-colors">
                  <div class="font-semibold text-white">{{ func.name }}</div>
                  <div v-if="func.purpose" class="text-xs text-white/40 mt-0.5 line-clamp-1">{{ func.purpose }}</div>
                </NuxtLink>
              </td>
              <td class="px-6 py-4">
                <span v-if="func.category" class="rounded bg-white/10 px-2 py-1 text-xs text-white/70">
                  {{ func.category }}
                </span>
                <span v-else class="text-white/30 text-xs">—</span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                  <span class="text-white">{{ func.capabilityCount || 0 }}</span>
                  <span class="text-white/40 text-xs">people</span>
                </div>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                  <span class="text-white">{{ func.processCount || 0 }}</span>
                  <span class="text-white/40 text-xs">processes</span>
                </div>
              </td>
              <td class="px-6 py-4">
                <span :class="['rounded-full px-2.5 py-1 text-xs font-medium', statusColor(func.coverageStatus || 'uncovered')]">
                  {{ statusLabel(func.coverageStatus || 'uncovered') }}
                </span>
              </td>
              <td class="px-6 py-4">
                <div class="flex gap-1">
                  <button
                    type="button"
                    @click="openEditDialog(func)"
                    class="rounded-lg p-2 text-white/40 hover:text-white hover:bg-white/10 transition-colors"
                    title="Edit function"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    @click="handleDeleteFunction(func.id)"
                    class="rounded-lg p-2 text-white/40 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                    title="Delete function"
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

    <!-- Grid View -->
    <div v-else class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <div
        v-for="func in filteredFunctions"
        :key="func.id"
        class="orbitos-card p-5"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <div class="flex items-center gap-2">
              <h3 class="font-semibold text-white">{{ func.name }}</h3>
            </div>
            <p v-if="func.purpose" class="mt-1 text-sm text-white/50 line-clamp-2">{{ func.purpose }}</p>
          </div>
          <span :class="['rounded-full px-2.5 py-1 text-xs font-medium flex-shrink-0', statusColor(func.coverageStatus || 'uncovered')]">
            {{ statusLabel(func.coverageStatus || 'uncovered') }}
          </span>
        </div>

        <!-- Meta Info -->
        <div class="mt-4 flex flex-wrap items-center gap-3 text-xs text-white/40">
          <span v-if="func.category" class="rounded bg-white/10 px-2 py-1">{{ func.category }}</span>
          <span>{{ func.capabilityCount || 0 }} capable</span>
          <span>•</span>
          <span>{{ func.processCount || 0 }} processes</span>
        </div>

        <div class="mt-4 flex gap-2 border-t border-white/10 pt-3">
          <NuxtLink
            :to="`/app/functions/${func.id}`"
            class="flex-1 rounded-lg bg-white/5 px-3 py-2 text-center text-xs text-white/70 hover:bg-white/10 transition-colors"
          >
            View Details
          </NuxtLink>
          <button
            type="button"
            @click="openEditDialog(func)"
            class="rounded-lg bg-white/5 px-3 py-2 text-xs text-white/70 hover:bg-white/10 transition-colors"
          >
            Edit
          </button>
          <button
            type="button"
            @click="handleDeleteFunction(func.id)"
            class="rounded-lg bg-red-500/10 px-3 py-2 text-xs text-red-400 hover:bg-red-500/20 transition-colors"
          >
            Delete
          </button>
        </div>
      </div>
    </div>

    <!-- Add Function Dialog -->
    <div
      v-if="showAddDialog"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      @click.self="showAddDialog = false"
      @keydown="handleDialogKeydown($event, () => showAddDialog = false)"
    >
      <div class="w-full max-w-2xl orbitos-glass p-6" @keydown="handleAddFormKeydown">
        <h2 class="orbitos-heading-sm">Add New Function</h2>
        <p class="text-sm orbitos-text mt-1">Define an atomic work unit that can be performed by people.</p>

        <div class="mt-6 space-y-4">
          <div>
            <label class="orbitos-label flex items-center gap-1">
              Function Name *
              <HelpFieldHelpTooltip entity-id="ENT004" field-name="name" />
            </label>
            <input
              v-model="newFunction.name"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Handle Inbound Request"
              autofocus
            />
          </div>

          <div>
            <label class="orbitos-label flex items-center gap-1">
              Purpose
              <HelpFieldHelpTooltip entity-id="ENT004" field-name="purpose" />
            </label>
            <textarea
              v-model="newFunction.purpose"
              rows="4"
              class="orbitos-input"
              placeholder="Why does this function exist?"
            ></textarea>
          </div>

          <div>
            <label class="orbitos-label flex items-center gap-1">
              Category
              <HelpFieldHelpTooltip entity-id="ENT004" field-name="category" />
            </label>
            <input
              v-model="newFunction.category"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Sales, Finance, Operations"
              list="category-suggestions"
            />
            <datalist id="category-suggestions">
              <option v-for="cat in categories" :key="cat" :value="cat" />
            </datalist>
          </div>

          <div>
            <label class="orbitos-label flex items-center gap-1">
              Description
              <HelpFieldHelpTooltip entity-id="ENT004" field-name="description" />
            </label>
            <textarea
              v-model="newFunction.description"
              rows="3"
              class="orbitos-input"
              placeholder="Additional details..."
            ></textarea>
          </div>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            @click="showAddDialog = false; newFunction = { name: '', purpose: '', category: '', description: '' }"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleAddFunction"
            :disabled="!newFunction.name || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Adding...
            </span>
            <span v-else>Add Function</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Edit Function Dialog -->
    <div
      v-if="showEditDialog && editingFunction"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      @click.self="showEditDialog = false"
      @keydown="handleDialogKeydown($event, () => showEditDialog = false)"
    >
      <div class="w-full max-w-lg orbitos-glass p-6" @keydown="handleEditFormKeydown">
        <h2 class="orbitos-heading-sm">Edit Function</h2>
        <p class="text-sm orbitos-text mt-1">Update function information.</p>

        <div class="mt-6 space-y-4">
          <div>
            <label class="orbitos-label">Function Name *</label>
            <input
              v-model="editingFunction.name"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Handle Inbound Request"
              autofocus
            />
          </div>

          <div>
            <label class="orbitos-label">Purpose</label>
            <input
              v-model="editingFunction.purpose"
              type="text"
              class="orbitos-input"
              placeholder="Why does this function exist?"
            />
          </div>

          <div>
            <label class="orbitos-label">Category</label>
            <input
              v-model="editingFunction.category"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Sales, Finance, Operations"
              list="category-suggestions-edit"
            />
            <datalist id="category-suggestions-edit">
              <option v-for="cat in categories" :key="cat" :value="cat" />
            </datalist>
          </div>

          <div>
            <label class="orbitos-label">Description</label>
            <textarea
              v-model="editingFunction.description"
              rows="3"
              class="orbitos-input"
              placeholder="Additional details..."
            ></textarea>
          </div>

          <!-- Role Assignment -->
          <div class="pt-2 border-t border-white/10">
            <SearchableAssigner
              :assigned="assignedRolesForAssigner"
              :available="availableRolesForAssigner"
              label="Assigned Roles"
              search-placeholder="Search roles to add..."
              :loading="loadingRoles"
              empty-assigned-text="No roles assigned to this function"
              empty-available-text="All roles are already assigned"
              no-results-text="No matching roles found"
              @add="handleAddRole"
              @remove="handleRemoveRole"
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
            @click="showEditDialog = false; editingFunction = null"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleEditFunction"
            :disabled="!editingFunction.name || isSubmitting"
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
