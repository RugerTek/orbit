<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const {
  organizationUsage,
  isLoading,
  error,
  fetchOrganizationUsage,
  formatTokens,
  formatCost,
  getProviderColor,
  getProviderTextColor
} = useUsageAnalytics()

const { currentOrganization } = useOrganizations()

// Date range selection
const dateRange = ref<'7d' | '30d' | '90d' | 'all'>('30d')
const hasFetched = ref(false)

const dateRangeOptions = [
  { value: '7d', label: 'Last 7 days' },
  { value: '30d', label: 'Last 30 days' },
  { value: '90d', label: 'Last 90 days' },
  { value: 'all', label: 'All time' }
]

// Calculate date range
function getDateRange(): { start?: Date; end?: Date } {
  const end = new Date()
  let start: Date | undefined

  switch (dateRange.value) {
    case '7d':
      start = new Date()
      start.setDate(start.getDate() - 7)
      break
    case '30d':
      start = new Date()
      start.setDate(start.getDate() - 30)
      break
    case '90d':
      start = new Date()
      start.setDate(start.getDate() - 90)
      break
    case 'all':
      start = undefined
      break
  }

  return { start, end: dateRange.value === 'all' ? undefined : end }
}

// Fetch data when date range changes
watch(dateRange, async () => {
  if (!currentOrganization.value) return
  const { start, end } = getDateRange()
  await fetchOrganizationUsage(undefined, start, end)
}, { immediate: false })

// Watch for organization to become available and fetch data
watch(currentOrganization, async (newOrg) => {
  if (newOrg && !hasFetched.value) {
    hasFetched.value = true
    const { start, end } = getDateRange()
    await fetchOrganizationUsage(undefined, start, end)
  }
}, { immediate: true })

// Initial fetch (only if org is already available)
onMounted(async () => {
  if (currentOrganization.value) {
    hasFetched.value = true
    const { start, end } = getDateRange()
    await fetchOrganizationUsage(undefined, start, end)
  }
})

// Calculate max for chart scaling
const maxDailyTokens = computed(() => {
  if (!organizationUsage.value?.dailyUsage.length) return 1
  return Math.max(...organizationUsage.value.dailyUsage.map(d => d.tokensUsed))
})

// Format date for display
function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

// Calculate percentage for bar chart
function getBarHeight(tokens: number): string {
  if (maxDailyTokens.value === 0) return '0%'
  return `${(tokens / maxDailyTokens.value) * 100}%`
}
</script>

<template>
  <div class="max-w-6xl mx-auto">
    <!-- Page Header -->
    <div class="mb-8">
      <div class="flex items-center gap-2 text-white/40 text-sm mb-2">
        <NuxtLink to="/app/settings" class="hover:text-white/60">Settings</NuxtLink>
        <span>/</span>
        <span class="text-white/60">Usage Analytics</span>
      </div>
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-white">AI Usage Analytics</h1>
          <p class="text-white/60 mt-1">Monitor token consumption, costs, and model usage</p>
        </div>

        <!-- Date Range Selector -->
        <div class="flex items-center gap-2">
          <button
            v-for="option in dateRangeOptions"
            :key="option.value"
            :class="[
              'px-3 py-1.5 rounded-lg text-sm font-medium transition-all',
              dateRange === option.value
                ? 'bg-purple-500 text-white'
                : 'bg-white/5 text-white/60 hover:bg-white/10 hover:text-white'
            ]"
            @click="dateRange = option.value as typeof dateRange"
          >
            {{ option.label }}
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-20">
      <div class="flex items-center gap-3 text-white/60">
        <svg class="animate-spin h-6 w-6" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
        </svg>
        <span>Loading usage data...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="orbitos-glass p-8 text-center">
      <div class="w-12 h-12 rounded-full bg-red-500/20 flex items-center justify-center mx-auto mb-4">
        <svg class="w-6 h-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </div>
      <p class="text-white/60">{{ error }}</p>
      <button class="orbitos-btn-secondary mt-4" @click="fetchOrganizationUsage()">
        Try Again
      </button>
    </div>

    <!-- Content -->
    <div v-else-if="organizationUsage" class="space-y-6">
      <!-- Summary Cards -->
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <!-- Total Tokens -->
        <div class="orbitos-glass p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-purple-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A2 2 0 013 12V7a4 4 0 014-4z" />
              </svg>
            </div>
            <span class="text-white/60 text-sm">Total Tokens</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ formatTokens(organizationUsage.totalTokens) }}</p>
        </div>

        <!-- Total Cost -->
        <div class="orbitos-glass p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-emerald-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <span class="text-white/60 text-sm">Total Cost</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ formatCost(organizationUsage.totalCostCents) }}</p>
        </div>

        <!-- AI Responses -->
        <div class="orbitos-glass p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-blue-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
            </div>
            <span class="text-white/60 text-sm">AI Responses</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ organizationUsage.totalResponses.toLocaleString() }}</p>
        </div>

        <!-- Conversations -->
        <div class="orbitos-glass p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-orange-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-orange-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8h2a2 2 0 012 2v6a2 2 0 01-2 2h-2v4l-4-4H9a1.994 1.994 0 01-1.414-.586m0 0L11 14h4a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2v4l.586-.586z" />
              </svg>
            </div>
            <span class="text-white/60 text-sm">Conversations</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ organizationUsage.conversationCount.toLocaleString() }}</p>
        </div>
      </div>

      <!-- Charts Row -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Daily Usage Chart -->
        <div class="orbitos-glass p-6">
          <h3 class="text-lg font-semibold text-white mb-4">Daily Token Usage</h3>

          <div v-if="organizationUsage.dailyUsage.length > 0" class="h-48">
            <!-- Simple bar chart -->
            <div class="flex items-end justify-between h-full gap-1">
              <div
                v-for="day in organizationUsage.dailyUsage"
                :key="day.date"
                class="flex-1 flex flex-col items-center gap-1"
              >
                <div
                  class="w-full bg-purple-500/60 rounded-t hover:bg-purple-500 transition-colors relative group"
                  :style="{ height: getBarHeight(day.tokensUsed), minHeight: day.tokensUsed > 0 ? '4px' : '0' }"
                >
                  <!-- Tooltip -->
                  <div class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 hidden group-hover:block z-10">
                    <div class="bg-gray-900 border border-white/10 rounded-lg px-3 py-2 text-xs whitespace-nowrap">
                      <p class="text-white font-medium">{{ formatDate(day.date) }}</p>
                      <p class="text-white/60">{{ formatTokens(day.tokensUsed) }} tokens</p>
                      <p class="text-white/60">{{ formatCost(day.costCents) }}</p>
                    </div>
                  </div>
                </div>
                <span class="text-[10px] text-white/40 truncate w-full text-center">
                  {{ formatDate(day.date).split(' ')[1] }}
                </span>
              </div>
            </div>
          </div>

          <div v-else class="h-48 flex items-center justify-center text-white/40">
            No usage data for this period
          </div>
        </div>

        <!-- Usage by Model -->
        <div class="orbitos-glass p-6">
          <h3 class="text-lg font-semibold text-white mb-4">Usage by Model</h3>

          <div v-if="organizationUsage.byModel.length > 0" class="space-y-3">
            <div
              v-for="model in organizationUsage.byModel"
              :key="model.modelId"
              class="p-3 rounded-xl bg-white/5 border border-white/10"
            >
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-center gap-2">
                  <span :class="['w-2 h-2 rounded-full', getProviderColor(model.provider)]" />
                  <span class="text-sm font-medium text-white">{{ model.modelId }}</span>
                  <span :class="['text-xs', getProviderTextColor(model.provider)]">{{ model.provider }}</span>
                </div>
                <span class="text-sm text-white/60">{{ model.responseCount }} responses</span>
              </div>
              <div class="flex items-center justify-between text-xs">
                <span class="text-white/40">{{ formatTokens(model.tokensUsed) }} tokens</span>
                <span class="text-emerald-400">{{ formatCost(model.costCents) }}</span>
              </div>
              <!-- Progress bar -->
              <div class="mt-2 h-1.5 bg-white/5 rounded-full overflow-hidden">
                <div
                  :class="['h-full rounded-full', getProviderColor(model.provider)]"
                  :style="{
                    width: `${(model.tokensUsed / organizationUsage.totalTokens) * 100}%`
                  }"
                />
              </div>
            </div>
          </div>

          <div v-else class="py-8 text-center text-white/40">
            No model usage data available
          </div>
        </div>
      </div>

      <!-- Usage by Agent -->
      <div class="orbitos-glass p-6">
        <h3 class="text-lg font-semibold text-white mb-4">Usage by Agent</h3>

        <div v-if="organizationUsage.byAgent.length > 0" class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="text-left text-white/40 text-xs border-b border-white/10">
                <th class="pb-3 font-medium">Agent</th>
                <th class="pb-3 font-medium text-right">Responses</th>
                <th class="pb-3 font-medium text-right">Tokens</th>
                <th class="pb-3 font-medium text-right">Cost</th>
                <th class="pb-3 font-medium text-right">Avg Tokens/Response</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-white/5">
              <tr v-for="agent in organizationUsage.byAgent" :key="agent.agentName" class="text-sm">
                <td class="py-3">
                  <span class="text-white font-medium">{{ agent.agentName }}</span>
                </td>
                <td class="py-3 text-right text-white/60">{{ agent.responseCount.toLocaleString() }}</td>
                <td class="py-3 text-right text-white/60">{{ formatTokens(agent.tokensUsed) }}</td>
                <td class="py-3 text-right text-emerald-400">{{ formatCost(agent.costCents) }}</td>
                <td class="py-3 text-right text-white/40">
                  {{ agent.responseCount > 0 ? Math.round(agent.tokensUsed / agent.responseCount).toLocaleString() : '-' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-else class="py-8 text-center text-white/40">
          No agent usage data available
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="orbitos-glass p-12 text-center">
      <div class="w-16 h-16 rounded-full bg-purple-500/20 flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      </div>
      <h3 class="text-lg font-semibold text-white mb-2">No Usage Data Yet</h3>
      <p class="text-white/60 max-w-md mx-auto">
        Start using AI agents in conversations to see your token usage and cost analytics here.
      </p>
    </div>
  </div>
</template>
