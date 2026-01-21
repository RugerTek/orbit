<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { processes, functions, isLoading, fetchProcesses, fetchFunctions, createProcess } = useOperations()

// Dialog state
const showAddDialog = ref(false)
const isSubmitting = ref(false)
const newProcess = ref({
  name: '',
  purpose: '',
  trigger: '',
  output: '',
  functionId: ''
})

// Fetch processes and functions on mount
onMounted(async () => {
  await Promise.all([fetchProcesses(), fetchFunctions()])
})

// Add process handler
const handleAddProcess = async () => {
  if (!newProcess.value.name) return

  isSubmitting.value = true
  try {
    const created = await createProcess({
      name: newProcess.value.name,
      purpose: newProcess.value.purpose || undefined,
      trigger: newProcess.value.trigger || undefined,
      output: newProcess.value.output || undefined,
      functionId: newProcess.value.functionId || undefined,
      status: 'draft',
      stateType: 'current'
    })

    // Reset form and close dialog
    newProcess.value = { name: '', purpose: '', trigger: '', output: '', functionId: '' }
    showAddDialog.value = false

    // Navigate to the new process
    if (created?.id) {
      navigateTo(`/app/processes/${created.id}`)
    }
  } catch (e) {
    console.error('Failed to add process:', e)
  } finally {
    isSubmitting.value = false
  }
}

// Handle Enter key
const handleFormKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Enter' && !e.shiftKey && newProcess.value.name && !isSubmitting.value) {
    e.preventDefault()
    handleAddProcess()
  }
  if (e.key === 'Escape') {
    showAddDialog.value = false
  }
}

// Get status color
const getStatusColor = (status: string) => {
  switch (status) {
    case 'active': return 'bg-emerald-500/20 text-emerald-300'
    case 'deprecated': return 'bg-red-500/20 text-red-300'
    default: return 'bg-amber-500/20 text-amber-300'
  }
}

// Get status label
const getStatusLabel = (status: string) => {
  switch (status) {
    case 'active': return 'Active'
    case 'deprecated': return 'Deprecated'
    default: return 'Draft'
  }
}

// Get frequency label
const getFrequencyLabel = (freq?: string) => {
  switch (freq) {
    case 'daily': return 'Daily'
    case 'weekly': return 'Weekly'
    case 'monthly': return 'Monthly'
    case 'on_demand': return 'On Demand'
    case 'continuous': return 'Continuous'
    default: return null
  }
}

// Stats
const stats = computed(() => {
  const active = processes.value.filter(p => p.status === 'active').length
  const draft = processes.value.filter(p => p.status === 'draft').length
  const totalFunctions = processes.value.reduce((acc, p) => acc + (p.functionCount || 0), 0)
  return { active, draft, totalFunctions }
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Processes</h1>
        <p class="orbitos-text">Design, monitor, and optimize operational workflows.</p>
      </div>
      <button
        type="button"
        @click="showAddDialog = true"
        class="orbitos-btn-primary py-2 px-4 text-sm"
      >
        <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        New Process
      </button>
    </div>

    <!-- Summary Stats -->
    <div class="grid gap-4 md:grid-cols-3">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total Processes</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ processes.length }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Active</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-300">{{ stats.active }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Linked Functions</div>
        <div class="mt-1 text-2xl font-semibold text-purple-300">{{ stats.totalFunctions }}</div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="processes.length === 0" class="orbitos-card-static p-12 text-center">
      <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-purple-500/20 flex items-center justify-center">
        <svg class="w-8 h-8 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" />
        </svg>
      </div>
      <div class="orbitos-text">No processes defined yet.</div>
      <button
        type="button"
        @click="showAddDialog = true"
        class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors"
      >
        Create your first process
      </button>
    </div>

    <!-- Process Cards -->
    <div v-else class="space-y-4">
      <NuxtLink
        v-for="process in processes"
        :key="process.id"
        :to="`/app/processes/${process.id}`"
        class="block orbitos-glass-subtle p-6 transition hover:border-purple-500/40"
      >
        <div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
          <div class="flex-1">
            <div class="flex items-center gap-3">
              <h2 class="text-lg font-semibold text-white">{{ process.name }}</h2>
              <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusColor(process.status)]">
                {{ getStatusLabel(process.status) }}
              </span>
              <span v-if="process.stateType === 'target'" class="rounded-full bg-blue-500/20 px-3 py-1 text-xs font-medium text-blue-300">
                Target State
              </span>
            </div>
            <p v-if="process.purpose" class="mt-1 text-sm text-white/60">{{ process.purpose }}</p>
          </div>
          <div class="flex items-center gap-2 text-white/40">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </div>

        <!-- Process Flow Preview -->
        <div class="mt-5 flex items-center gap-2 overflow-x-auto pb-2">
          <div v-if="process.trigger" class="flex items-center gap-1 rounded-lg bg-emerald-500/20 px-3 py-1.5 text-xs text-emerald-300">
            <svg class="h-3 w-3" fill="currentColor" viewBox="0 0 24 24">
              <circle cx="12" cy="12" r="10" />
            </svg>
            <span>{{ process.trigger }}</span>
          </div>
          <svg v-if="process.trigger" class="h-4 w-4 flex-shrink-0 text-white/20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
          </svg>
          <div class="flex items-center gap-1 rounded-lg bg-white/10 px-3 py-1.5 text-xs text-white/60">
            <span>{{ process.activityCount || 0 }} activities</span>
          </div>
          <svg v-if="process.output" class="h-4 w-4 flex-shrink-0 text-white/20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
          </svg>
          <div v-if="process.output" class="flex items-center gap-1 rounded-lg bg-purple-500/20 px-3 py-1.5 text-xs text-purple-300">
            <svg class="h-3 w-3" fill="currentColor" viewBox="0 0 24 24">
              <rect x="4" y="4" width="16" height="16" rx="2" />
            </svg>
            <span>{{ process.output }}</span>
          </div>
        </div>

        <!-- Meta Info -->
        <div class="mt-4 flex flex-wrap items-center gap-4 border-t border-white/10 pt-4 text-xs text-white/40">
          <div v-if="process.owner" class="flex items-center gap-1.5">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            <span>{{ process.owner.name }}</span>
          </div>
          <div class="flex items-center gap-1.5">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
            </svg>
            <span>{{ process.functionCount || 0 }} functions</span>
          </div>
          <div v-if="getFrequencyLabel(process.frequency)" class="flex items-center gap-1.5">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>{{ getFrequencyLabel(process.frequency) }}</span>
          </div>
        </div>
      </NuxtLink>
    </div>

    <!-- Add Process Dialog -->
    <div
      v-if="showAddDialog"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
      @keydown="handleFormKeydown"
    >
      <div class="w-full max-w-lg orbitos-glass p-6">
        <h2 class="orbitos-heading-sm">Create New Process</h2>
        <p class="text-sm orbitos-text mt-1">Define a new operational workflow.</p>

        <div class="mt-6 space-y-4">
          <div>
            <label class="orbitos-label">Process Name *</label>
            <input
              v-model="newProcess.name"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Customer Onboarding"
              autofocus
            />
          </div>

          <div>
            <label class="orbitos-label">Purpose</label>
            <input
              v-model="newProcess.purpose"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Activate new customers in 14 days"
            />
          </div>

          <div>
            <label class="orbitos-label">Trigger</label>
            <input
              v-model="newProcess.trigger"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Contract signed"
            />
          </div>

          <div>
            <label class="orbitos-label">Output</label>
            <input
              v-model="newProcess.output"
              type="text"
              class="orbitos-input"
              placeholder="e.g., Customer live on platform"
            />
          </div>

          <div>
            <label class="orbitos-label">Function</label>
            <select v-model="newProcess.functionId" class="orbitos-input">
              <option value="">Select a function (optional)</option>
              <option v-for="func in functions" :key="func.id" :value="func.id">
                {{ func.name }}
              </option>
            </select>
            <p class="text-xs text-white/40 mt-1">Associate this process with a business function</p>
          </div>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            @click="showAddDialog = false; newProcess = { name: '', purpose: '', trigger: '', output: '', functionId: '' }"
            class="flex-1 orbitos-btn-secondary"
          >
            Cancel
          </button>
          <button
            type="button"
            @click="handleAddProcess"
            :disabled="!newProcess.name || isSubmitting"
            class="flex-1 orbitos-btn-primary"
          >
            <span v-if="isSubmitting" class="flex items-center justify-center gap-2">
              <div class="orbitos-spinner orbitos-spinner-sm"></div>
              Creating...
            </span>
            <span v-else>Create Process</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
