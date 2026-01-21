<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { people, resources, roles, resourceSubtypes, isLoading, fetchPeople, fetchResources, fetchRoles, fetchRoleAssignments, fetchResourceSubtypes, createResourceSubtype, roleAssignments, createRoleAssignment, createRole } = useOperations()
const { get, post, put, delete: del } = useApi()
const { currentOrganizationId } = useOrganizations()

// Get organization ID dynamically
const getOrgId = (): string => {
  if (currentOrganizationId.value) {
    return currentOrganizationId.value
  }
  // Fallback to localStorage
  if (typeof window !== 'undefined') {
    return localStorage.getItem('currentOrganizationId') || '11111111-1111-1111-1111-111111111111'
  }
  return '11111111-1111-1111-1111-111111111111'
}

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const isSubmitting = ref(false)
const newPerson = ref({
  name: '',
  roleId: '' as string,
  roleName: '',
  email: ''
})
const editingPerson = ref<{
  id: string
  name: string
  roleId: string
  roleName: string
  email: string
} | null>(null)

// Role autocomplete state
const roleSearchQuery = ref('')
const showRoleDropdown = ref(false)
const editRoleSearchQuery = ref('')
const showEditRoleDropdown = ref(false)
const isCreatingRole = ref(false)

// Filtered roles based on search
const filteredRoles = computed(() => {
  const query = roleSearchQuery.value.toLowerCase().trim()
  if (!query) return roles.value
  return roles.value.filter(r =>
    r.name.toLowerCase().includes(query) ||
    r.department?.toLowerCase().includes(query)
  )
})

const editFilteredRoles = computed(() => {
  const query = editRoleSearchQuery.value.toLowerCase().trim()
  if (!query) return roles.value
  return roles.value.filter(r =>
    r.name.toLowerCase().includes(query) ||
    r.department?.toLowerCase().includes(query)
  )
})

// Check if typed role name exists
const roleNameExists = computed(() => {
  const query = roleSearchQuery.value.toLowerCase().trim()
  return roles.value.some(r => r.name.toLowerCase() === query)
})

const editRoleNameExists = computed(() => {
  const query = editRoleSearchQuery.value.toLowerCase().trim()
  return roles.value.some(r => r.name.toLowerCase() === query)
})

// Select a role from dropdown
const selectRole = (role: { id: string; name: string }) => {
  newPerson.value.roleId = role.id
  newPerson.value.roleName = role.name
  roleSearchQuery.value = role.name
  showRoleDropdown.value = false
}

const selectEditRole = (role: { id: string; name: string }) => {
  if (editingPerson.value) {
    editingPerson.value.roleId = role.id
    editingPerson.value.roleName = role.name
    editRoleSearchQuery.value = role.name
    showEditRoleDropdown.value = false
  }
}

// Create new role and select it
const createAndSelectRole = async () => {
  const roleName = roleSearchQuery.value.trim()
  if (!roleName || roleNameExists.value) return

  isCreatingRole.value = true
  try {
    const newRole = await createRole({ name: roleName })
    selectRole({ id: newRole.id, name: newRole.name })
  } catch (e) {
    console.error('Failed to create role:', e)
  } finally {
    isCreatingRole.value = false
  }
}

const createAndSelectEditRole = async () => {
  const roleName = editRoleSearchQuery.value.trim()
  if (!roleName || editRoleNameExists.value) return

  isCreatingRole.value = true
  try {
    const newRole = await createRole({ name: roleName })
    selectEditRole({ id: newRole.id, name: newRole.name })
  } catch (e) {
    console.error('Failed to create role:', e)
  } finally {
    isCreatingRole.value = false
  }
}

// Clear role selection
const clearRole = () => {
  newPerson.value.roleId = ''
  newPerson.value.roleName = ''
  roleSearchQuery.value = ''
}

const clearEditRole = () => {
  if (editingPerson.value) {
    editingPerson.value.roleId = ''
    editingPerson.value.roleName = ''
    editRoleSearchQuery.value = ''
  }
}

// Fetch people on mount
onMounted(async () => {
  await Promise.all([
    fetchPeople(),
    fetchRoles(),
    fetchRoleAssignments(),
    fetchResourceSubtypes()
  ])
})

// Get or create Employee subtype for this organization
const getEmployeeSubtypeId = async (): Promise<string | null> => {
  // First check if we already have an Employee subtype (or any Person subtype)
  const employeeSubtype = resourceSubtypes.value.find(
    s => s.name.toLowerCase() === 'employee' && s.resourceType === 'person'
  )

  if (employeeSubtype) {
    return employeeSubtype.id
  }

  // If Employee not found, look for any Person type subtype
  const anyPersonSubtype = resourceSubtypes.value.find(
    s => s.resourceType === 'person'
  )

  if (anyPersonSubtype) {
    return anyPersonSubtype.id
  }

  // If no Person subtype exists, create Employee
  try {
    const newSubtype = await createResourceSubtype({
      name: 'Employee',
      description: 'Full-time or part-time employee',
      resourceType: 'person'
    })
    return newSubtype.id
  } catch (e) {
    console.error('Failed to create Employee subtype:', e)
    return null
  }
}

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
  if (!newPerson.value.name) {
    console.warn('Name is required')
    return
  }

  isSubmitting.value = true
  try {
    // Get or create the Employee subtype for this organization
    console.log('Getting employee subtype for org:', getOrgId())
    console.log('Available subtypes:', resourceSubtypes.value)

    const employeeSubtypeId = await getEmployeeSubtypeId()
    if (!employeeSubtypeId) {
      console.error('Could not get or create Employee subtype')
      alert('Failed to create person: Could not find or create Employee subtype')
      return
    }

    console.log('Using subtype ID:', employeeSubtypeId)

    // Create resource with Employee subtype (Person type)
    const createdResource = await post<{ id: string }>(`/api/organizations/${getOrgId()}/operations/resources`, {
      name: newPerson.value.name,
      description: undefined, // No longer storing role as free text
      metadata: newPerson.value.email ? JSON.stringify({ email: newPerson.value.email }) : undefined,
      resourceSubtypeId: employeeSubtypeId,
      status: 1 // Active = 1
    })

    console.log('Created resource:', createdResource)

    // If a role was selected, create a RoleAssignment
    if (newPerson.value.roleId && createdResource?.id) {
      await createRoleAssignment({
        resourceId: createdResource.id,
        roleId: newPerson.value.roleId,
        isPrimary: true,
        allocationPercentage: 100
      })
    }

    // Refresh the lists
    await Promise.all([fetchPeople(), fetchRoleAssignments()])

    // Reset form and close dialog
    newPerson.value = { name: '', roleId: '', roleName: '', email: '' }
    roleSearchQuery.value = ''
    showAddDialog.value = false
  } catch (e: unknown) {
    console.error('Failed to add person:', e)
    const errorMessage = e instanceof Error ? e.message : 'Unknown error'
    alert(`Failed to add person: ${errorMessage}`)
  } finally {
    isSubmitting.value = false
  }
}

// Edit person handler - fetch full resource data to get metadata
const openEditDialog = async (person: { id: string; name: string; description?: string }) => {
  try {
    // Fetch full resource data to get metadata (email)
    const fullData = await get<{ id: string; name: string; description?: string; metadata?: string }>(
      `/api/organizations/${getOrgId()}/operations/resources/${person.id}`
    )
    const meta = parseMetadata(fullData.metadata)

    // Get current role assignment for this person
    const personRoles = roleAssignments.value.filter(ra => ra.resourceId === person.id)
    const primaryRole = personRoles.find(ra => ra.isPrimary) || personRoles[0]

    editingPerson.value = {
      id: fullData.id,
      name: fullData.name,
      roleId: primaryRole?.roleId || '',
      roleName: primaryRole?.roleName || '',
      email: meta.email || ''
    }
    editRoleSearchQuery.value = primaryRole?.roleName || ''
    showEditDialog.value = true
  } catch (e) {
    console.error('Failed to load person data:', e)
    // Fallback to basic data if fetch fails
    const personRoles = roleAssignments.value.filter(ra => ra.resourceId === person.id)
    const primaryRole = personRoles.find(ra => ra.isPrimary) || personRoles[0]

    editingPerson.value = {
      id: person.id,
      name: person.name,
      roleId: primaryRole?.roleId || '',
      roleName: primaryRole?.roleName || '',
      email: ''
    }
    editRoleSearchQuery.value = primaryRole?.roleName || ''
    showEditDialog.value = true
  }
}

const handleEditPerson = async () => {
  if (!editingPerson.value || !editingPerson.value.name) return

  isSubmitting.value = true
  try {
    // Update resource basic info
    await put(`/api/organizations/${getOrgId()}/operations/resources/${editingPerson.value.id}`, {
      name: editingPerson.value.name,
      description: undefined, // No longer storing role as free text
      metadata: editingPerson.value.email ? JSON.stringify({ email: editingPerson.value.email }) : undefined,
      status: 1 // Active = 1
    })

    // Handle role assignment changes
    const currentRoleAssignments = roleAssignments.value.filter(ra => ra.resourceId === editingPerson.value?.id)
    const currentPrimaryRole = currentRoleAssignments.find(ra => ra.isPrimary) || currentRoleAssignments[0]

    // If role changed, update the assignment
    if (editingPerson.value.roleId !== currentPrimaryRole?.roleId) {
      // Remove old primary role assignment if exists
      if (currentPrimaryRole) {
        await del(`/api/organizations/${getOrgId()}/operations/resources/role-assignments/${currentPrimaryRole.id}`)
      }

      // Create new role assignment if a role is selected
      if (editingPerson.value.roleId) {
        await createRoleAssignment({
          resourceId: editingPerson.value.id,
          roleId: editingPerson.value.roleId,
          isPrimary: true,
          allocationPercentage: 100
        })
      }
    }

    // Refresh the lists
    await Promise.all([fetchPeople(), fetchRoleAssignments()])

    // Reset and close dialog
    editingPerson.value = null
    editRoleSearchQuery.value = ''
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
    await del(`/api/organizations/${getOrgId()}/operations/resources/${editingPerson.value.id}`)

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
    // Close dropdown first if open, then dialog
    if (showRoleDropdown.value) {
      showRoleDropdown.value = false
    } else if (showEditRoleDropdown.value) {
      showEditRoleDropdown.value = false
    } else {
      closeDialog()
    }
  }
}

// Close dropdown when clicking outside
const closeRoleDropdown = () => {
  // Small delay to allow click on dropdown items to register
  setTimeout(() => {
    showRoleDropdown.value = false
  }, 150)
}

const closeEditRoleDropdown = () => {
  setTimeout(() => {
    showEditRoleDropdown.value = false
  }, 150)
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
            <tr
              v-for="person in people"
              :key="person.id"
              class="hover:bg-white/5 transition-colors cursor-pointer"
              @click="navigateTo(`/app/people/${person.id}`)"
            >
              <td class="px-6 py-4">
                <NuxtLink :to="`/app/people/${person.id}`" class="block hover:text-purple-300 transition-colors">
                  <div class="font-semibold text-white">{{ person.name }}</div>
                  <div class="text-xs text-white/40">
                    <template v-if="getPersonRoles(person.id).length > 0">
                      {{ getPersonRoles(person.id).find(r => r.isPrimary)?.roleName || getPersonRoles(person.id)[0]?.roleName }}
                      <span v-if="getPersonRoles(person.id).length > 1" class="text-white/30">
                        (+{{ getPersonRoles(person.id).length - 1 }} more)
                      </span>
                    </template>
                    <template v-else>No role assigned</template>
                  </div>
                </NuxtLink>
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
                  @click.stop="openEditDialog(person)"
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

          <div class="relative">
            <label class="orbitos-label">Primary Role</label>
            <p class="text-xs text-white/40 mb-2">Assign a role to define this person's responsibilities</p>
            <div class="relative">
              <input
                v-model="roleSearchQuery"
                type="text"
                class="orbitos-input pr-10"
                placeholder="Search or create role..."
                @focus="showRoleDropdown = true"
                @input="showRoleDropdown = true"
                @blur="closeRoleDropdown"
              />
              <button
                v-if="newPerson.roleId"
                type="button"
                @click="clearRole"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-white/40 hover:text-white"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <!-- Selected role badge -->
            <div v-if="newPerson.roleId" class="mt-2">
              <span class="inline-flex items-center gap-1 rounded-full bg-purple-500/20 border border-purple-500/30 px-3 py-1 text-sm text-purple-300">
                {{ newPerson.roleName }}
                <button type="button" @click="clearRole" class="ml-1 hover:text-white">
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </span>
            </div>
            <!-- Dropdown -->
            <div
              v-if="showRoleDropdown && !newPerson.roleId"
              class="absolute z-10 mt-1 w-full rounded-lg border border-white/10 bg-slate-800 shadow-xl max-h-48 overflow-y-auto"
            >
              <!-- Create new option -->
              <button
                v-if="roleSearchQuery.trim() && !roleNameExists"
                type="button"
                @click="createAndSelectRole"
                :disabled="isCreatingRole"
                class="w-full text-left px-4 py-3 hover:bg-white/10 transition-colors border-b border-white/10 flex items-center gap-2"
              >
                <svg class="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                <span class="text-emerald-300">Create "{{ roleSearchQuery.trim() }}"</span>
                <span v-if="isCreatingRole" class="orbitos-spinner orbitos-spinner-sm ml-auto"></span>
              </button>
              <!-- Existing roles -->
              <button
                v-for="role in filteredRoles"
                :key="role.id"
                type="button"
                @click="selectRole(role)"
                class="w-full text-left px-4 py-2 hover:bg-white/10 transition-colors"
              >
                <div class="text-white">{{ role.name }}</div>
                <div v-if="role.department" class="text-xs text-white/40">{{ role.department }}</div>
              </button>
              <!-- No results -->
              <div v-if="filteredRoles.length === 0 && !roleSearchQuery.trim()" class="px-4 py-3 text-white/40 text-sm">
                Type to search or create a role
              </div>
            </div>
          </div>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            @click="showAddDialog = false; newPerson = { name: '', roleId: '', roleName: '', email: '' }; roleSearchQuery = ''"
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

          <div class="relative">
            <label class="orbitos-label">Primary Role</label>
            <p class="text-xs text-white/40 mb-2">Assign a role to define this person's responsibilities</p>
            <div class="relative">
              <input
                v-model="editRoleSearchQuery"
                type="text"
                class="orbitos-input pr-10"
                placeholder="Search or create role..."
                @focus="showEditRoleDropdown = true"
                @input="showEditRoleDropdown = true"
                @blur="closeEditRoleDropdown"
              />
              <button
                v-if="editingPerson.roleId"
                type="button"
                @click="clearEditRole"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-white/40 hover:text-white"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <!-- Selected role badge -->
            <div v-if="editingPerson.roleId" class="mt-2">
              <span class="inline-flex items-center gap-1 rounded-full bg-purple-500/20 border border-purple-500/30 px-3 py-1 text-sm text-purple-300">
                {{ editingPerson.roleName }}
                <button type="button" @click="clearEditRole" class="ml-1 hover:text-white">
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </span>
            </div>
            <!-- Dropdown -->
            <div
              v-if="showEditRoleDropdown && !editingPerson.roleId"
              class="absolute z-10 mt-1 w-full rounded-lg border border-white/10 bg-slate-800 shadow-xl max-h-48 overflow-y-auto"
            >
              <!-- Create new option -->
              <button
                v-if="editRoleSearchQuery.trim() && !editRoleNameExists"
                type="button"
                @click="createAndSelectEditRole"
                :disabled="isCreatingRole"
                class="w-full text-left px-4 py-3 hover:bg-white/10 transition-colors border-b border-white/10 flex items-center gap-2"
              >
                <svg class="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                <span class="text-emerald-300">Create "{{ editRoleSearchQuery.trim() }}"</span>
                <span v-if="isCreatingRole" class="orbitos-spinner orbitos-spinner-sm ml-auto"></span>
              </button>
              <!-- Existing roles -->
              <button
                v-for="role in editFilteredRoles"
                :key="role.id"
                type="button"
                @click="selectEditRole(role)"
                class="w-full text-left px-4 py-2 hover:bg-white/10 transition-colors"
              >
                <div class="text-white">{{ role.name }}</div>
                <div v-if="role.department" class="text-xs text-white/40">{{ role.department }}</div>
              </button>
              <!-- No results -->
              <div v-if="editFilteredRoles.length === 0 && !editRoleSearchQuery.trim()" class="px-4 py-3 text-white/40 text-sm">
                Type to search or create a role
              </div>
            </div>
          </div>

          <!-- Link to detail page for multiple roles -->
          <div class="rounded-lg bg-purple-500/10 border border-purple-500/20 p-3">
            <p class="text-xs text-purple-300">
              Need to assign multiple roles with different allocations?
              <NuxtLink
                :to="`/app/people/${editingPerson.id}`"
                class="underline hover:text-purple-200"
                @click="showEditDialog = false"
              >
                View full profile
              </NuxtLink>
            </p>
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
