<script setup lang="ts">
import type { OpsResource } from '~/types/operations'

definePageMeta({
  layout: 'app'
})

const {
  resources,
  resourceSubtypes,
  isLoading,
  fetchResources,
  fetchResourceSubtypes,
  createResourceSubtype,
  createResource,
  updateResource,
  deleteResource,
} = useOperations()

// Dialog state
const showDialog = ref(false)
const isEditing = ref(false)
const isSaving = ref(false)
const editingResource = ref<OpsResource | null>(null)

// Form state
const form = ref({
  name: '',
  description: '',
  status: 'active' as OpsResource['status'],
  resourceSubtypeId: '',
})

// Fetch resources and subtypes on mount
onMounted(async () => {
  await Promise.all([fetchResources(), fetchResourceSubtypes()])
})

// Status colors
const statusColor = (status: string) => {
  switch (status) {
    case 'active': return 'bg-emerald-500/20 text-emerald-300'
    case 'inactive': return 'bg-amber-500/20 text-amber-300'
    case 'archived': return 'bg-slate-500/20 text-slate-300'
    default: return 'bg-blue-500/20 text-blue-300'
  }
}

// Resource type icons
const resourceTypeLabel = (type: string) => {
  switch (type) {
    case 'person': return 'Person'
    case 'team': return 'Team'
    case 'tool': return 'Tool'
    case 'automation': return 'Automation'
    case 'partner': return 'Partner'
    case 'asset': return 'Asset'
    default: return type
  }
}

// Open dialog for adding new resource
const openAddDialog = () => {
  isEditing.value = false
  editingResource.value = null
  form.value = {
    name: '',
    description: '',
    status: 'active',
    resourceSubtypeId: resourceSubtypes.value[0]?.id || '',
  }
  showDialog.value = true
}

// Open dialog for editing resource
const openEditDialog = (resource: OpsResource) => {
  isEditing.value = true
  editingResource.value = resource
  form.value = {
    name: resource.name,
    description: resource.description || '',
    status: resource.status,
    resourceSubtypeId: '', // We don't have this from the transformed resource
  }
  showDialog.value = true
}

// Save resource (create or update)
const saveResource = async () => {
  if (!form.value.name.trim()) return

  isSaving.value = true
  try {
    if (isEditing.value && editingResource.value) {
      await updateResource(editingResource.value.id, {
        name: form.value.name,
        description: form.value.description || undefined,
        status: form.value.status,
      })
    } else {
      // For new resources, we need a subtype
      let subtypeId = form.value.resourceSubtypeId

      // If no subtypes exist, create a default one
      if (!subtypeId && resourceSubtypes.value.length === 0) {
        const newSubtype = await createResourceSubtype({
          name: 'General',
          description: 'General resource type',
          resourceType: 'person',
        })
        subtypeId = newSubtype.id
      } else if (!subtypeId && resourceSubtypes.value.length > 0) {
        const firstSubtype = resourceSubtypes.value[0]
        if (firstSubtype) {
          subtypeId = firstSubtype.id
        }
      }

      await createResource({
        name: form.value.name,
        description: form.value.description || undefined,
        status: form.value.status,
        resourceSubtypeId: subtypeId,
      })
    }
    showDialog.value = false
  } catch (e) {
    console.error('Failed to save resource:', e)
  } finally {
    isSaving.value = false
  }
}

// Delete resource
const handleDeleteResource = async (resource: OpsResource) => {
  if (!confirm(`Are you sure you want to delete "${resource.name}"?`)) return

  try {
    await deleteResource(resource.id)
  } catch (e) {
    console.error('Failed to delete resource:', e)
  }
}

// Close dialog
const closeDialog = () => {
  showDialog.value = false
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Resource Registry</h1>
        <p class="orbitos-text">People, tools, partners, and assets powering operations.</p>
      </div>
      <button type="button" class="orbitos-btn-primary py-2 px-4 text-sm" @click="openAddDialog">
        Add Resource
      </button>
    </div>

    <!-- Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total Resources</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ resources.length }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">People</div>
        <div class="mt-1 text-2xl font-semibold text-purple-300">
          {{ resources.filter(r => r.resourceType === 'person').length }}
        </div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Tools</div>
        <div class="mt-1 text-2xl font-semibold text-blue-300">
          {{ resources.filter(r => r.resourceType === 'tool').length }}
        </div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Active</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-300">
          {{ resources.filter(r => r.status === 'active').length }}
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="resources.length === 0" class="orbitos-card-static p-12 text-center">
      <div class="orbitos-text">No resources registered yet.</div>
      <button type="button" class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors" @click="openAddDialog">
        Add your first resource
      </button>
    </div>

    <!-- Resources Table -->
    <div v-else class="orbitos-glass-subtle overflow-hidden">
      <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <div class="text-sm text-white/70">All Resources</div>
        <div class="text-xs text-white/40">{{ resources.length }} total</div>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-black/20 text-xs uppercase text-white/40">
            <tr>
              <th class="px-6 py-3">Resource</th>
              <th class="px-6 py-3">Type</th>
              <th class="px-6 py-3">Description</th>
              <th class="px-6 py-3">Status</th>
              <th class="px-6 py-3">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr v-for="resource in resources" :key="resource.id" class="hover:bg-white/5 transition-colors">
              <td class="px-6 py-4 text-white font-semibold">{{ resource.name }}</td>
              <td class="px-6 py-4 text-white/70">
                <span class="rounded bg-white/10 px-2 py-1 text-xs">
                  {{ resourceTypeLabel(resource.resourceType) }}
                </span>
              </td>
              <td class="px-6 py-4 text-white/60 max-w-xs truncate">{{ resource.description || '-' }}</td>
              <td class="px-6 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-medium', statusColor(resource.status)]">
                  {{ resource.status }}
                </span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                  <button
                    type="button"
                    class="text-white/60 hover:text-white transition-colors"
                    @click="openEditDialog(resource)"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    class="text-red-400/60 hover:text-red-400 transition-colors"
                    @click="handleDeleteResource(resource)"
                  >
                    Delete
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Add/Edit Dialog -->
    <BaseDialog
      v-model="showDialog"
      size="md"
      :title="isEditing ? 'Edit Resource' : 'Add Resource'"
      @close="closeDialog"
      @submit="saveResource"
    >
      <div class="space-y-4">
        <!-- Name -->
        <div>
          <label class="block text-sm font-medium text-white/70 mb-1">Name</label>
          <input
            v-model="form.name"
            type="text"
            class="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-white/40 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
            placeholder="Resource name"
            autofocus
          />
        </div>

        <!-- Description -->
        <div>
          <label class="block text-sm font-medium text-white/70 mb-1">Description</label>
          <textarea
            v-model="form.description"
            rows="3"
            class="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white placeholder-white/40 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
            placeholder="Optional description"
          ></textarea>
        </div>

        <!-- Resource Type (only for new resources) -->
        <div v-if="!isEditing && resourceSubtypes.length > 0">
          <label class="block text-sm font-medium text-white/70 mb-1">Type</label>
          <select
            v-model="form.resourceSubtypeId"
            class="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          >
            <option v-for="subtype in resourceSubtypes" :key="subtype.id" :value="subtype.id">
              {{ subtype.name }} ({{ resourceTypeLabel(subtype.resourceType) }})
            </option>
          </select>
        </div>

        <!-- Status -->
        <div>
          <label class="block text-sm font-medium text-white/70 mb-1">Status</label>
          <select
            v-model="form.status"
            class="w-full rounded-lg bg-white/5 border border-white/10 px-4 py-2 text-white focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          >
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
            <option value="archived">Archived</option>
          </select>
        </div>
      </div>

      <template #footer="{ close }">
        <div class="flex justify-end gap-3">
          <button
            type="button"
            class="rounded-lg px-4 py-2 text-sm text-white/70 hover:text-white transition-colors"
            @click="close"
          >
            Cancel
          </button>
          <button
            type="button"
            class="orbitos-btn-primary px-4 py-2 text-sm"
            :disabled="isSaving || !form.name.trim()"
            @click="saveResource"
          >
            {{ isSaving ? 'Saving...' : (isEditing ? 'Save Changes' : 'Add Resource') }}
          </button>
        </div>
      </template>
    </BaseDialog>
  </div>
</template>
