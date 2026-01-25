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
      <div class="flex items-center gap-3">
        <NuxtLink
          to="/app/processes/guide"
          class="orbitos-btn-secondary py-2 px-4 text-sm inline-flex items-center gap-2"
        >
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
          </svg>
          Mapping Guide
        </NuxtLink>
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

    <!-- Processes Table -->
    <div v-else class="orbitos-glass-subtle overflow-hidden">
      <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <div class="text-sm text-white/70">{{ processes.length }} processes</div>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-black/20 text-xs uppercase text-white/40">
            <tr>
              <th class="px-6 py-3">Process</th>
              <th class="px-6 py-3">Trigger</th>
              <th class="px-6 py-3">Output</th>
              <th class="px-6 py-3">Activities</th>
              <th class="px-6 py-3">Status</th>
              <th class="px-6 py-3">Owner</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr
              v-for="process in processes"
              :key="process.id"
              class="hover:bg-white/5 transition-colors cursor-pointer"
              @click="navigateTo(`/app/processes/${process.id}`)"
            >
              <td class="px-6 py-4">
                <NuxtLink :to="`/app/processes/${process.id}`" class="block hover:text-purple-300 transition-colors" @click.stop>
                  <div class="font-semibold text-white">{{ process.name }}</div>
                  <div v-if="process.purpose" class="text-xs text-white/40 mt-0.5 line-clamp-1">{{ process.purpose }}</div>
                </NuxtLink>
              </td>
              <td class="px-6 py-4">
                <span v-if="process.trigger" class="inline-flex items-center gap-1 rounded bg-emerald-500/20 px-2 py-1 text-xs text-emerald-300">
                  <svg class="h-2 w-2" fill="currentColor" viewBox="0 0 24 24">
                    <circle cx="12" cy="12" r="10" />
                  </svg>
                  {{ process.trigger }}
                </span>
                <span v-else class="text-white/30 text-xs">—</span>
              </td>
              <td class="px-6 py-4">
                <span v-if="process.output" class="inline-flex items-center gap-1 rounded bg-purple-500/20 px-2 py-1 text-xs text-purple-300">
                  <svg class="h-2 w-2" fill="currentColor" viewBox="0 0 24 24">
                    <rect x="4" y="4" width="16" height="16" rx="2" />
                  </svg>
                  {{ process.output }}
                </span>
                <span v-else class="text-white/30 text-xs">—</span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                  <span class="text-white">{{ process.activityCount || 0 }}</span>
                  <span class="text-white/40 text-xs">activities</span>
                </div>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-2">
                  <span :class="['rounded-full px-2.5 py-1 text-xs font-medium', getStatusColor(process.status)]">
                    {{ getStatusLabel(process.status) }}
                  </span>
                  <span v-if="process.stateType === 'target'" class="rounded-full bg-blue-500/20 px-2 py-0.5 text-xs text-blue-300">
                    Target
                  </span>
                </div>
              </td>
              <td class="px-6 py-4">
                <span v-if="process.owner" class="text-white/70 text-xs">{{ process.owner.name }}</span>
                <span v-else class="text-white/30 text-xs">—</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
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
