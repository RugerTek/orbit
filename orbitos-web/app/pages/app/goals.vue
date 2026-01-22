<script setup lang="ts">
import type { OpsGoal, GoalWithProgress, OpsResource } from '~/types/operations'

definePageMeta({
  layout: 'app'
})

const {
  goals,
  people,
  fetchGoals,
  fetchResources,
  createGoal,
  updateGoal,
  deleteGoal,
  isLoading
} = useOperations()

// Fetch data on mount
onMounted(async () => {
  await Promise.all([fetchGoals(), fetchResources()])
})

// Quarter filter
const currentDate = new Date()
const currentYear = currentDate.getFullYear()
const currentQuarter = Math.ceil((currentDate.getMonth() + 1) / 3)

// Generate quarter options (current year and next year)
const quarterOptions = computed(() => {
  const options: { label: string; value: string; startDate: Date; endDate: Date }[] = []

  for (let year = currentYear - 1; year <= currentYear + 1; year++) {
    for (let q = 1; q <= 4; q++) {
      const startMonth = (q - 1) * 3
      const endMonth = startMonth + 2
      options.push({
        label: `Q${q} ${year}`,
        value: `${year}-Q${q}`,
        startDate: new Date(year, startMonth, 1),
        endDate: new Date(year, endMonth + 1, 0) // Last day of the quarter
      })
    }
  }

  return options
})

const selectedQuarter = ref(`${currentYear}-Q${currentQuarter}`)
const showQuarterDropdown = ref(false)

const selectedQuarterLabel = computed(() => {
  const option = quarterOptions.value.find(o => o.value === selectedQuarter.value)
  return option?.label || 'All Time'
})

// Filter goals by selected quarter
const filteredGoals = computed(() => {
  if (selectedQuarter.value === 'all') {
    return goals.value
  }

  const option = quarterOptions.value.find(o => o.value === selectedQuarter.value)
  if (!option) return goals.value

  return goals.value.filter(goal => {
    // If goal has dates, check if it overlaps with the quarter
    if (goal.startDate || goal.endDate) {
      const goalStart = goal.startDate ? new Date(goal.startDate) : new Date(0)
      const goalEnd = goal.endDate ? new Date(goal.endDate) : new Date(9999, 11, 31)

      // Check if the goal's timeframe overlaps with the quarter
      return goalStart <= option.endDate && goalEnd >= option.startDate
    }

    // If no dates, include by default (or could exclude)
    return true
  })
})

// Dialog state
const showAddObjectiveDialog = ref(false)
const showEditObjectiveDialog = ref(false)
const showAddKeyResultDialog = ref(false)
const showEditKeyResultDialog = ref(false)
const isSubmitting = ref(false)

// Form state for new objective
const newObjective = ref({
  name: '',
  description: '',
  startDate: '',
  endDate: '',
  ownerId: ''
})

// Form state for editing objective
const editingObjective = ref<{
  id: string
  name: string
  description: string
  startDate: string
  endDate: string
  ownerId: string
} | null>(null)

// Form state for new key result
const newKeyResult = ref({
  name: '',
  description: '',
  targetValue: 100,
  currentValue: 0,
  parentGoalId: ''
})

// Form state for editing key result
const editingKeyResult = ref<{
  id: string
  name: string
  description: string
  targetValue: number
  currentValue: number
  parentGoalId: string
} | null>(null)

// Selected/expanded goal
const selectedGoal = ref<string | null>(null)

const toggleGoal = (id: string) => {
  selectedGoal.value = selectedGoal.value === id ? null : id
}

// Progress color helpers
const getProgressColor = (progress: number) => {
  if (progress >= 80) return 'bg-emerald-500'
  if (progress >= 50) return 'bg-blue-500'
  if (progress >= 30) return 'bg-amber-500'
  return 'bg-red-500'
}

const getProgressTextColor = (progress: number) => {
  if (progress >= 80) return 'text-emerald-300'
  if (progress >= 50) return 'text-blue-300'
  if (progress >= 30) return 'text-amber-300'
  return 'text-red-300'
}

// Stats (based on filtered goals)
const overallProgress = computed(() => {
  if (filteredGoals.value.length === 0) return 0
  return Math.round(filteredGoals.value.reduce((acc, g) => acc + g.progress, 0) / filteredGoals.value.length)
})

const totalKeyResults = computed(() => {
  return filteredGoals.value.reduce((acc, g) => acc + (g.keyResults?.length || 0), 0)
})

const atRiskCount = computed(() => {
  return filteredGoals.value.filter(g => g.progress < 50).length
})

const onTrackCount = computed(() => {
  return filteredGoals.value.filter(g => g.progress >= 70).length
})

// CRUD Operations

// Create Objective
const handleAddObjective = async () => {
  if (!newObjective.value.name) return

  isSubmitting.value = true
  try {
    await createGoal({
      name: newObjective.value.name,
      description: newObjective.value.description || undefined,
      goalType: 'objective',
      startDate: newObjective.value.startDate || undefined,
      endDate: newObjective.value.endDate || undefined,
      ownerId: newObjective.value.ownerId || undefined,
      status: 'active'
    })

    // Reset form and close dialog
    newObjective.value = { name: '', description: '', startDate: '', endDate: '', ownerId: '' }
    showAddObjectiveDialog.value = false

    // Refresh goals to get updated data
    await fetchGoals()
  } catch (e) {
    console.error('Failed to add objective:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Open Edit Objective Dialog
const openEditObjectiveDialog = (goal: GoalWithProgress) => {
  editingObjective.value = {
    id: goal.id,
    name: goal.name,
    description: goal.description || '',
    startDate: goal.startDate || '',
    endDate: goal.endDate || '',
    ownerId: goal.ownerId || ''
  }
  showEditObjectiveDialog.value = true
}

// Update Objective
const handleEditObjective = async () => {
  if (!editingObjective.value || !editingObjective.value.name) return

  isSubmitting.value = true
  try {
    await updateGoal(editingObjective.value.id, {
      name: editingObjective.value.name,
      description: editingObjective.value.description || undefined,
      startDate: editingObjective.value.startDate || undefined,
      endDate: editingObjective.value.endDate || undefined,
      ownerId: editingObjective.value.ownerId || undefined
    })

    editingObjective.value = null
    showEditObjectiveDialog.value = false

    // Refresh goals
    await fetchGoals()
  } catch (e) {
    console.error('Failed to update objective:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Delete Objective
const handleDeleteObjective = async (id: string) => {
  if (confirm('Are you sure you want to delete this objective? All associated key results will also be deleted.')) {
    try {
      await deleteGoal(id)
    } catch (e) {
      console.error('Failed to delete objective:', e)
    }
  }
}

// Delete from Edit Dialog
const handleDeleteObjectiveFromEdit = async () => {
  if (!editingObjective.value) return

  if (confirm('Are you sure you want to delete this objective? All associated key results will also be deleted.')) {
    isSubmitting.value = true
    try {
      await deleteGoal(editingObjective.value.id)
      editingObjective.value = null
      showEditObjectiveDialog.value = false
    } catch (e) {
      console.error('Failed to delete objective:', e)
    } finally {
      isSubmitting.value = false
    }
  }
}

// Open Add Key Result Dialog
const openAddKeyResultDialog = (parentGoalId: string) => {
  newKeyResult.value = {
    name: '',
    description: '',
    targetValue: 100,
    currentValue: 0,
    parentGoalId
  }
  showAddKeyResultDialog.value = true
}

// Create Key Result
const handleAddKeyResult = async () => {
  if (!newKeyResult.value.name || !newKeyResult.value.parentGoalId) return

  isSubmitting.value = true
  try {
    await createGoal({
      name: newKeyResult.value.name,
      description: newKeyResult.value.description || undefined,
      goalType: 'key_result',
      targetValue: newKeyResult.value.targetValue,
      currentValue: newKeyResult.value.currentValue,
      parentGoalId: newKeyResult.value.parentGoalId,
      status: 'active'
    })

    // Reset form and close dialog
    newKeyResult.value = { name: '', description: '', targetValue: 100, currentValue: 0, parentGoalId: '' }
    showAddKeyResultDialog.value = false

    // Refresh goals to get updated data
    await fetchGoals()
  } catch (e) {
    console.error('Failed to add key result:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Open Edit Key Result Dialog
const openEditKeyResultDialog = (kr: GoalWithProgress, parentGoalId: string) => {
  editingKeyResult.value = {
    id: kr.id,
    name: kr.name,
    description: kr.description || '',
    targetValue: kr.targetValue || 100,
    currentValue: kr.currentValue || 0,
    parentGoalId
  }
  showEditKeyResultDialog.value = true
}

// Update Key Result
const handleEditKeyResult = async () => {
  if (!editingKeyResult.value || !editingKeyResult.value.name) return

  isSubmitting.value = true
  try {
    await updateGoal(editingKeyResult.value.id, {
      name: editingKeyResult.value.name,
      description: editingKeyResult.value.description || undefined,
      targetValue: editingKeyResult.value.targetValue,
      currentValue: editingKeyResult.value.currentValue
    })

    editingKeyResult.value = null
    showEditKeyResultDialog.value = false

    // Refresh goals
    await fetchGoals()
  } catch (e) {
    console.error('Failed to update key result:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Delete Key Result
const handleDeleteKeyResult = async (id: string) => {
  if (confirm('Are you sure you want to delete this key result?')) {
    try {
      await deleteGoal(id)
      // Refresh goals
      await fetchGoals()
    } catch (e) {
      console.error('Failed to delete key result:', e)
    }
  }
}

// Delete Key Result from Edit Dialog
const handleDeleteKeyResultFromEdit = async () => {
  if (!editingKeyResult.value) return

  if (confirm('Are you sure you want to delete this key result?')) {
    isSubmitting.value = true
    try {
      await deleteGoal(editingKeyResult.value.id)
      editingKeyResult.value = null
      showEditKeyResultDialog.value = false
      // Refresh goals
      await fetchGoals()
    } catch (e) {
      console.error('Failed to delete key result:', e)
    } finally {
      isSubmitting.value = false
    }
  }
}

// Keyboard handlers
const handleObjectiveFormKeydown = (e: KeyboardEvent, isEdit: boolean) => {
  if (e.key === 'Enter' && !e.shiftKey && !isSubmitting.value) {
    const name = isEdit ? editingObjective.value?.name : newObjective.value.name
    if (name) {
      e.preventDefault()
      isEdit ? handleEditObjective() : handleAddObjective()
    }
  } else if (e.key === 'Escape') {
    if (isEdit) {
      showEditObjectiveDialog.value = false
      editingObjective.value = null
    } else {
      showAddObjectiveDialog.value = false
    }
  }
}

const handleKeyResultFormKeydown = (e: KeyboardEvent, isEdit: boolean) => {
  if (e.key === 'Enter' && !e.shiftKey && !isSubmitting.value) {
    const name = isEdit ? editingKeyResult.value?.name : newKeyResult.value.name
    if (name) {
      e.preventDefault()
      isEdit ? handleEditKeyResult() : handleAddKeyResult()
    }
  } else if (e.key === 'Escape') {
    if (isEdit) {
      showEditKeyResultDialog.value = false
      editingKeyResult.value = null
    } else {
      showAddKeyResultDialog.value = false
    }
  }
}

// Get owner name helper
const getOwnerName = (ownerId?: string): string | undefined => {
  if (!ownerId) return undefined
  const owner = people.value.find(p => p.id === ownerId)
  return owner?.name
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Goals & OKRs</h1>
        <p class="text-slate-400">Track objectives and key results across the organization</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- Quarter Dropdown -->
        <div class="relative">
          <button
            @click="showQuarterDropdown = !showQuarterDropdown"
            class="rounded-xl border border-slate-700 bg-slate-800/70 px-4 py-2 text-sm text-slate-200 hover:bg-slate-800 transition-colors flex items-center gap-2"
          >
            {{ selectedQuarterLabel }}
            <svg :class="['h-4 w-4 transition-transform', showQuarterDropdown ? 'rotate-180' : '']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          <!-- Dropdown Menu -->
          <div
            v-if="showQuarterDropdown"
            class="absolute right-0 mt-2 w-40 rounded-xl border border-slate-700 bg-slate-800 shadow-xl z-50 py-1 max-h-64 overflow-y-auto"
          >
            <button
              @click="selectedQuarter = 'all'; showQuarterDropdown = false"
              :class="[
                'w-full px-4 py-2 text-left text-sm hover:bg-white/10 transition-colors',
                selectedQuarter === 'all' ? 'text-purple-300 bg-purple-500/10' : 'text-slate-200'
              ]"
            >
              All Time
            </button>
            <button
              v-for="option in quarterOptions"
              :key="option.value"
              @click="selectedQuarter = option.value; showQuarterDropdown = false"
              :class="[
                'w-full px-4 py-2 text-left text-sm hover:bg-white/10 transition-colors',
                selectedQuarter === option.value ? 'text-purple-300 bg-purple-500/10' : 'text-slate-200'
              ]"
            >
              {{ option.label }}
            </button>
          </div>

          <!-- Click outside to close -->
          <div
            v-if="showQuarterDropdown"
            class="fixed inset-0 z-40"
            @click="showQuarterDropdown = false"
          />
        </div>

        <button
          @click="showAddObjectiveDialog = true"
          class="rounded-xl bg-gradient-to-r from-purple-500 to-blue-600 px-4 py-2 text-sm font-semibold text-white hover:from-purple-600 hover:to-blue-700 transition-all"
        >
          <span class="flex items-center gap-2">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            New Objective
          </span>
        </button>
      </div>
    </div>

    <!-- Summary Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">Overall Progress</div>
        <div class="mt-2 flex items-end gap-2">
          <span :class="['text-3xl font-bold', getProgressTextColor(overallProgress)]">{{ overallProgress }}%</span>
        </div>
        <div class="mt-2 h-2 rounded-full bg-slate-700">
          <div :class="['h-full rounded-full transition-all', getProgressColor(overallProgress)]" :style="{ width: `${overallProgress}%` }" />
        </div>
      </div>
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">Objectives</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ filteredGoals.length }}</div>
        <div class="mt-1 text-xs text-slate-400">{{ onTrackCount }} on track</div>
      </div>
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">Key Results</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ totalKeyResults }}</div>
        <div class="mt-1 text-xs text-slate-400">Across all objectives</div>
      </div>
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">At Risk</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">{{ atRiskCount }}</div>
        <div class="mt-1 text-xs text-slate-400">Need attention</div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredGoals.length === 0" class="rounded-2xl border border-slate-700 bg-slate-800/60 p-12 text-center">
      <div v-if="goals.length === 0" class="text-slate-400">No objectives defined yet.</div>
      <div v-else class="text-slate-400">No objectives found for {{ selectedQuarterLabel }}.</div>
      <button
        v-if="goals.length === 0"
        @click="showAddObjectiveDialog = true"
        class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
      >
        Create your first objective
      </button>
      <button
        v-else
        @click="selectedQuarter = 'all'"
        class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
      >
        View all objectives
      </button>
    </div>

    <!-- Objectives List -->
    <div v-else class="space-y-4">
      <div
        v-for="goal in filteredGoals"
        :key="goal.id"
        class="rounded-2xl border border-slate-700 bg-slate-800/60 overflow-hidden"
      >
        <!-- Objective Header -->
        <div class="p-6">
          <div class="flex items-start justify-between">
            <button
              class="flex-1 text-left"
              @click="toggleGoal(goal.id)"
            >
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 text-lg font-bold text-white">
                  O
                </div>
                <div>
                  <h3 class="text-lg font-semibold text-white">{{ goal.name }}</h3>
                  <p class="text-sm text-slate-400">{{ goal.keyResults?.length || 0 }} Key Results</p>
                </div>
              </div>
            </button>
            <div class="flex items-center gap-4">
              <div class="text-right">
                <div :class="['text-2xl font-bold', getProgressTextColor(goal.progress)]">{{ goal.progress }}%</div>
                <div class="text-xs text-slate-400">Progress</div>
              </div>

              <!-- Action buttons -->
              <div class="flex items-center gap-1">
                <button
                  @click.stop="openEditObjectiveDialog(goal)"
                  class="rounded-lg p-2 text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
                  title="Edit objective"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button
                  @click.stop="handleDeleteObjective(goal.id)"
                  class="rounded-lg p-2 text-slate-400 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                  title="Delete objective"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
                <button
                  @click="toggleGoal(goal.id)"
                  class="rounded-lg p-2 text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
                >
                  <svg
                    :class="['h-5 w-5 transition-transform', selectedGoal === goal.id ? 'rotate-180' : '']"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <!-- Progress Bar -->
          <div class="mt-4 h-2 rounded-full bg-slate-700">
            <div
              :class="['h-full rounded-full transition-all', getProgressColor(goal.progress)]"
              :style="{ width: `${goal.progress}%` }"
            />
          </div>

          <!-- Owner display -->
          <div v-if="goal.ownerId" class="mt-2 text-xs text-slate-400">
            Owner: {{ getOwnerName(goal.ownerId) || 'Unknown' }}
          </div>
        </div>

        <!-- Key Results (Expanded) -->
        <div v-if="selectedGoal === goal.id" class="border-t border-slate-700 bg-slate-900/40">
          <div class="p-4 space-y-3">
            <!-- Key Results List -->
            <div
              v-for="kr in goal.keyResults"
              :key="kr.id"
              class="rounded-xl border border-slate-700 bg-slate-800/60 p-4"
            >
              <div class="flex items-start justify-between mb-3">
                <div class="flex items-center gap-3">
                  <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-500/20 text-sm font-bold text-blue-300">
                    KR
                  </div>
                  <div>
                    <h4 class="font-medium text-white">{{ kr.name }}</h4>
                    <p v-if="kr.targetValue !== undefined" class="text-xs text-slate-400">
                      {{ kr.currentValue || 0 }} / {{ kr.targetValue }} target
                    </p>
                  </div>
                </div>
                <div class="flex items-center gap-3">
                  <span :class="['text-lg font-semibold', getProgressTextColor(kr.progress)]">
                    {{ kr.progress }}%
                  </span>
                  <div class="flex items-center gap-1">
                    <button
                      @click="openEditKeyResultDialog(kr, goal.id)"
                      class="rounded-lg p-1.5 text-slate-400 hover:text-white hover:bg-white/10 transition-colors"
                      title="Edit key result"
                    >
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button
                      @click="handleDeleteKeyResult(kr.id)"
                      class="rounded-lg p-1.5 text-slate-400 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                      title="Delete key result"
                    >
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
              <div class="h-1.5 rounded-full bg-slate-700">
                <div
                  :class="['h-full rounded-full transition-all', getProgressColor(kr.progress)]"
                  :style="{ width: `${kr.progress}%` }"
                />
              </div>
            </div>

            <!-- Add Key Result Button -->
            <button
              @click="openAddKeyResultDialog(goal.id)"
              class="w-full rounded-xl border border-dashed border-slate-600 bg-slate-800/30 p-4 text-sm text-slate-400 hover:text-white hover:border-slate-500 hover:bg-slate-800/50 transition-all flex items-center justify-center gap-2"
            >
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Add Key Result
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- AI Insights -->
    <div class="rounded-2xl border border-purple-500/30 bg-purple-500/10 p-6">
      <div class="flex items-start gap-4">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-purple-500/20">
          <svg class="h-5 w-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
          </svg>
        </div>
        <div class="flex-1">
          <h3 class="font-semibold text-purple-300">AI Goal Insights</h3>
          <p class="mt-1 text-sm text-slate-300">
            Based on current trajectory, "Expand 20 accounts by $10k+" is at risk of missing target.
            Consider: (1) Focus on top 5 expansion-ready accounts, (2) Accelerate QBR scheduling,
            (3) Bundle upsell with renewal conversations.
          </p>
          <button class="mt-3 text-sm text-purple-400 hover:text-purple-300 flex items-center gap-1">
            View detailed analysis
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Add Objective Dialog -->
    <Teleport to="body">
      <div
        v-if="showAddObjectiveDialog"
        class="fixed inset-0 z-50 flex items-center justify-center"
      >
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" @click="showAddObjectiveDialog = false" />

        <!-- Dialog Content -->
        <div
          class="relative w-full max-w-lg mx-4 orbitos-glass p-6"
          @click.stop
          @keydown="handleObjectiveFormKeydown($event, false)"
        >
          <h2 class="orbitos-heading-sm">New Objective</h2>
          <p class="text-sm orbitos-text mt-1">Create a new objective to track organizational goals.</p>

          <div class="mt-6 space-y-4">
            <div>
              <label for="objective-name" class="orbitos-label">Objective Name *</label>
              <input
                id="objective-name"
                v-model="newObjective.name"
                type="text"
                class="orbitos-input"
                placeholder="e.g., Increase market share in EMEA"
                autofocus
              />
            </div>

            <div>
              <label for="objective-description" class="orbitos-label">Description</label>
              <textarea
                id="objective-description"
                v-model="newObjective.description"
                rows="3"
                class="orbitos-input"
                placeholder="Describe what success looks like..."
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label for="objective-start-date" class="orbitos-label">Start Date</label>
                <input
                  id="objective-start-date"
                  v-model="newObjective.startDate"
                  type="date"
                  class="orbitos-input"
                />
              </div>
              <div>
                <label for="objective-end-date" class="orbitos-label">End Date</label>
                <input
                  id="objective-end-date"
                  v-model="newObjective.endDate"
                  type="date"
                  class="orbitos-input"
                />
              </div>
            </div>

            <div>
              <label for="objective-owner" class="orbitos-label">Owner</label>
              <select
                id="objective-owner"
                v-model="newObjective.ownerId"
                class="orbitos-input"
              >
                <option value="">Select an owner...</option>
                <option v-for="person in people" :key="person.id" :value="person.id">
                  {{ person.name }}{{ person.email ? ` (${person.email})` : '' }}
                </option>
              </select>
            </div>
          </div>

          <div class="mt-6 flex gap-3">
            <button
              type="button"
              @click="showAddObjectiveDialog = false; newObjective = { name: '', description: '', startDate: '', endDate: '', ownerId: '' }"
              class="flex-1 orbitos-btn-secondary"
            >
              Cancel
            </button>
            <button
              type="button"
              @click="handleAddObjective"
              :disabled="!newObjective.name || isSubmitting"
              class="flex-1 orbitos-btn-primary"
            >
              <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
                <div class="orbitos-spinner orbitos-spinner-sm"></div>
                Creating...
              </span>
              <span v-else>Create Objective</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Edit Objective Dialog -->
    <Teleport to="body">
      <div
        v-if="showEditObjectiveDialog && editingObjective"
        class="fixed inset-0 z-50 flex items-center justify-center"
      >
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" @click="showEditObjectiveDialog = false" />

        <!-- Dialog Content -->
        <div
          class="relative w-full max-w-lg mx-4 orbitos-glass p-6"
          @click.stop
          @keydown="handleObjectiveFormKeydown($event, true)"
        >
          <h2 class="orbitos-heading-sm">Edit Objective</h2>
          <p class="text-sm orbitos-text mt-1">Update objective information.</p>

          <div class="mt-6 space-y-4">
            <div>
              <label for="edit-objective-name" class="orbitos-label">Objective Name *</label>
              <input
                id="edit-objective-name"
                v-model="editingObjective.name"
                type="text"
                class="orbitos-input"
                placeholder="e.g., Increase market share in EMEA"
                autofocus
              />
            </div>

            <div>
              <label for="edit-objective-description" class="orbitos-label">Description</label>
              <textarea
                id="edit-objective-description"
                v-model="editingObjective.description"
                rows="3"
                class="orbitos-input"
                placeholder="Describe what success looks like..."
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label for="edit-objective-start-date" class="orbitos-label">Start Date</label>
                <input
                  id="edit-objective-start-date"
                  v-model="editingObjective.startDate"
                  type="date"
                  class="orbitos-input"
                />
              </div>
              <div>
                <label for="edit-objective-end-date" class="orbitos-label">End Date</label>
                <input
                  id="edit-objective-end-date"
                  v-model="editingObjective.endDate"
                  type="date"
                  class="orbitos-input"
                />
              </div>
            </div>

            <div>
              <label for="edit-objective-owner" class="orbitos-label">Owner</label>
              <select
                id="edit-objective-owner"
                v-model="editingObjective.ownerId"
                class="orbitos-input"
              >
                <option value="">Select an owner...</option>
                <option v-for="person in people" :key="person.id" :value="person.id">
                  {{ person.name }}{{ person.email ? ` (${person.email})` : '' }}
                </option>
              </select>
            </div>
          </div>

          <div class="mt-6 flex gap-3">
            <button
              type="button"
              @click="handleDeleteObjectiveFromEdit"
              :disabled="isSubmitting"
              class="orbitos-btn-secondary text-red-400 hover:text-red-300 hover:border-red-500/50"
              title="Delete objective"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
            <button
              type="button"
              @click="showEditObjectiveDialog = false; editingObjective = null"
              class="flex-1 orbitos-btn-secondary"
            >
              Cancel
            </button>
            <button
              type="button"
              @click="handleEditObjective"
              :disabled="!editingObjective.name || isSubmitting"
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
    </Teleport>

    <!-- Add Key Result Dialog -->
    <Teleport to="body">
      <div
        v-if="showAddKeyResultDialog"
        class="fixed inset-0 z-50 flex items-center justify-center"
      >
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" @click="showAddKeyResultDialog = false" />

        <!-- Dialog Content -->
        <div
          class="relative w-full max-w-lg mx-4 orbitos-glass p-6"
          @click.stop
          @keydown="handleKeyResultFormKeydown($event, false)"
        >
          <h2 class="orbitos-heading-sm">Add Key Result</h2>
          <p class="text-sm orbitos-text mt-1">Define a measurable key result for this objective.</p>

          <div class="mt-6 space-y-4">
            <div>
              <label for="kr-name" class="orbitos-label">Key Result Name *</label>
              <input
                id="kr-name"
                v-model="newKeyResult.name"
                type="text"
                class="orbitos-input"
                placeholder="e.g., Achieve $1M ARR in EMEA"
                autofocus
              />
            </div>

            <div>
              <label for="kr-description" class="orbitos-label">Description</label>
              <textarea
                id="kr-description"
                v-model="newKeyResult.description"
                rows="2"
                class="orbitos-input"
                placeholder="Additional details..."
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label for="kr-target" class="orbitos-label">Target Value</label>
                <input
                  id="kr-target"
                  v-model.number="newKeyResult.targetValue"
                  type="number"
                  min="0"
                  class="orbitos-input"
                  placeholder="100"
                />
              </div>
              <div>
                <label for="kr-current" class="orbitos-label">Current Value</label>
                <input
                  id="kr-current"
                  v-model.number="newKeyResult.currentValue"
                  type="number"
                  min="0"
                  class="orbitos-input"
                  placeholder="0"
                />
              </div>
            </div>

            <!-- Progress preview -->
            <div class="rounded-lg bg-slate-800/50 p-3">
              <div class="flex justify-between text-sm mb-2">
                <span class="text-slate-400">Progress Preview</span>
                <span :class="getProgressTextColor(Math.round((newKeyResult.currentValue / (newKeyResult.targetValue || 1)) * 100))">
                  {{ Math.round((newKeyResult.currentValue / (newKeyResult.targetValue || 1)) * 100) }}%
                </span>
              </div>
              <div class="h-2 rounded-full bg-slate-700">
                <div
                  :class="['h-full rounded-full transition-all', getProgressColor(Math.round((newKeyResult.currentValue / (newKeyResult.targetValue || 1)) * 100))]"
                  :style="{ width: `${Math.min(100, Math.round((newKeyResult.currentValue / (newKeyResult.targetValue || 1)) * 100))}%` }"
                />
              </div>
            </div>
          </div>

          <div class="mt-6 flex gap-3">
            <button
              type="button"
              @click="showAddKeyResultDialog = false; newKeyResult = { name: '', description: '', targetValue: 100, currentValue: 0, parentGoalId: '' }"
              class="flex-1 orbitos-btn-secondary"
            >
              Cancel
            </button>
            <button
              type="button"
              @click="handleAddKeyResult"
              :disabled="!newKeyResult.name || isSubmitting"
              class="flex-1 orbitos-btn-primary"
            >
              <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
                <div class="orbitos-spinner orbitos-spinner-sm"></div>
                Adding...
              </span>
              <span v-else>Add Key Result</span>
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Edit Key Result Dialog -->
    <Teleport to="body">
      <div
        v-if="showEditKeyResultDialog && editingKeyResult"
        class="fixed inset-0 z-50 flex items-center justify-center"
      >
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" @click="showEditKeyResultDialog = false" />

        <!-- Dialog Content -->
        <div
          class="relative w-full max-w-lg mx-4 orbitos-glass p-6"
          @click.stop
          @keydown="handleKeyResultFormKeydown($event, true)"
        >
          <h2 class="orbitos-heading-sm">Edit Key Result</h2>
          <p class="text-sm orbitos-text mt-1">Update key result progress and details.</p>

          <div class="mt-6 space-y-4">
            <div>
              <label for="edit-kr-name" class="orbitos-label">Key Result Name *</label>
              <input
                id="edit-kr-name"
                v-model="editingKeyResult.name"
                type="text"
                class="orbitos-input"
                placeholder="e.g., Achieve $1M ARR in EMEA"
                autofocus
              />
            </div>

            <div>
              <label for="edit-kr-description" class="orbitos-label">Description</label>
              <textarea
                id="edit-kr-description"
                v-model="editingKeyResult.description"
                rows="2"
                class="orbitos-input"
                placeholder="Additional details..."
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label for="edit-kr-target" class="orbitos-label">Target Value</label>
                <input
                  id="edit-kr-target"
                  v-model.number="editingKeyResult.targetValue"
                  type="number"
                  min="0"
                  class="orbitos-input"
                  placeholder="100"
                />
              </div>
              <div>
                <label for="edit-kr-current" class="orbitos-label">Current Value</label>
                <input
                  id="edit-kr-current"
                  v-model.number="editingKeyResult.currentValue"
                  type="number"
                  min="0"
                  class="orbitos-input"
                  placeholder="0"
                />
              </div>
            </div>

            <!-- Progress preview -->
            <div class="rounded-lg bg-slate-800/50 p-3">
              <div class="flex justify-between text-sm mb-2">
                <span class="text-slate-400">Progress Preview</span>
                <span :class="getProgressTextColor(Math.round((editingKeyResult.currentValue / (editingKeyResult.targetValue || 1)) * 100))">
                  {{ Math.round((editingKeyResult.currentValue / (editingKeyResult.targetValue || 1)) * 100) }}%
                </span>
              </div>
              <div class="h-2 rounded-full bg-slate-700">
                <div
                  :class="['h-full rounded-full transition-all', getProgressColor(Math.round((editingKeyResult.currentValue / (editingKeyResult.targetValue || 1)) * 100))]"
                  :style="{ width: `${Math.min(100, Math.round((editingKeyResult.currentValue / (editingKeyResult.targetValue || 1)) * 100))}%` }"
                />
              </div>
            </div>
          </div>

          <div class="mt-6 flex gap-3">
            <button
              type="button"
              @click="handleDeleteKeyResultFromEdit"
              :disabled="isSubmitting"
              class="orbitos-btn-secondary text-red-400 hover:text-red-300 hover:border-red-500/50"
              title="Delete key result"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
            <button
              type="button"
              @click="showEditKeyResultDialog = false; editingKeyResult = null"
              class="flex-1 orbitos-btn-secondary"
            >
              Cancel
            </button>
            <button
              type="button"
              @click="handleEditKeyResult"
              :disabled="!editingKeyResult.name || isSubmitting"
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
    </Teleport>
  </div>
</template>
