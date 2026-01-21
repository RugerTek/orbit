<script setup lang="ts">
import { useSuperAdmin, type FunctionEntity, type Organization } from '~/composables/useSuperAdmin'
import type { AssignableItem } from '~/components/SearchableAssigner.vue'
import type { RoleFunction } from '~/composables/useOperations'

definePageMeta({
  layout: 'admin'
})

const { getFunctions, getFunctionsCount, getOrganizations, createFunction, updateFunction, deleteFunction } = useSuperAdmin()
const {
  roles,
  fetchRoles,
  fetchFunctionRoles,
  assignRoleToFunction,
  unassignRoleFromFunction,
  refreshOrganizationId,
} = useOperations()

const functions = ref<FunctionEntity[]>([])
const organizations = ref<Organization[]>([])
const totalCount = ref(0)
const loading = ref(true)
const search = ref('')
const selectedOrgFilter = ref('')
const page = ref(1)
const pageSize = 20

// Modal state
const showModal = ref(false)
const modalMode = ref<'create' | 'edit'>('create')
const selectedFunction = ref<FunctionEntity | null>(null)
const formData = ref({
  name: '',
  description: '',
  purpose: '',
  category: '',
  organizationId: ''
})
const formError = ref('')
const formLoading = ref(false)

// Delete confirmation
const showDeleteConfirm = ref(false)
const functionToDelete = ref<FunctionEntity | null>(null)
const deleteLoading = ref(false)

// Role assignment state (for edit mode)
const assignedRoles = ref<RoleFunction[]>([])
const loadingRoles = ref(false)

const loadFunctions = async () => {
  loading.value = true
  try {
    const [functionsData, count] = await Promise.all([
      getFunctions(selectedOrgFilter.value || undefined, search.value || undefined, page.value, pageSize),
      getFunctionsCount(selectedOrgFilter.value || undefined, search.value || undefined)
    ])
    functions.value = functionsData
    totalCount.value = count
  } catch (e) {
    console.error('Failed to load functions:', e)
  } finally {
    loading.value = false
  }
}

const loadOrganizations = async () => {
  try {
    organizations.value = await getOrganizations(undefined, 1, 100)
  } catch (e) {
    console.error('Failed to load organizations:', e)
  }
}

onMounted(async () => {
  await loadOrganizations()
  await loadFunctions()
})

watch([search, selectedOrgFilter], () => {
  page.value = 1
  loadFunctions()
})

watch(page, loadFunctions)

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize))

// Get unique categories from loaded functions
const categories = computed(() => {
  const cats = new Set<string>()
  functions.value.forEach(f => {
    if (f.category) cats.add(f.category)
  })
  return Array.from(cats).sort()
})

const openCreateModal = () => {
  modalMode.value = 'create'
  selectedFunction.value = null
  formData.value = {
    name: '',
    description: '',
    purpose: '',
    category: '',
    organizationId: selectedOrgFilter.value || (organizations.value[0]?.id || '')
  }
  formError.value = ''
  showModal.value = true
}

const openEditModal = async (func: FunctionEntity) => {
  modalMode.value = 'edit'
  selectedFunction.value = func
  formData.value = {
    name: func.name,
    description: func.description || '',
    purpose: func.purpose || '',
    category: func.category || '',
    organizationId: func.organizationId
  }
  formError.value = ''
  assignedRoles.value = []
  showModal.value = true

  // Load roles for the function's organization and the assigned roles
  await loadRolesForOrg(func.organizationId)
  await loadAssignedRoles(func.id)
}

const handleSubmit = async () => {
  formError.value = ''
  formLoading.value = true

  try {
    if (modalMode.value === 'create') {
      await createFunction({
        name: formData.value.name,
        description: formData.value.description || undefined,
        purpose: formData.value.purpose || undefined,
        category: formData.value.category || undefined,
        organizationId: formData.value.organizationId
      })
    } else if (selectedFunction.value) {
      await updateFunction(selectedFunction.value.id, {
        name: formData.value.name,
        description: formData.value.description || undefined,
        purpose: formData.value.purpose || undefined,
        category: formData.value.category || undefined
      })
    }

    showModal.value = false
    await loadFunctions()
  } catch (e: unknown) {
    if (e && typeof e === 'object' && 'data' in e) {
      const errData = e as { data?: { Message?: string } }
      formError.value = errData.data?.Message || 'An error occurred'
    } else {
      formError.value = 'An error occurred'
    }
  } finally {
    formLoading.value = false
  }
}

const confirmDelete = (func: FunctionEntity) => {
  functionToDelete.value = func
  showDeleteConfirm.value = true
}

const handleDelete = async () => {
  if (!functionToDelete.value) return

  deleteLoading.value = true
  try {
    await deleteFunction(functionToDelete.value.id)
    showDeleteConfirm.value = false
    functionToDelete.value = null
    await loadFunctions()
  } catch (e) {
    console.error('Failed to delete function:', e)
  } finally {
    deleteLoading.value = false
  }
}

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

const getCategoryColor = (category: string | undefined) => {
  if (!category) return 'bg-slate-500/20 text-slate-300'
  const colors: Record<string, string> = {
    'read': 'bg-green-500/20 text-green-300',
    'write': 'bg-blue-500/20 text-blue-300',
    'delete': 'bg-red-500/20 text-red-300',
    'admin': 'bg-purple-500/20 text-purple-300',
    'manage': 'bg-amber-500/20 text-amber-300',
  }
  return colors[category.toLowerCase()] || 'bg-slate-500/20 text-slate-300'
}

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

// Load roles for the selected organization
const loadRolesForOrg = async (orgId: string) => {
  // Update the organization context for useOperations
  if (typeof window !== 'undefined') {
    localStorage.setItem('currentOrganizationId', orgId)
    refreshOrganizationId()
  }
  loadingRoles.value = true
  try {
    await fetchRoles()
  } catch (e) {
    console.error('Failed to load roles:', e)
  } finally {
    loadingRoles.value = false
  }
}

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
  if (!selectedFunction.value) return
  try {
    const newAssignment = await assignRoleToFunction(selectedFunction.value.id, roleId)
    assignedRoles.value.push(newAssignment)
    // Increment the role count in the local function data
    const funcIndex = functions.value.findIndex(f => f.id === selectedFunction.value?.id)
    if (funcIndex !== -1) {
      functions.value[funcIndex].roleCount++
    }
  } catch (e) {
    console.error('Failed to assign role:', e)
  }
}

// Handle removing a role from the function
const handleRemoveRole = async (roleId: string) => {
  if (!selectedFunction.value) return
  try {
    await unassignRoleFromFunction(selectedFunction.value.id, roleId)
    assignedRoles.value = assignedRoles.value.filter(rf => rf.roleId !== roleId)
    // Decrement the role count in the local function data
    const funcIndex = functions.value.findIndex(f => f.id === selectedFunction.value?.id)
    if (funcIndex !== -1 && functions.value[funcIndex].roleCount > 0) {
      functions.value[funcIndex].roleCount--
    }
  } catch (e) {
    console.error('Failed to unassign role:', e)
  }
}
</script>

<template>
  <div>
    <!-- Header -->
    <div class="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-white">Functions</h1>
        <p class="mt-1 text-slate-400">Manage functions and capabilities per organization</p>
      </div>
      <button
        @click="openCreateModal"
        :disabled="organizations.length === 0"
        class="inline-flex items-center gap-2 rounded-xl bg-purple-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-purple-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Add Function
      </button>
    </div>

    <!-- Filters -->
    <div class="mb-6 flex flex-col sm:flex-row gap-4">
      <div class="flex-1 relative">
        <svg class="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          v-model="search"
          type="text"
          placeholder="Search functions..."
          class="w-full rounded-xl bg-slate-800 border border-slate-700 pl-12 pr-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
        />
      </div>
      <select
        v-model="selectedOrgFilter"
        class="rounded-xl bg-slate-800 border border-slate-700 px-4 py-3 text-white focus:outline-none focus:border-purple-500"
      >
        <option value="">All Organizations</option>
        <option v-for="org in organizations" :key="org.id" :value="org.id">{{ org.name }}</option>
      </select>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-12">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-purple-500/30 border-t-purple-500"></div>
    </div>

    <!-- Functions table -->
    <div v-else class="rounded-2xl bg-slate-800/50 border border-slate-700/50 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead>
            <tr class="border-b border-slate-700">
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Function</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Organization</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Category</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Roles</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Created</th>
              <th class="px-6 py-4 text-right text-xs font-medium text-slate-400 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-700/50">
            <tr v-if="functions.length === 0">
              <td colspan="6" class="px-6 py-12 text-center text-slate-400">
                No functions found
              </td>
            </tr>
            <tr v-for="func in functions" :key="func.id" class="hover:bg-slate-700/30 transition-colors">
              <td class="px-6 py-4">
                <div>
                  <p class="text-sm font-medium text-white">{{ func.name }}</p>
                  <p v-if="func.description" class="text-sm text-slate-400 line-clamp-1">{{ func.description }}</p>
                  <p v-if="func.purpose" class="text-xs text-slate-500 mt-1">{{ func.purpose }}</p>
                </div>
              </td>
              <td class="px-6 py-4">
                <span class="inline-flex items-center rounded-full bg-blue-500/20 px-2.5 py-1 text-xs font-medium text-blue-300">
                  {{ func.organizationName }}
                </span>
              </td>
              <td class="px-6 py-4">
                <span
                  v-if="func.category"
                  :class="['inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium', getCategoryColor(func.category)]"
                >
                  {{ func.category }}
                </span>
                <span v-else class="text-sm text-slate-500">-</span>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-white">{{ func.roleCount }}</span>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-slate-300">{{ formatDate(func.createdAt) }}</span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center justify-end gap-2">
                  <button
                    @click="openEditModal(func)"
                    class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
                    title="Edit function"
                  >
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    @click="confirmDelete(func)"
                    class="rounded-lg p-2 text-slate-400 hover:bg-red-500/20 hover:text-red-400 transition-colors"
                    title="Delete function"
                  >
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex items-center justify-between border-t border-slate-700 px-6 py-4">
        <p class="text-sm text-slate-400">
          Showing {{ (page - 1) * pageSize + 1 }} to {{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }} functions
        </p>
        <div class="flex items-center gap-2">
          <button
            @click="page = page - 1"
            :disabled="page === 1"
            class="rounded-lg px-3 py-2 text-sm text-slate-400 hover:bg-slate-700 hover:text-white disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Previous
          </button>
          <button
            @click="page = page + 1"
            :disabled="page === totalPages"
            class="rounded-lg px-3 py-2 text-sm text-slate-400 hover:bg-slate-700 hover:text-white disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Next
          </button>
        </div>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <BaseDialog
      v-model="showModal"
      size="md"
      :title="modalMode === 'create' ? 'Create Function' : 'Edit Function'"
      @submit="handleSubmit"
    >
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <div v-if="modalMode === 'create'">
          <label class="block text-sm font-medium text-slate-300 mb-2">Organization</label>
          <select
            v-model="formData.organizationId"
            required
            class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white focus:outline-none focus:border-purple-500"
          >
            <option v-for="org in organizations" :key="org.id" :value="org.id">{{ org.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-300 mb-2">Name</label>
          <input
            v-model="formData.name"
            type="text"
            required
            class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
            placeholder="e.g., users.read"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-300 mb-2">Description</label>
          <textarea
            v-model="formData.description"
            rows="2"
            class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500 resize-none"
            placeholder="What does this function allow..."
          />
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-slate-300 mb-2">Purpose</label>
            <input
              v-model="formData.purpose"
              type="text"
              class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
              placeholder="e.g., Data Access"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-300 mb-2">Category</label>
            <input
              v-model="formData.category"
              type="text"
              list="categories"
              class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
              placeholder="e.g., read"
            />
            <datalist id="categories">
              <option v-for="cat in categories" :key="cat" :value="cat" />
              <option value="read" />
              <option value="write" />
              <option value="delete" />
              <option value="admin" />
              <option value="manage" />
            </datalist>
          </div>
        </div>

        <!-- Role Assignment (only in edit mode) -->
        <div v-if="modalMode === 'edit'" class="pt-2 border-t border-slate-700">
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

        <div v-if="formError" class="rounded-xl bg-red-500/10 border border-red-500/20 p-3">
          <p class="text-sm text-red-400">{{ formError }}</p>
        </div>

        <div class="flex gap-3 pt-4">
          <button
            type="button"
            @click="showModal = false"
            class="flex-1 rounded-xl bg-slate-700 px-4 py-3 text-sm font-medium text-white hover:bg-slate-600 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            :disabled="formLoading"
            class="flex-1 rounded-xl bg-purple-600 px-4 py-3 text-sm font-medium text-white hover:bg-purple-500 disabled:opacity-50 transition-colors"
          >
            {{ formLoading ? 'Saving...' : modalMode === 'create' ? 'Create' : 'Save' }}
          </button>
        </div>
      </form>
    </BaseDialog>

    <!-- Delete Confirmation Modal -->
    <BaseDialog
      v-model="showDeleteConfirm"
      size="sm"
      title="Delete Function"
      @submit="handleDelete"
    >
      <div class="text-center">
        <div class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-red-500/20 mb-4">
          <svg class="h-6 w-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <p class="text-sm text-slate-400 mb-6">
          Are you sure you want to delete <span class="font-medium text-white">{{ functionToDelete?.name }}</span>? Roles with this function will lose this capability.
        </p>
        <div class="flex gap-3">
          <button
            @click="showDeleteConfirm = false"
            class="flex-1 rounded-xl bg-slate-700 px-4 py-3 text-sm font-medium text-white hover:bg-slate-600 transition-colors"
          >
            Cancel
          </button>
          <button
            @click="handleDelete"
            :disabled="deleteLoading"
            class="flex-1 rounded-xl bg-red-600 px-4 py-3 text-sm font-medium text-white hover:bg-red-500 disabled:opacity-50 transition-colors"
          >
            {{ deleteLoading ? 'Deleting...' : 'Delete' }}
          </button>
        </div>
      </div>
    </BaseDialog>
  </div>
</template>
