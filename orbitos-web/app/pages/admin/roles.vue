<script setup lang="ts">
import { useSuperAdmin, type Role, type Organization } from '~/composables/useSuperAdmin'

definePageMeta({
  layout: 'admin'
})

const { getRoles, getRolesCount, getOrganizations, createRole, updateRole, deleteRole } = useSuperAdmin()

const roles = ref<Role[]>([])
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
const selectedRole = ref<Role | null>(null)
const formData = ref({
  name: '',
  description: '',
  purpose: '',
  department: '',
  organizationId: ''
})
const formError = ref('')
const formLoading = ref(false)

// Delete confirmation
const showDeleteConfirm = ref(false)
const roleToDelete = ref<Role | null>(null)
const deleteLoading = ref(false)

const loadRoles = async () => {
  loading.value = true
  try {
    const [rolesData, count] = await Promise.all([
      getRoles(selectedOrgFilter.value || undefined, search.value || undefined, page.value, pageSize),
      getRolesCount(selectedOrgFilter.value || undefined, search.value || undefined)
    ])
    roles.value = rolesData
    totalCount.value = count
  } catch (e) {
    console.error('Failed to load roles:', e)
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
  await loadRoles()
})

watch([search, selectedOrgFilter], () => {
  page.value = 1
  loadRoles()
})

watch(page, loadRoles)

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize))

const openCreateModal = () => {
  modalMode.value = 'create'
  selectedRole.value = null
  formData.value = {
    name: '',
    description: '',
    purpose: '',
    department: '',
    organizationId: selectedOrgFilter.value || (organizations.value[0]?.id || '')
  }
  formError.value = ''
  showModal.value = true
}

const openEditModal = (role: Role) => {
  modalMode.value = 'edit'
  selectedRole.value = role
  formData.value = {
    name: role.name,
    description: role.description || '',
    purpose: role.purpose || '',
    department: role.department || '',
    organizationId: role.organizationId
  }
  formError.value = ''
  showModal.value = true
}

const handleSubmit = async () => {
  formError.value = ''
  formLoading.value = true

  try {
    if (modalMode.value === 'create') {
      await createRole({
        name: formData.value.name,
        description: formData.value.description || undefined,
        purpose: formData.value.purpose || undefined,
        department: formData.value.department || undefined,
        organizationId: formData.value.organizationId
      })
    } else if (selectedRole.value) {
      await updateRole(selectedRole.value.id, {
        name: formData.value.name,
        description: formData.value.description || undefined,
        purpose: formData.value.purpose || undefined,
        department: formData.value.department || undefined
      })
    }

    showModal.value = false
    await loadRoles()
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

const confirmDelete = (role: Role) => {
  roleToDelete.value = role
  showDeleteConfirm.value = true
}

const handleDelete = async () => {
  if (!roleToDelete.value) return

  deleteLoading.value = true
  try {
    await deleteRole(roleToDelete.value.id)
    showDeleteConfirm.value = false
    roleToDelete.value = null
    await loadRoles()
  } catch (e) {
    console.error('Failed to delete role:', e)
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
</script>

<template>
  <div>
    <!-- Header -->
    <div class="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-white">Roles</h1>
        <p class="mt-1 text-slate-400">Manage roles and permissions per organization</p>
      </div>
      <button
        @click="openCreateModal"
        :disabled="organizations.length === 0"
        class="inline-flex items-center gap-2 rounded-xl bg-purple-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-purple-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Add Role
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
          placeholder="Search roles..."
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

    <!-- Roles table -->
    <div v-else class="rounded-2xl bg-slate-800/50 border border-slate-700/50 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead>
            <tr class="border-b border-slate-700">
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Role</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Organization</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Department</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Users</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Functions</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Created</th>
              <th class="px-6 py-4 text-right text-xs font-medium text-slate-400 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-700/50">
            <tr v-if="roles.length === 0">
              <td colspan="7" class="px-6 py-12 text-center text-slate-400">
                No roles found
              </td>
            </tr>
            <tr v-for="role in roles" :key="role.id" class="hover:bg-slate-700/30 transition-colors">
              <td class="px-6 py-4">
                <div>
                  <p class="text-sm font-medium text-white">{{ role.name }}</p>
                  <p v-if="role.description" class="text-sm text-slate-400 line-clamp-1">{{ role.description }}</p>
                </div>
              </td>
              <td class="px-6 py-4">
                <span class="inline-flex items-center rounded-full bg-blue-500/20 px-2.5 py-1 text-xs font-medium text-blue-300">
                  {{ role.organizationName }}
                </span>
              </td>
              <td class="px-6 py-4">
                <span v-if="role.department" class="text-sm text-slate-300">{{ role.department }}</span>
                <span v-else class="text-sm text-slate-500">-</span>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-white">{{ role.userCount }}</span>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-white">{{ role.functionCount }}</span>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-slate-300">{{ formatDate(role.createdAt) }}</span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center justify-end gap-2">
                  <button
                    @click="openEditModal(role)"
                    class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
                    title="Edit role"
                  >
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    @click="confirmDelete(role)"
                    class="rounded-lg p-2 text-slate-400 hover:bg-red-500/20 hover:text-red-400 transition-colors"
                    title="Delete role"
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
          Showing {{ (page - 1) * pageSize + 1 }} to {{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }} roles
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
    <Teleport to="body">
      <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/50" @click="showModal = false" />
        <div class="relative w-full max-w-md rounded-2xl bg-slate-800 border border-slate-700 p-6">
          <h2 class="text-xl font-bold text-white mb-6">
            {{ modalMode === 'create' ? 'Create Role' : 'Edit Role' }}
          </h2>

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
                placeholder="e.g., Project Manager"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-300 mb-2">Description</label>
              <textarea
                v-model="formData.description"
                rows="2"
                class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500 resize-none"
                placeholder="Brief description of this role..."
              />
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-slate-300 mb-2">Purpose</label>
                <input
                  v-model="formData.purpose"
                  type="text"
                  class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                  placeholder="e.g., Management"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-300 mb-2">Department</label>
                <input
                  v-model="formData.department"
                  type="text"
                  class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                  placeholder="e.g., Engineering"
                />
              </div>
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
        </div>
      </div>
    </Teleport>

    <!-- Delete Confirmation Modal -->
    <Teleport to="body">
      <div v-if="showDeleteConfirm" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/50" @click="showDeleteConfirm = false" />
        <div class="relative w-full max-w-sm rounded-2xl bg-slate-800 border border-slate-700 p-6">
          <div class="text-center">
            <div class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-red-500/20 mb-4">
              <svg class="h-6 w-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            <h3 class="text-lg font-semibold text-white mb-2">Delete Role</h3>
            <p class="text-sm text-slate-400 mb-6">
              Are you sure you want to delete <span class="font-medium text-white">{{ roleToDelete?.name }}</span>? Users with this role will lose these permissions.
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
        </div>
      </div>
    </Teleport>
  </div>
</template>
