<script setup lang="ts">
import type { ProcessWithActivities, OpsActivity, ActivityWithDetails } from '~/types/operations'
import type { ActivityMetadata } from '~/types/activity-metadata'
import { getDefaultMetadata, parseMetadata, stringifyMetadata } from '~/types/activity-metadata'

definePageMeta({
  layout: 'app'
})

const route = useRoute()
const processId = route.params.id as string

const { fetchProcessById, fetchProcesses, processes, functions, fetchFunctions, updateProcess, createActivity, updateActivity, deleteActivity, updateActivityPositions, createEdge, deleteEdge, updateFlowEndpoints } = useOperations()

// Process data from API
const process = ref<ProcessWithActivities | null>(null)
const isLoading = ref(true)
const isSaving = ref(false)

// Edit mode state
const isEditMode = ref(false)
const editingActivityId = ref<string | null>(null)
const editForm = ref({
  name: '',
  description: '',
  duration: '',
  assignedRole: '',
  type: 'manual' as OpsActivity['activityType'],
  linkedProcessId: '' as string,
  metadata: {} as ActivityMetadata
})

// Drag and drop state
const draggedActivityId = ref<string | null>(null)
const dragOverIndex = ref<number | null>(null)

// View mode
const viewMode = ref<'flowchart' | 'swimlane'>('flowchart')
const selectedActivity = ref<OpsActivity | null>(null)

// Show add activity modal
const showAddModal = ref(false)
const newActivity = ref({
  name: '',
  type: 'manual' as OpsActivity['activityType'],
  assignedRole: '',
  duration: '',
  description: '',
  linkedProcessId: '' as string,
  metadata: {} as ActivityMetadata
})

// Show edit process dialog (for trigger/output/purpose)
const showEditProcessDialog = ref(false)
const editProcessForm = ref({
  name: '',
  purpose: '',
  trigger: '',
  output: '',
  functionId: ''
})

// Available processes for subprocess linking (exclude current process)
const availableSubprocesses = computed(() => {
  return processes.value.filter(p => p.id !== processId)
})

// Fetch process and available subprocesses on mount
onMounted(async () => {
  isLoading.value = true
  try {
    // Fetch all processes for subprocess dropdown and functions for function dropdown
    await Promise.all([fetchProcesses(), fetchFunctions()])

    const data = await fetchProcessById(processId)
    if (data) {
      process.value = data
      if (data.activities && data.activities.length > 0) {
        selectedActivity.value = data.activities[0]
      }
    }
  } catch (e) {
    console.error('Failed to fetch process:', e)
  } finally {
    isLoading.value = false
  }
})

// Get sorted activities
const sortedActivities = computed(() => {
  if (!process.value?.activities) return []
  return [...process.value.activities].sort((a, b) => a.order - b.order)
})

// Select activity - works in both view and edit mode
const selectActivity = (activity: OpsActivity) => {
  selectedActivity.value = activity
}

// Activity type colors
const getActivityTypeColor = (type: string) => {
  switch (type) {
    case 'manual': return 'bg-blue-500'
    case 'automated': return 'bg-emerald-500'
    case 'decision': return 'bg-amber-500'
    case 'handoff': return 'bg-purple-500'
    case 'hybrid': return 'bg-cyan-500'
    default: return 'bg-slate-500'
  }
}

// Status color (placeholder - would come from activity data)
const getStatusColor = (status?: string) => {
  switch (status) {
    case 'complete': return 'bg-emerald-500/20 text-emerald-300'
    case 'in_progress': return 'bg-blue-500/20 text-blue-300'
    case 'spof': return 'bg-red-500/20 text-red-300'
    case 'at_risk': return 'bg-amber-500/20 text-amber-300'
    default: return 'bg-slate-500/20 text-slate-300'
  }
}

// Drag and Drop handlers
const handleDragStart = (e: DragEvent, activityId: string) => {
  if (!isEditMode.value) return
  draggedActivityId.value = activityId
  if (e.dataTransfer) {
    e.dataTransfer.effectAllowed = 'move'
  }
}

const handleDragOver = (e: DragEvent, index: number) => {
  if (!isEditMode.value || !draggedActivityId.value) return
  e.preventDefault()
  dragOverIndex.value = index
}

const handleDragLeave = () => {
  dragOverIndex.value = null
}

const handleDrop = async (targetIndex: number) => {
  if (!draggedActivityId.value || !isEditMode.value || !process.value) return

  const activities = [...sortedActivities.value]
  const draggedIndex = activities.findIndex(a => a.id === draggedActivityId.value)

  if (draggedIndex === -1 || draggedIndex === targetIndex) {
    draggedActivityId.value = null
    dragOverIndex.value = null
    return
  }

  // Remove from old position and insert at new position
  const [removed] = activities.splice(draggedIndex, 1)
  activities.splice(targetIndex, 0, removed)

  // Update order numbers locally first for immediate feedback
  activities.forEach((act, idx) => {
    act.order = idx + 1
  })

  if (process.value) {
    process.value.activities = activities
  }

  // Persist to API
  isSaving.value = true
  try {
    // Update each activity's order in the API
    for (const act of activities) {
      await updateActivity(processId, act.id, { order: act.order })
    }
  } catch (e) {
    console.error('Failed to reorder activities:', e)
    // Refresh from API on error
    const data = await fetchProcessById(processId)
    if (data) process.value = data
  } finally {
    isSaving.value = false
  }

  draggedActivityId.value = null
  dragOverIndex.value = null
}

const handleDragEnd = () => {
  draggedActivityId.value = null
  dragOverIndex.value = null
}

// Edit in place handlers
const startEditing = (activity: OpsActivity) => {
  editingActivityId.value = activity.id
  const activityWithDetails = activity as ActivityWithDetails
  editForm.value = {
    name: activity.name,
    description: activity.description || '',
    duration: activity.estimatedDuration?.toString() || '',
    assignedRole: activityWithDetails.assignedResource?.name || '',
    type: activity.activityType,
    linkedProcessId: activity.linkedProcessId || '',
    metadata: activity.metadataJson ? parseMetadata(activity.metadataJson) : {}
  }
}

const saveEdit = async () => {
  if (!editingActivityId.value || !process.value) return

  // Find the current activity to get its order
  const currentActivity = process.value.activities.find(a => a.id === editingActivityId.value)
  if (!currentActivity) return

  isSaving.value = true
  try {
    await updateActivity(processId, editingActivityId.value, {
      name: editForm.value.name,
      description: editForm.value.description || undefined,
      activityType: editForm.value.type,
      estimatedDuration: editForm.value.duration ? parseInt(editForm.value.duration) : undefined,
      // Send null explicitly to clear, or the actual value to set
      linkedProcessId: editForm.value.linkedProcessId || null,
      order: currentActivity.order,
      positionX: currentActivity.positionX,
      positionY: currentActivity.positionY,
      metadataJson: Object.keys(editForm.value.metadata || {}).length > 0
        ? stringifyMetadata(editForm.value.metadata)
        : null
    })

    // Refresh process to get updated linked process data
    const data = await fetchProcessById(processId)
    if (data) {
      process.value = data
    }
  } catch (e) {
    console.error('Failed to update activity:', e)
  } finally {
    isSaving.value = false
    editingActivityId.value = null
  }
}

const cancelEdit = () => {
  editingActivityId.value = null
}

// Delete activity
const handleDeleteActivity = async (activityId: string) => {
  if (!confirm('Are you sure you want to delete this activity?')) return
  if (!process.value) return

  isSaving.value = true
  try {
    await deleteActivity(processId, activityId)

    // Update local state
    if (process.value.activities) {
      process.value.activities = process.value.activities.filter(a => a.id !== activityId)
      // Reorder remaining activities
      process.value.activities.forEach((act, idx) => {
        act.order = idx + 1
      })
    }

    if (selectedActivity.value?.id === activityId) {
      selectedActivity.value = process.value.activities?.[0] || null
    }
  } catch (e) {
    console.error('Failed to delete activity:', e)
  } finally {
    isSaving.value = false
  }
}

// Add new activity
const addNewActivity = async () => {
  if (!newActivity.value.name || !process.value) return

  isSaving.value = true
  try {
    const newOrder = (process.value.activities?.length || 0) + 1

    await createActivity(processId, {
      name: newActivity.value.name,
      description: newActivity.value.description || undefined,
      order: newOrder,
      activityType: newActivity.value.type,
      estimatedDuration: newActivity.value.duration ? parseInt(newActivity.value.duration) : undefined,
      linkedProcessId: newActivity.value.linkedProcessId || undefined,
      metadataJson: Object.keys(newActivity.value.metadata || {}).length > 0
        ? stringifyMetadata(newActivity.value.metadata)
        : undefined
    })

    // Close modal and reset form FIRST (before data refresh to avoid race condition)
    showAddModal.value = false
    newActivity.value = {
      name: '',
      type: 'manual',
      assignedRole: '',
      duration: '',
      description: '',
      linkedProcessId: '',
      metadata: {}
    }

    // Wait for next tick to let the modal fully unmount
    await nextTick()

    // Then refresh process to get new activity with ID
    const data = await fetchProcessById(processId)
    if (data) {
      console.log('[Process Page] Fetched process with', data.activities?.length || 0, 'activities')
      process.value = data
    }
  } catch (e) {
    console.error('Failed to add activity:', e)
  } finally {
    isSaving.value = false
  }
}

// Handle Enter key in add modal
const handleAddFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && newActivity.value.name && !isSaving.value) {
    e.preventDefault()
    addNewActivity()
  }
  if (e.key === 'Escape') {
    showAddModal.value = false
  }
}

// Toggle edit mode
const toggleEditMode = () => {
  isEditMode.value = !isEditMode.value
  if (!isEditMode.value) {
    editingActivityId.value = null
  }
}

// Open edit process dialog (for trigger/output)
const openEditProcessDialog = () => {
  if (!process.value) return
  editProcessForm.value = {
    name: process.value.name,
    purpose: process.value.purpose || '',
    trigger: process.value.trigger || '',
    output: process.value.output || '',
    functionId: process.value.functionId || ''
  }
  showEditProcessDialog.value = true
}

// Save process details
const saveProcessDetails = async () => {
  if (!process.value || !editProcessForm.value.name) return

  isSaving.value = true
  try {
    await updateProcess(processId, {
      name: editProcessForm.value.name,
      purpose: editProcessForm.value.purpose || undefined,
      trigger: editProcessForm.value.trigger || undefined,
      output: editProcessForm.value.output || undefined,
      functionId: editProcessForm.value.functionId || undefined
    })

    // Update local state
    process.value.name = editProcessForm.value.name
    process.value.purpose = editProcessForm.value.purpose || undefined
    process.value.trigger = editProcessForm.value.trigger || undefined
    process.value.output = editProcessForm.value.output || undefined
    process.value.functionId = editProcessForm.value.functionId || undefined

    showEditProcessDialog.value = false
  } catch (e) {
    console.error('Failed to update process:', e)
  } finally {
    isSaving.value = false
  }
}

// Vue Flow event handlers
const handlePositionsChanged = async (positions: Array<{ activityId: string; positionX: number; positionY: number }>) => {
  if (!process.value) return

  try {
    await updateActivityPositions(processId, positions)
    // Update local state
    for (const pos of positions) {
      const activity = process.value.activities.find(a => a.id === pos.activityId)
      if (activity) {
        activity.positionX = pos.positionX
        activity.positionY = pos.positionY
      }
    }
  } catch (e) {
    console.error('Failed to update positions:', e)
  }
}

const handleEdgeCreated = async (edge: { sourceActivityId: string; targetActivityId: string; sourceHandle?: string }) => {
  if (!process.value) return

  // Handle connections from Start node
  if (edge.sourceActivityId === 'start') {
    try {
      await updateFlowEndpoints(processId, { entryActivityId: edge.targetActivityId })
      process.value.entryActivityId = edge.targetActivityId
      process.value.useExplicitFlow = true // Mark as explicit mode
      console.log('[Process Page] Set entry activity:', edge.targetActivityId)
    } catch (e) {
      console.error('Failed to set entry activity:', e)
    }
    return
  }

  // Handle connections to End node
  if (edge.targetActivityId === 'end') {
    try {
      await updateFlowEndpoints(processId, { exitActivityId: edge.sourceActivityId })
      process.value.exitActivityId = edge.sourceActivityId
      process.value.useExplicitFlow = true // Mark as explicit mode
      console.log('[Process Page] Set exit activity:', edge.sourceActivityId)
    } catch (e) {
      console.error('Failed to set exit activity:', e)
    }
    return
  }

  // Regular edge between activities
  try {
    const newEdge = await createEdge(processId, {
      sourceActivityId: edge.sourceActivityId,
      targetActivityId: edge.targetActivityId,
      sourceHandle: edge.sourceHandle,
      edgeType: 'smoothstep',
      animated: false
    })
    if (newEdge) {
      process.value.edges = [...(process.value.edges || []), newEdge]
      process.value.useExplicitFlow = true // Mark as explicit mode
    }
  } catch (e) {
    console.error('Failed to create edge:', e)
  }
}

const handleEdgeDeleted = async (edgeId: string) => {
  if (!process.value) return

  try {
    await deleteEdge(processId, edgeId)
    process.value.edges = (process.value.edges || []).filter(e => e.id !== edgeId)
    process.value.useExplicitFlow = true // Keep in explicit mode after deletion
  } catch (e) {
    console.error('Failed to delete edge:', e)
  }
}

// Handle deletion of implicit edges (edges generated from activity order, not stored in DB)
// When user deletes an implicit edge, we need to switch to explicit flow mode
const handleImplicitEdgeDeleted = async (edge: { sourceActivityId: string; targetActivityId: string }) => {
  if (!process.value) return

  console.log('[ProcessEditor] Implicit edge deleted, switching to explicit mode:', edge)

  isSaving.value = true
  try {
    const sortedActivities = [...process.value.activities].sort((a, b) => a.order - b.order)
    if (sortedActivities.length === 0) return

    // Determine which entry/exit to set
    const firstActivity = sortedActivities[0]
    const lastActivity = sortedActivities[sortedActivities.length - 1]

    // Check if this is a Start or End edge being deleted
    // Start edge connects 'start' node to the entry activity
    // End edge connects the exit activity to 'end' node
    const isStartEdgeDeleted = edge.sourceActivityId === 'start'
    const isEndEdgeDeleted = edge.targetActivityId === 'end'

    console.log('[ProcessEditor] isStartEdgeDeleted:', isStartEdgeDeleted, 'isEndEdgeDeleted:', isEndEdgeDeleted)

    // First, set the flow endpoints to switch to explicit mode
    // Use clearEntry/clearExit flags to properly clear the IDs in the database
    await updateFlowEndpoints(processId, {
      entryActivityId: isStartEdgeDeleted ? undefined : (process.value.entryActivityId || firstActivity.id),
      exitActivityId: isEndEdgeDeleted ? undefined : (process.value.exitActivityId || lastActivity.id),
      clearEntry: isStartEdgeDeleted,
      clearExit: isEndEdgeDeleted,
    })

    // Update local state immediately
    if (isStartEdgeDeleted) {
      process.value.entryActivityId = undefined
    }
    if (isEndEdgeDeleted) {
      process.value.exitActivityId = undefined
    }
    process.value.useExplicitFlow = true

    // Check if this is an activity-to-activity edge (not start/end)
    const isActivityToActivityEdge = edge.sourceActivityId !== 'start' && edge.targetActivityId !== 'end'

    if (isActivityToActivityEdge) {
      // Create all activity-to-activity edges EXCEPT the deleted one
      const createdEdges: typeof process.value.edges = []
      for (let i = 0; i < sortedActivities.length - 1; i++) {
        const current = sortedActivities[i]
        const next = sortedActivities[i + 1]

        // Skip if this is the edge being deleted
        if (edge.sourceActivityId === current.id && edge.targetActivityId === next.id) {
          console.log('[ProcessEditor] Skipping deleted edge:', current.id, '->', next.id)
          continue
        }

        // Create this edge in the database
        try {
          const createdEdge = await createEdge(processId, {
            sourceActivityId: current.id,
            targetActivityId: next.id,
          })
          createdEdges.push(createdEdge)
        } catch (e) {
          console.error('Failed to create edge:', e)
        }
      }
    }

    // Refresh from API to get consistent state
    const data = await fetchProcessById(processId)
    if (data) {
      console.log('[ProcessEditor] Refreshed process after implicit edge deletion, useExplicitFlow:', data.useExplicitFlow, 'entryActivityId:', data.entryActivityId, 'exitActivityId:', data.exitActivityId)
      process.value = data
    }
  } catch (e) {
    console.error('Failed to switch to explicit mode:', e)
  } finally {
    isSaving.value = false
  }
}

const handleNodeDoubleClick = (activityId: string) => {
  const activity = process.value?.activities.find(a => a.id === activityId)
  if (activity) {
    startEditing(activity)
  }
}

// Get unique roles from activities
const uniqueRoles = computed(() => {
  if (!process.value?.activities) return []
  const roles = process.value.activities
    .map(a => a.assignedResource?.name)
    .filter((r): r is string => !!r)
  return [...new Set(roles)]
})
</script>

<template>
  <div class="flex h-[calc(100vh-8rem)] flex-col">
    <!-- Loading State -->
    <div v-if="isLoading" class="flex-1 flex items-center justify-center">
      <div class="orbitos-spinner orbitos-spinner-lg"></div>
    </div>

    <!-- Not Found -->
    <div v-else-if="!process" class="flex-1 flex items-center justify-center">
      <div class="text-center">
        <div class="orbitos-text mb-4">Process not found</div>
        <NuxtLink to="/app/processes" class="orbitos-btn-secondary">
          Back to Processes
        </NuxtLink>
      </div>
    </div>

    <!-- Process Content -->
    <template v-else>
      <!-- Header -->
      <div class="shrink-0 mb-4 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-4">
          <NuxtLink
            to="/app/processes"
            class="flex items-center gap-2 text-white/40 hover:text-white transition-colors"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
            <span>Back</span>
          </NuxtLink>
          <div class="h-6 w-px bg-white/20" />
          <div>
            <div class="flex items-center gap-3">
              <h1 class="text-xl font-bold text-white">{{ process.name }}</h1>
              <button
                type="button"
                class="p-1 text-white/40 hover:text-white transition-colors"
                title="Edit process details"
                @click="openEditProcessDialog"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                </svg>
              </button>
              <span
                :class="[
                  'rounded-full px-3 py-1 text-xs font-medium',
                  process.status === 'active'
                    ? 'bg-emerald-500/20 text-emerald-300'
                    : process.status === 'deprecated'
                      ? 'bg-red-500/20 text-red-300'
                      : 'bg-amber-500/20 text-amber-300'
                ]"
              >
                {{ process.status === 'active' ? 'Active' : process.status === 'deprecated' ? 'Deprecated' : 'Draft' }}
              </span>
              <span v-if="isEditMode" class="rounded-full bg-purple-500/30 px-3 py-1 text-xs font-medium text-purple-300 animate-pulse">
                Edit Mode
              </span>
              <span v-if="isSaving" class="flex items-center gap-1 text-xs text-white/40">
                <div class="orbitos-spinner orbitos-spinner-xs"></div>
                Saving...
              </span>
            </div>
            <p v-if="process.purpose" class="text-sm text-white/60">{{ process.purpose }}</p>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <!-- View Toggle -->
          <div class="flex rounded-lg bg-white/5 p-1">
            <button
              type="button"
              :class="[
                'rounded-md px-3 py-1.5 text-sm transition-colors',
                viewMode === 'flowchart' ? 'bg-purple-500/30 text-purple-300' : 'text-white/40 hover:text-white'
              ]"
              @click="viewMode = 'flowchart'"
            >
              Flowchart
            </button>
            <button
              type="button"
              :class="[
                'rounded-md px-3 py-1.5 text-sm transition-colors',
                viewMode === 'swimlane' ? 'bg-purple-500/30 text-purple-300' : 'text-white/40 hover:text-white'
              ]"
              @click="viewMode = 'swimlane'"
            >
              Swimlane
            </button>
          </div>

          <NuxtLink
            to="/app/processes/guide"
            class="orbitos-btn-secondary py-2 px-4 text-sm inline-flex items-center gap-2"
            title="Symbol guide and best practices"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
            </svg>
            Guide
          </NuxtLink>
          <NuxtLink
            :to="`/app/processes/${processId}/documentation`"
            class="orbitos-btn-secondary py-2 px-4 text-sm inline-flex items-center gap-2"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            Export
          </NuxtLink>
          <button
            type="button"
            :class="[
              'py-2 px-4 text-sm font-semibold rounded-xl transition-all',
              isEditMode
                ? 'bg-emerald-500 text-white hover:bg-emerald-600'
                : 'orbitos-btn-primary'
            ]"
            @click="toggleEditMode"
          >
            {{ isEditMode ? 'Done Editing' : 'Edit Process' }}
          </button>
        </div>
      </div>

      <!-- Main Content -->
      <div class="flex flex-1 gap-4 overflow-hidden min-h-0">
        <!-- Canvas Area -->
        <div class="flex-1 orbitos-glass-subtle overflow-hidden min-h-0">
          <!-- Flowchart View with Vue Flow -->
          <div v-if="viewMode === 'flowchart'" class="h-full relative min-h-0">
            <!-- Empty State -->
            <div v-if="sortedActivities.length === 0" class="h-full flex flex-col items-center justify-center">
              <div class="rounded-xl border-2 border-dashed border-white/20 bg-white/5 p-8 text-center max-w-sm">
                <svg class="h-12 w-12 mx-auto mb-4 text-white/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
                </svg>
                <h4 class="text-lg font-semibold text-white mb-2">No activities yet</h4>
                <p class="text-sm text-white/50 mb-4">Add activities to define the steps in this process</p>
                <button
                  type="button"
                  class="orbitos-btn-primary px-6 py-2"
                  @click="showAddModal = true"
                >
                  <svg class="h-4 w-4 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                  </svg>
                  Add First Activity
                </button>
              </div>
            </div>

            <!-- Vue Flow Canvas -->
            <ProcessFlowCanvas
              v-else
              :process="process"
              :is-edit-mode="isEditMode"
              @node-click="(id: string) => { const act = process?.activities.find(a => a.id === id); if (act) selectActivity(act); }"
              @node-double-click="handleNodeDoubleClick"
              @positions-changed="handlePositionsChanged"
              @edge-created="handleEdgeCreated"
              @edge-deleted="handleEdgeDeleted"
              @implicit-edge-deleted="handleImplicitEdgeDeleted"
            />

            <!-- Add Activity Button (floating) - shown in edit mode -->
            <div v-if="isEditMode && sortedActivities.length > 0" class="absolute bottom-4 right-4 z-10">
              <button
                type="button"
                class="flex items-center gap-2 rounded-lg bg-purple-500 px-4 py-2 text-sm font-medium text-white shadow-lg shadow-purple-500/30 hover:bg-purple-600 transition-colors"
                @click="showAddModal = true"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                Add Activity
              </button>
            </div>
          </div>

          <!-- Swimlane View -->
          <div v-else class="h-full overflow-auto">
            <div class="min-w-[800px] p-4">
              <div class="space-y-0">
                <template v-for="role in uniqueRoles" :key="role">
                  <div class="flex border-b border-white/10">
                    <div class="w-40 flex-shrink-0 border-r border-white/10 bg-white/5 p-4">
                      <div class="flex flex-col items-center justify-center h-full text-center">
                        <div class="h-10 w-10 rounded-full bg-gradient-to-br from-purple-500 to-blue-600 flex items-center justify-center mb-2">
                          <svg class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                          </svg>
                        </div>
                        <span class="text-sm font-medium text-white">{{ role }}</span>
                      </div>
                    </div>
                    <div class="flex-1 flex items-center gap-4 p-4 min-h-[120px]">
                      <template v-for="(activity, idx) in sortedActivities.filter(a => a.assignedResource?.name === role)" :key="activity.id">
                        <div
                          :class="[
                            'cursor-pointer rounded-lg border p-3 transition-all min-w-[150px]',
                            selectedActivity?.id === activity.id
                              ? 'border-purple-500 bg-white/10'
                              : 'border-white/10 bg-white/5 hover:border-white/20'
                          ]"
                          @click="selectActivity(activity)"
                        >
                          <div class="text-sm font-medium text-white">{{ activity.name }}</div>
                          <div v-if="activity.estimatedDuration" class="mt-1 text-xs text-white/40">{{ activity.estimatedDuration }} min</div>
                        </div>
                        <svg v-if="idx < sortedActivities.filter(a => a.assignedResource?.name === role).length - 1" class="h-4 w-4 text-white/20 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                        </svg>
                      </template>
                    </div>
                  </div>
                </template>
              </div>
            </div>
          </div>
        </div>

        <!-- Properties Panel -->
        <div class="w-80 flex-shrink-0 orbitos-glass-subtle overflow-y-auto">
          <div class="p-4">
            <h3 class="text-lg font-semibold text-white mb-4">Properties</h3>

            <!-- Edit Form -->
            <div v-if="editingActivityId" class="space-y-4">
              <div class="rounded-xl border border-purple-500/30 bg-purple-500/10 p-4">
                <h4 class="text-sm font-medium text-purple-300 mb-3">Editing Activity</h4>
                <div class="space-y-3">
                  <div>
                    <label class="orbitos-label">Name</label>
                    <input
                      v-model="editForm.name"
                      type="text"
                      class="orbitos-input"
                    />
                  </div>
                  <div>
                    <label class="orbitos-label">Type</label>
                    <select v-model="editForm.type" class="orbitos-input">
                      <optgroup label="Basic Activities">
                        <option value="manual">Manual</option>
                        <option value="automated">Automated</option>
                        <option value="hybrid">Hybrid</option>
                        <option value="handoff">Handoff</option>
                        <option value="decision">Decision</option>
                      </optgroup>
                      <optgroup label="IE Symbols (ASME)">
                        <option value="operation">Operation (Value-Add)</option>
                        <option value="inspection">Inspection (Quality Check)</option>
                        <option value="transport">Transport (Movement)</option>
                        <option value="delay">Delay (Wait/Queue)</option>
                        <option value="storage">Storage</option>
                        <option value="document">Document</option>
                        <option value="database">Database</option>
                        <option value="manualInput">Manual Input (Data Entry)</option>
                        <option value="display">Display (Output)</option>
                      </optgroup>
                    </select>
                  </div>
                  <div>
                    <label class="orbitos-label">Description</label>
                    <textarea v-model="editForm.description" rows="3" class="orbitos-input" />
                  </div>
                  <div>
                    <label class="orbitos-label">Duration (minutes)</label>
                    <input v-model="editForm.duration" type="number" class="orbitos-input" />
                  </div>
                  <div>
                    <label class="orbitos-label">Link to Subprocess</label>
                    <select v-model="editForm.linkedProcessId" class="orbitos-input">
                      <option value="">None (no subprocess)</option>
                      <option v-for="p in availableSubprocesses" :key="p.id" :value="p.id">
                        {{ p.name }}
                      </option>
                    </select>
                    <p class="text-xs text-white/40 mt-1">Link this activity to another process for drill-down</p>
                  </div>

                  <!-- Type-specific metadata fields for IE symbols -->
                  <ActivityMetadataFields
                    v-model="editForm.metadata"
                    :activity-type="editForm.type"
                  />

                  <div class="flex gap-2 pt-2">
                    <button
                      type="button"
                      class="flex-1 orbitos-btn-primary py-2"
                      :disabled="isSaving"
                      @click="saveEdit"
                    >
                      Save
                    </button>
                    <button
                      type="button"
                      class="flex-1 orbitos-btn-secondary py-2"
                      @click="cancelEdit"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <div v-else-if="selectedActivity" class="space-y-4">
              <!-- Selected Activity Info -->
              <div class="rounded-xl border border-white/10 bg-white/5 p-4">
                <div class="flex items-center gap-3 mb-3">
                  <div :class="['h-10 w-10 rounded-lg flex items-center justify-center', getActivityTypeColor(selectedActivity.activityType)]">
                    <svg v-if="selectedActivity.activityType === 'manual'" class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                    </svg>
                    <svg v-else-if="selectedActivity.activityType === 'automated'" class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
                    </svg>
                    <svg v-else-if="selectedActivity.activityType === 'decision'" class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <svg v-else class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
                    </svg>
                  </div>
                  <div>
                    <div class="font-medium text-white">{{ selectedActivity.activityType.charAt(0).toUpperCase() + selectedActivity.activityType.slice(1) }} Activity</div>
                    <div class="text-xs text-white/40">Step {{ selectedActivity.order }}</div>
                  </div>
                </div>

                <div class="space-y-3">
                  <div>
                    <label class="text-xs text-white/40 block mb-1">Name</label>
                    <div class="text-sm text-white">{{ selectedActivity.name }}</div>
                  </div>
                  <div v-if="selectedActivity.description">
                    <label class="text-xs text-white/40 block mb-1">Description</label>
                    <div class="text-sm text-white/70">{{ selectedActivity.description }}</div>
                  </div>
                  <div class="grid grid-cols-2 gap-3">
                    <div v-if="selectedActivity.assignedResource">
                      <label class="text-xs text-white/40 block mb-1">Assigned To</label>
                      <div class="text-sm text-white">{{ selectedActivity.assignedResource.name }}</div>
                    </div>
                    <div v-if="selectedActivity.estimatedDuration">
                      <label class="text-xs text-white/40 block mb-1">Duration</label>
                      <div class="text-sm text-white">{{ selectedActivity.estimatedDuration }} min</div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Linked Function -->
              <div v-if="selectedActivity.function" class="rounded-xl border border-purple-500/30 bg-purple-500/10 p-4">
                <div class="flex items-center gap-2 mb-2">
                  <svg class="h-4 w-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
                  </svg>
                  <span class="text-sm font-medium text-purple-300">Linked Function</span>
                </div>
                <div class="text-white font-medium">{{ selectedActivity.function.name }}</div>
                <NuxtLink
                  :to="`/app/functions/${selectedActivity.function.id}`"
                  class="mt-2 inline-flex items-center gap-1 text-xs text-purple-400 hover:text-purple-300"
                >
                  View function details
                  <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                  </svg>
                </NuxtLink>
              </div>

              <!-- Linked Subprocess Portal -->
              <div v-if="(selectedActivity as ActivityWithDetails).linkedProcess" class="rounded-xl border border-cyan-500/30 bg-gradient-to-br from-cyan-500/20 to-cyan-600/10 p-4 space-y-3">
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <svg class="h-5 w-5 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 5l7 7-7 7M5 5l7 7-7 7" />
                    </svg>
                    <span class="text-sm font-semibold text-cyan-300">Subprocess Portal</span>
                  </div>
                  <span class="text-xs bg-cyan-500/20 text-cyan-300 px-2 py-0.5 rounded-full">
                    {{ (selectedActivity as ActivityWithDetails).linkedProcess?.activityCount || 0 }} activities
                  </span>
                </div>

                <!-- Subprocess Name -->
                <div class="text-white font-semibold text-lg">
                  {{ (selectedActivity as ActivityWithDetails).linkedProcess?.name }}
                </div>

                <!-- Subprocess Purpose -->
                <div v-if="(selectedActivity as ActivityWithDetails).linkedProcess?.purpose" class="text-white/70 text-sm">
                  {{ (selectedActivity as ActivityWithDetails).linkedProcess?.purpose }}
                </div>

                <!-- Subprocess Trigger & Output (Start/End) -->
                <div class="grid grid-cols-2 gap-3 mt-3">
                  <!-- Start/Trigger -->
                  <div class="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-2">
                    <div class="flex items-center gap-1 mb-1">
                      <div class="w-2 h-2 rounded-full bg-emerald-400"></div>
                      <span class="text-xs font-medium text-emerald-300">Start</span>
                    </div>
                    <div class="text-xs text-white/60">
                      {{ (selectedActivity as ActivityWithDetails).linkedProcess?.trigger || 'No trigger defined' }}
                    </div>
                  </div>

                  <!-- End/Output -->
                  <div class="rounded-lg border border-red-500/30 bg-red-500/10 p-2">
                    <div class="flex items-center gap-1 mb-1">
                      <div class="w-2 h-2 rounded-full bg-red-400"></div>
                      <span class="text-xs font-medium text-red-300">End</span>
                    </div>
                    <div class="text-xs text-white/60">
                      {{ (selectedActivity as ActivityWithDetails).linkedProcess?.output || 'No output defined' }}
                    </div>
                  </div>
                </div>

                <!-- Drill Down Link -->
                <NuxtLink
                  :to="`/app/processes/${selectedActivity.linkedProcessId}`"
                  class="mt-2 w-full inline-flex items-center justify-center gap-2 text-sm font-medium text-cyan-300 hover:text-cyan-200 bg-cyan-500/20 hover:bg-cyan-500/30 rounded-lg px-3 py-2 transition-colors"
                >
                  Open subprocess
                  <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                  </svg>
                </NuxtLink>
              </div>
            </div>

            <div v-else class="text-center py-8 text-white/40">
              <svg class="h-12 w-12 mx-auto mb-3 text-white/20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122" />
              </svg>
              <p class="text-sm">Click an activity to view details</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Bottom Stats -->
      <div class="shrink-0 mt-4 flex items-center justify-between text-sm text-white/40">
        <div class="flex items-center gap-6">
          <span class="flex items-center gap-2">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
            </svg>
            {{ sortedActivities.length }} activities
          </span>
          <span class="flex items-center gap-2">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
            </svg>
            {{ sortedActivities.filter(a => a.function).length }} linked functions
          </span>
          <span class="flex items-center gap-2">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            {{ uniqueRoles.length }} roles
          </span>
        </div>
        <div v-if="process.owner">
          Owner: {{ process.owner.name }}
        </div>
      </div>
    </template>

    <!-- Add Activity Modal -->
    <BaseDialog
      v-model="showAddModal"
      size="lg"
      title="Add New Activity"
      @submit="addNewActivity"
    >
      <div class="space-y-4">
        <div>
          <label class="orbitos-label">Activity Name *</label>
          <input
            v-model="newActivity.name"
            type="text"
            placeholder="e.g., Review Contract"
            class="orbitos-input"
            autofocus
          />
        </div>

        <div>
          <label class="orbitos-label">Type</label>
          <select v-model="newActivity.type" class="orbitos-input">
            <optgroup label="Basic Activities">
              <option value="manual">Manual</option>
              <option value="automated">Automated</option>
              <option value="hybrid">Hybrid</option>
              <option value="handoff">Handoff</option>
              <option value="decision">Decision</option>
            </optgroup>
            <optgroup label="IE Symbols (ASME)">
              <option value="operation">Operation (Value-Add)</option>
              <option value="inspection">Inspection (Quality Check)</option>
              <option value="transport">Transport (Movement)</option>
              <option value="delay">Delay (Wait/Queue)</option>
              <option value="storage">Storage</option>
              <option value="document">Document</option>
              <option value="database">Database</option>
              <option value="manualInput">Manual Input (Data Entry)</option>
              <option value="display">Display (Output)</option>
            </optgroup>
          </select>
        </div>

        <div>
          <label class="orbitos-label">Duration (minutes)</label>
          <input
            v-model="newActivity.duration"
            type="number"
            placeholder="e.g., 30"
            class="orbitos-input"
          />
        </div>

        <div>
          <label class="orbitos-label">Description</label>
          <textarea
            v-model="newActivity.description"
            rows="3"
            placeholder="What does this activity involve?"
            class="orbitos-input"
          />
        </div>

        <div>
          <label class="orbitos-label">Link to Subprocess (optional)</label>
          <select v-model="newActivity.linkedProcessId" class="orbitos-input">
            <option value="">None (no subprocess)</option>
            <option v-for="p in availableSubprocesses" :key="p.id" :value="p.id">
              {{ p.name }}
            </option>
          </select>
          <p class="text-xs text-white/40 mt-1">Enable drill-down to another process</p>
        </div>

        <!-- Type-specific metadata fields for IE symbols -->
        <ActivityMetadataFields
          v-model="newActivity.metadata"
          :activity-type="newActivity.type"
        />
      </div>

      <template #footer="{ close }">
        <div class="flex gap-3">
          <button
            type="button"
            class="flex-1 orbitos-btn-secondary"
            @click="close"
          >
            Cancel
          </button>
          <button
            type="button"
            class="flex-1 orbitos-btn-primary"
            :disabled="!newActivity.name || isSaving"
            @click="addNewActivity"
          >
            <span v-if="isSaving" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Adding...
            </span>
            <span v-else>Add Activity</span>
          </button>
        </div>
      </template>
    </BaseDialog>

    <!-- Edit Process Dialog -->
    <BaseDialog
      v-model="showEditProcessDialog"
      size="lg"
      title="Edit Process Details"
      subtitle="Configure the process trigger (Start) and output (End) conditions"
      @submit="saveProcessDetails"
    >
      <div class="space-y-4">
        <div>
          <label class="orbitos-label">Process Name *</label>
          <input
            v-model="editProcessForm.name"
            type="text"
            placeholder="e.g., Customer Onboarding"
            class="orbitos-input"
            autofocus
          />
        </div>

        <div>
          <label class="orbitos-label">Purpose</label>
          <input
            v-model="editProcessForm.purpose"
            type="text"
            placeholder="What is the goal of this process?"
            class="orbitos-input"
          />
        </div>

        <div>
          <label class="orbitos-label">Function</label>
          <select v-model="editProcessForm.functionId" class="orbitos-input">
            <option value="">Select a function (optional)</option>
            <option v-for="func in functions" :key="func.id" :value="func.id">
              {{ func.name }}
            </option>
          </select>
          <p class="text-xs text-white/40 mt-1">Associate this process with a business function</p>
        </div>

        <div class="p-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10">
          <div class="flex items-center gap-2 mb-3">
            <div class="h-6 w-6 rounded-full bg-emerald-500/30 flex items-center justify-center">
              <svg class="h-3 w-3 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <span class="text-sm font-medium text-emerald-300">Start Node - Trigger Condition</span>
          </div>
          <label class="orbitos-label text-emerald-200/70">When does this process start?</label>
          <input
            v-model="editProcessForm.trigger"
            type="text"
            placeholder="e.g., New customer signs up, Order received, Request submitted"
            class="orbitos-input bg-emerald-900/20 border-emerald-500/30 focus:border-emerald-500"
          />
        </div>

        <div class="p-4 rounded-xl border border-red-500/30 bg-red-500/10">
          <div class="flex items-center gap-2 mb-3">
            <div class="h-6 w-6 rounded-full bg-red-500/30 flex items-center justify-center">
              <svg class="h-3 w-3 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 10a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
              </svg>
            </div>
            <span class="text-sm font-medium text-red-300">End Node - Output/Completion</span>
          </div>
          <label class="orbitos-label text-red-200/70">What is the end result of this process?</label>
          <input
            v-model="editProcessForm.output"
            type="text"
            placeholder="e.g., Customer fully onboarded, Order shipped, Request fulfilled"
            class="orbitos-input bg-red-900/20 border-red-500/30 focus:border-red-500"
          />
        </div>
      </div>

      <template #footer="{ close }">
        <div class="flex gap-3">
          <button
            type="button"
            class="flex-1 orbitos-btn-secondary"
            @click="close"
          >
            Cancel
          </button>
          <button
            type="button"
            class="flex-1 orbitos-btn-primary"
            :disabled="!editProcessForm.name || isSaving"
            @click="saveProcessDetails"
          >
            <span v-if="isSaving" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Saving...
            </span>
            <span v-else>Save Changes</span>
          </button>
        </div>
      </template>
    </BaseDialog>
  </div>
</template>
