<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { roles, isLoading, fetchRoles, createRole, updateRole, deleteRole } = useOperations()

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const isSubmitting = ref(false)

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

// Fetch roles on mount
onMounted(async () => {
  await fetchRoles()
})

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
const openEditDialog = (role: { id: string; name: string; purpose?: string; department?: string; description?: string }) => {
  editingRole.value = {
    id: role.id,
    name: role.name,
    purpose: role.purpose || '',
    department: role.department || '',
    description: role.description || ''
  }
  showEditDialog.value = true
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
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Roles</h1>
        <p class="orbitos-text">Operational roles and their coverage status.</p>
      </div>
      <button
        type="button"
        @click="showAddDialog = true"
        class="orbitos-btn-primary py-2 px-4 text-sm"
      >
        Add Role
      </button>
    </div>

    <!-- Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total Roles</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ roles.length }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Covered</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-300">
          {{ roles.filter(r => r.coverageStatus === 'covered').length }}
        </div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">At Risk</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">
          {{ roles.filter(r => r.coverageStatus === 'at_risk').length }}
        </div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Uncovered</div>
        <div class="mt-1 text-2xl font-semibold text-red-300">
          {{ roles.filter(r => r.coverageStatus === 'uncovered').length }}
        </div>
      </div>
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

    <!-- Roles Grid -->
    <div v-else class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <div
        v-for="role in roles"
        :key="role.id"
        class="orbitos-card p-5"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <h3 class="font-semibold text-white">{{ role.name }}</h3>
            <p v-if="role.purpose" class="mt-1 text-sm text-white/50 line-clamp-2">{{ role.purpose }}</p>
          </div>
          <span :class="['rounded-full px-2.5 py-1 text-xs font-medium flex-shrink-0', statusColor(role.coverageStatus || 'uncovered')]">
            {{ statusLabel(role.coverageStatus || 'uncovered') }}
          </span>
        </div>

        <div class="mt-4 flex flex-wrap items-center gap-3 text-xs text-white/40">
          <span v-if="role.department" class="rounded bg-white/10 px-2 py-1">{{ role.department }}</span>
          <span>{{ role.assignmentCount || 0 }} assigned</span>
          <span>•</span>
          <span>{{ role.functionCount || 0 }} functions</span>
        </div>

        <div class="mt-4 flex gap-2 border-t border-white/10 pt-3">
          <button
            type="button"
            @click="openEditDialog(role)"
            class="flex-1 rounded-lg bg-white/5 px-3 py-2 text-center text-xs text-white/70 hover:bg-white/10 transition-colors"
          >
            Edit
          </button>
          <button
            type="button"
            @click="handleDeleteRole(role.id)"
            class="rounded-lg bg-red-500/10 px-3 py-2 text-xs text-red-400 hover:bg-red-500/20 transition-colors"
          >
            Delete
          </button>
        </div>
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
