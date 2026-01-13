<script setup lang="ts">
import { useSuperAdmin, type Organization } from '~/composables/useSuperAdmin'

definePageMeta({
  layout: 'admin'
})

const { getOrganizations, getOrganizationsCount, createOrganization, updateOrganization, deleteOrganization } = useSuperAdmin()

const organizations = ref<Organization[]>([])
const totalCount = ref(0)
const loading = ref(true)
const search = ref('')
const page = ref(1)
const pageSize = 20

// Modal state
const showModal = ref(false)
const modalMode = ref<'create' | 'edit'>('create')
const selectedOrg = ref<Organization | null>(null)
const formData = ref({
  name: '',
  slug: '',
  description: '',
  logoUrl: ''
})
const formError = ref('')
const formLoading = ref(false)

// Delete confirmation
const showDeleteConfirm = ref(false)
const orgToDelete = ref<Organization | null>(null)
const deleteLoading = ref(false)

const loadOrganizations = async () => {
  loading.value = true
  try {
    const [orgsData, count] = await Promise.all([
      getOrganizations(search.value || undefined, page.value, pageSize),
      getOrganizationsCount(search.value || undefined)
    ])
    organizations.value = orgsData
    totalCount.value = count
  } catch (e) {
    console.error('Failed to load organizations:', e)
  } finally {
    loading.value = false
  }
}

onMounted(loadOrganizations)

watch([search], () => {
  page.value = 1
  loadOrganizations()
})

watch(page, loadOrganizations)

const totalPages = computed(() => Math.ceil(totalCount.value / pageSize))

// Auto-generate slug from name
watch(() => formData.value.name, (name) => {
  if (modalMode.value === 'create') {
    formData.value.slug = name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
  }
})

const openCreateModal = () => {
  modalMode.value = 'create'
  selectedOrg.value = null
  formData.value = { name: '', slug: '', description: '', logoUrl: '' }
  formError.value = ''
  showModal.value = true
}

const openEditModal = (org: Organization) => {
  modalMode.value = 'edit'
  selectedOrg.value = org
  formData.value = {
    name: org.name,
    slug: org.slug,
    description: org.description || '',
    logoUrl: org.logoUrl || ''
  }
  formError.value = ''
  showModal.value = true
}

const handleSubmit = async () => {
  formError.value = ''
  formLoading.value = true

  try {
    if (modalMode.value === 'create') {
      await createOrganization({
        name: formData.value.name,
        slug: formData.value.slug,
        description: formData.value.description || undefined,
        logoUrl: formData.value.logoUrl || undefined
      })
    } else if (selectedOrg.value) {
      await updateOrganization(selectedOrg.value.id, {
        name: formData.value.name,
        slug: formData.value.slug,
        description: formData.value.description || undefined,
        logoUrl: formData.value.logoUrl || undefined
      })
    }

    showModal.value = false
    await loadOrganizations()
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

const confirmDelete = (org: Organization) => {
  orgToDelete.value = org
  showDeleteConfirm.value = true
}

const handleDelete = async () => {
  if (!orgToDelete.value) return

  deleteLoading.value = true
  try {
    await deleteOrganization(orgToDelete.value.id)
    showDeleteConfirm.value = false
    orgToDelete.value = null
    await loadOrganizations()
  } catch (e) {
    console.error('Failed to delete organization:', e)
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
        <h1 class="text-2xl font-bold text-white">Organizations</h1>
        <p class="mt-1 text-slate-400">Manage organizations and tenants</p>
      </div>
      <button
        @click="openCreateModal"
        class="inline-flex items-center gap-2 rounded-xl bg-purple-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-purple-500 transition-colors"
      >
        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Add Organization
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
          placeholder="Search organizations..."
          class="w-full rounded-xl bg-slate-800 border border-slate-700 pl-12 pr-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
        />
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-12">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-purple-500/30 border-t-purple-500"></div>
    </div>

    <!-- Organizations grid -->
    <div v-else>
      <div v-if="organizations.length === 0" class="text-center py-12">
        <p class="text-slate-400">No organizations found</p>
      </div>

      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div
          v-for="org in organizations"
          :key="org.id"
          class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6 hover:border-purple-500/30 transition-colors"
        >
          <div class="flex items-start justify-between mb-4">
            <div class="flex items-center gap-3">
              <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 to-purple-600 text-lg font-bold text-white">
                {{ org.name.charAt(0).toUpperCase() }}
              </div>
              <div>
                <h3 class="text-lg font-semibold text-white">{{ org.name }}</h3>
                <p class="text-sm text-slate-400">{{ org.slug }}</p>
              </div>
            </div>
            <div class="flex items-center gap-1">
              <button
                @click="openEditModal(org)"
                class="rounded-lg p-2 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button
                @click="confirmDelete(org)"
                class="rounded-lg p-2 text-slate-400 hover:bg-red-500/20 hover:text-red-400 transition-colors"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
          </div>

          <p v-if="org.description" class="text-sm text-slate-300 mb-4 line-clamp-2">{{ org.description }}</p>

          <div class="grid grid-cols-3 gap-4 pt-4 border-t border-slate-700/50">
            <div class="text-center">
              <p class="text-2xl font-bold text-white">{{ org.memberCount }}</p>
              <p class="text-xs text-slate-400">Members</p>
            </div>
            <div class="text-center">
              <p class="text-2xl font-bold text-white">{{ org.roleCount }}</p>
              <p class="text-xs text-slate-400">Roles</p>
            </div>
            <div class="text-center">
              <p class="text-2xl font-bold text-white">{{ org.functionCount }}</p>
              <p class="text-xs text-slate-400">Functions</p>
            </div>
          </div>

          <div class="mt-4 pt-4 border-t border-slate-700/50 flex items-center justify-between text-xs text-slate-400">
            <span>Created {{ formatDate(org.createdAt) }}</span>
            <NuxtLink
              :to="`/admin/organizations/${org.id}`"
              class="text-purple-400 hover:text-purple-300 transition-colors"
            >
              View Details →
            </NuxtLink>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex items-center justify-between mt-6">
        <p class="text-sm text-slate-400">
          Showing {{ (page - 1) * pageSize + 1 }} to {{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }} organizations
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
            {{ modalMode === 'create' ? 'Create Organization' : 'Edit Organization' }}
          </h2>

          <form @submit.prevent="handleSubmit" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-slate-300 mb-2">Name</label>
              <input
                v-model="formData.name"
                type="text"
                required
                class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                placeholder="Acme Corp"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-300 mb-2">Slug</label>
              <input
                v-model="formData.slug"
                type="text"
                required
                pattern="[a-z0-9-]+"
                class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                placeholder="acme-corp"
              />
              <p class="mt-1 text-xs text-slate-400">URL-friendly identifier (lowercase, hyphens only)</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-300 mb-2">Description</label>
              <textarea
                v-model="formData.description"
                rows="3"
                class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500 resize-none"
                placeholder="Brief description of the organization..."
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-300 mb-2">Logo URL</label>
              <input
                v-model="formData.logoUrl"
                type="url"
                class="w-full rounded-xl bg-slate-700 border border-slate-600 px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:border-purple-500"
                placeholder="https://example.com/logo.png"
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
            <h3 class="text-lg font-semibold text-white mb-2">Delete Organization</h3>
            <p class="text-sm text-slate-400 mb-6">
              Are you sure you want to delete <span class="font-medium text-white">{{ orgToDelete?.name }}</span>? This will also delete all associated roles, functions, and memberships.
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
