<script setup lang="ts">
import { useSuperAdmin, type User } from '~/composables/useSuperAdmin'

definePageMeta({
  layout: 'admin'
})

const { getUsers, getUsersCount, createUser, updateUser, deleteUser, resetUserPassword } = useSuperAdmin()

const users = ref<User[]>([])
const totalCount = ref(0)
const loading = ref(true)
const search = ref('')
const page = ref(1)
const pageSize = 20

// Modal state
const showModal = ref(false)
const modalMode = ref<'create' | 'edit' | 'password'>('create')
const selectedUser = ref<User | null>(null)
const formData = ref({
  email: '',
  displayName: '',
  firstName: '',
  lastName: '',
  password: ''
})
const formError = ref('')
const formLoading = ref(false)

// Delete confirmation
const showDeleteConfirm = ref(false)
const userToDelete = ref<User | null>(null)
const deleteLoading = ref(false)

const loadUsers = async () => {
  loading.value = true
  try {
    const [usersData, count] = await Promise.all([
      getUsers(search.value || undefined, page.value, pageSize),
      getUsersCount(search.value || undefined)
    ])
    users.value = usersData
    totalCount.value = count
  } catch (e) {
    console.error('Failed to load users:', e)
  } finally {
    loading.value = false
  }
}

onMounted(loadUsers)

watch([search], () => {
  page.value = 1
  loadUsers()
})

watch(page, loadUsers)

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize))

const openCreateModal = () => {
  modalMode.value = 'create'
  selectedUser.value = null
  formData.value = { email: '', displayName: '', firstName: '', lastName: '', password: '' }
  formError.value = ''
  showModal.value = true
}

const openEditModal = (user: User) => {
  modalMode.value = 'edit'
  selectedUser.value = user
  formData.value = {
    email: user.email,
    displayName: user.displayName,
    firstName: user.firstName || '',
    lastName: user.lastName || '',
    password: ''
  }
  formError.value = ''
  showModal.value = true
}

const openPasswordModal = (user: User) => {
  modalMode.value = 'password'
  selectedUser.value = user
  formData.value = { ...formData.value, password: '' }
  formError.value = ''
  showModal.value = true
}

const handleSubmit = async () => {
  formError.value = ''
  formLoading.value = true

  try {
    if (modalMode.value === 'create') {
      await createUser({
        email: formData.value.email,
        displayName: formData.value.displayName,
        firstName: formData.value.firstName || undefined,
        lastName: formData.value.lastName || undefined,
        password: formData.value.password || undefined
      })
    } else if (modalMode.value === 'edit' && selectedUser.value) {
      await updateUser(selectedUser.value.id, {
        email: formData.value.email,
        displayName: formData.value.displayName,
        firstName: formData.value.firstName || undefined,
        lastName: formData.value.lastName || undefined
      })
    } else if (modalMode.value === 'password' && selectedUser.value) {
      await resetUserPassword(selectedUser.value.id, formData.value.password)
    }

    showModal.value = false
    await loadUsers()
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

const confirmDelete = (user: User) => {
  userToDelete.value = user
  showDeleteConfirm.value = true
}

const handleDelete = async () => {
  if (!userToDelete.value) return

  deleteLoading.value = true
  try {
    await deleteUser(userToDelete.value.id)
    showDeleteConfirm.value = false
    userToDelete.value = null
    await loadUsers()
  } catch (e) {
    console.error('Failed to delete user:', e)
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

const getAuthMethods = (user: User) => {
  const methods = []
  if (user.hasPassword) methods.push('Password')
  if (user.hasGoogleId) methods.push('Google')
  if (user.hasAzureAdId) methods.push('Microsoft')
  return methods
}
</script>

<template>
  <div>
    <!-- Header -->
    <div class="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-white">Users</h1>
        <p class="mt-1 text-slate-400">Manage user accounts and authentication</p>
      </div>
      <button
        @click="openCreateModal"
        class="inline-flex items-center gap-2 rounded-xl bg-purple-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-purple-500 transition-colors"
      >
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Add User
      </button>
    </div>

    <!-- Search -->
    <div class="mb-6">
      <div class="relative">
        <svg class="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          v-model="search"
          type="text"
          placeholder="Search users by email or name..."
          class="w-full rounded-xl bg-slate-800 border border-slate-700 pl-12 pr-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
        />
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-12">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-purple-500/30 border-t-purple-500"></div>
    </div>

    <!-- Users table -->
    <div v-else class="rounded-2xl bg-slate-800/50 border border-slate-700/50 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead>
            <tr class="border-b border-slate-700">
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">User</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Auth Methods</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Organizations</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Last Login</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-slate-400 uppercase tracking-wider">Created</th>
              <th class="px-6 py-4 text-right text-xs font-medium text-slate-400 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-700/50">
            <tr v-if="users.length === 0">
              <td colspan="6" class="px-6 py-12 text-center text-slate-400">
                No users found
              </td>
            </tr>
            <tr v-for="user in users" :key="user.id" class="hover:bg-slate-700/30 transition-colors">
              <td class="px-6 py-4">
                <div class="flex items-center gap-3">
                  <div class="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white">
                    {{ user.displayName.charAt(0).toUpperCase() }}
                  </div>
                  <div>
                    <p class="text-sm font-medium text-white">{{ user.displayName }}</p>
                    <p class="text-sm text-slate-400">{{ user.email }}</p>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4">
                <div class="flex flex-wrap gap-1">
                  <span
                    v-for="method in getAuthMethods(user)"
                    :key="method"
                    :class="[
                      'inline-flex items-center rounded-full px-2 py-1 text-xs font-medium',
                      method === 'Password' ? 'bg-slate-600 text-slate-200' :
                      method === 'Google' ? 'bg-red-500/20 text-red-300' :
                      'bg-blue-500/20 text-blue-300'
                    ]"
                  >
                    {{ method }}
                  </span>
                  <span v-if="getAuthMethods(user).length === 0" class="text-sm text-slate-500">None</span>
                </div>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-white">{{ user.organizationCount }}</span>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-slate-300">{{ user.lastLoginAt ? formatDate(user.lastLoginAt) : 'Never' }}</span>
              </td>
              <td class="px-6 py-4">
                <span class="text-sm text-slate-300">{{ formatDate(user.createdAt) }}</span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center justify-end gap-2">
                  <button
                    @click="openEditModal(user)"
                    class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
                    title="Edit user"
                  >
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    @click="openPasswordModal(user)"
                    class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
                    title="Reset password"
                  >
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
                    </svg>
                  </button>
                  <button
                    @click="confirmDelete(user)"
                    class="rounded-lg p-2 text-slate-400 hover:bg-red-500/20 hover:text-red-400 transition-colors"
                    title="Delete user"
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
          Showing {{ (page - 1) * pageSize + 1 }} to {{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }} users
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
            {{ modalMode === 'create' ? 'Create User' : modalMode === 'edit' ? 'Edit User' : 'Reset Password' }}
          </h2>

          <form @submit.prevent="handleSubmit" class="space-y-4">
            <template v-if="modalMode !== 'password'">
              <div>
                <label class="block text-sm font-medium text-slate-300 mb-2">Email</label>
                <input
                  v-model="formData.email"
                  type="email"
                  required
                  class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-300 mb-2">Display Name</label>
                <input
                  v-model="formData.displayName"
                  type="text"
                  required
                  class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                />
              </div>
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-slate-300 mb-2">First Name</label>
                  <input
                    v-model="formData.firstName"
                    type="text"
                    class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-slate-300 mb-2">Last Name</label>
                  <input
                    v-model="formData.lastName"
                    type="text"
                    class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                  />
                </div>
              </div>
            </template>

            <div v-if="modalMode === 'create' || modalMode === 'password'">
              <label class="block text-sm font-medium text-slate-300 mb-2">
                {{ modalMode === 'create' ? 'Password (optional)' : 'New Password' }}
              </label>
              <input
                v-model="formData.password"
                type="password"
                :required="modalMode === 'password'"
                class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
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
            <h3 class="text-lg font-semibold text-white mb-2">Delete User</h3>
            <p class="text-sm text-slate-400 mb-6">
              Are you sure you want to delete <span class="font-medium text-white">{{ userToDelete?.displayName }}</span>? This action cannot be undone.
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
