<script setup lang="ts">
definePageMeta({
  layout: 'admin'
})

const {
  globalUsage,
  isLoading,
  error,
  fetchGlobalUsage,
  formatTokens,
  formatCost,
  getProviderColor,
  getProviderTextColor
} = useUsageAnalytics()

// Date range selection
const dateRange = ref<'7d' | '30d' | '90d' | 'all'>('30d')

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
  const { start, end } = getDateRange()
  await fetchGlobalUsage(start, end)
}, { immediate: false })

// Initial fetch
onMounted(async () => {
  const { start, end } = getDateRange()
  await fetchGlobalUsage(start, end)
})

// Calculate max for chart scaling
const maxDailyTokens = computed(() => {
  if (!globalUsage.value?.dailyUsage.length) return 1
  return Math.max(...globalUsage.value.dailyUsage.map(d => d.tokensUsed))
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

// Calculate percentage of total for pie-like displays
function getPercentage(value: number, total: number): string {
  if (total === 0) return '0%'
  return `${((value / total) * 100).toFixed(1)}%`
}
</script>

<template>
  <div>
    <!-- Page Header -->
    <div class="mb-8">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-white">Global AI Usage Analytics</h1>
          <p class="text-slate-400 mt-1">Monitor token consumption and costs across all organizations</p>
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
                : 'bg-slate-700/50 text-slate-400 hover:bg-slate-700 hover:text-white'
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
      <div class="flex items-center gap-3 text-slate-400">
        <svg class="animate-spin h-6 w-6" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
        </svg>
        <span>Loading global usage data...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="rounded-2xl bg-red-500/10 border border-red-500/20 p-8 text-center">
      <div class="w-12 h-12 rounded-full bg-red-500/20 flex items-center justify-center mx-auto mb-4">
        <svg class="w-6 h-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </div>
      <p class="text-red-400">{{ error }}</p>
      <button class="mt-4 px-4 py-2 bg-slate-700 hover:bg-slate-600 text-white rounded-lg transition-colors" @click="fetchGlobalUsage()">
        Try Again
      </button>
    </div>

    <!-- Content -->
    <div v-else-if="globalUsage" class="space-y-6">
      <!-- Summary Cards -->
      <div class="grid grid-cols-1 md:grid-cols-5 gap-4">
        <!-- Total Tokens -->
        <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-purple-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A2 2 0 013 12V7a4 4 0 014-4z" />
              </svg>
            </div>
            <span class="text-slate-400 text-sm">Total Tokens</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ formatTokens(globalUsage.totalTokens) }}</p>
        </div>

        <!-- Total Cost -->
        <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-emerald-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <span class="text-slate-400 text-sm">Total Cost</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ formatCost(globalUsage.totalCostCents) }}</p>
        </div>

        <!-- AI Responses -->
        <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-blue-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
              </svg>
            </div>
            <span class="text-slate-400 text-sm">AI Responses</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ globalUsage.totalResponses.toLocaleString() }}</p>
        </div>

        <!-- Organizations -->
        <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-orange-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-orange-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
            </div>
            <span class="text-slate-400 text-sm">Active Orgs</span>
          </div>
          <p class="text-2xl font-bold text-white">{{ globalUsage.organizationCount }}</p>
        </div>

        <!-- Avg Cost per Org -->
        <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-cyan-500/20 flex items-center justify-center">
              <svg class="w-5 h-5 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
            </div>
            <span class="text-slate-400 text-sm">Avg/Org</span>
          </div>
          <p class="text-2xl font-bold text-white">
            {{ globalUsage.organizationCount > 0 ? formatCost(Math.round(globalUsage.totalCostCents / globalUsage.organizationCount)) : '$0.00' }}
          </p>
        </div>
      </div>

      <!-- Charts Row -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Daily Usage Chart -->
        <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6">
          <h3 class="text-lg font-semibold text-white mb-4">Daily Token Usage</h3>

          <div v-if="globalUsage.dailyUsage.length > 0" class="h-48">
            <!-- Simple bar chart -->
            <div class="flex items-end justify-between h-full gap-1">
              <div
                v-for="day in globalUsage.dailyUsage"
                :key="day.date"
                class="flex-1 flex flex-col items-center gap-1"
              >
                <div
                  class="w-full bg-purple-500/60 rounded-t hover:bg-purple-500 transition-colors relative group"
                  :style="{ height: getBarHeight(day.tokensUsed), minHeight: day.tokensUsed > 0 ? '4px' : '0' }"
                >
                  <!-- Tooltip -->
                  <div class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 hidden group-hover:block z-10">
                    <div class="bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 text-xs whitespace-nowrap">
                      <p class="text-white font-medium">{{ formatDate(day.date) }}</p>
                      <p class="text-slate-400">{{ formatTokens(day.tokensUsed) }} tokens</p>
                      <p class="text-slate-400">{{ formatCost(day.costCents) }}</p>
                    </div>
                  </div>
                </div>
                <span class="text-[10px] text-slate-500 truncate w-full text-center">
                  {{ formatDate(day.date).split(' ')[1] }}
                </span>
              </div>
            </div>
          </div>

          <div v-else class="h-48 flex items-center justify-center text-slate-500">
            No usage data for this period
          </div>
        </div>

        <!-- Usage by Provider -->
        <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6">
          <h3 class="text-lg font-semibold text-white mb-4">Usage by Provider</h3>

          <div v-if="globalUsage.byProvider.length > 0" class="space-y-4">
            <div
              v-for="provider in globalUsage.byProvider"
              :key="provider.provider"
              class="p-4 rounded-xl bg-slate-700/30 border border-slate-700/50"
            >
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-center gap-3">
                  <span :class="['w-3 h-3 rounded-full', getProviderColor(provider.provider)]" />
                  <span class="text-base font-semibold text-white">{{ provider.provider }}</span>
                </div>
                <span class="text-lg font-bold text-emerald-400">{{ formatCost(provider.costCents) }}</span>
              </div>
              <div class="flex items-center justify-between text-sm text-slate-400 mb-3">
                <span>{{ formatTokens(provider.tokensUsed) }} tokens</span>
                <span>{{ provider.responseCount.toLocaleString() }} responses</span>
                <span>{{ getPercentage(provider.tokensUsed, globalUsage.totalTokens) }}</span>
              </div>
              <!-- Progress bar -->
              <div class="h-2 bg-slate-700 rounded-full overflow-hidden">
                <div
                  :class="['h-full rounded-full', getProviderColor(provider.provider)]"
                  :style="{
                    width: `${(provider.tokensUsed / globalUsage.totalTokens) * 100}%`
                  }"
                />
              </div>
            </div>
          </div>

          <div v-else class="py-8 text-center text-slate-500">
            No provider usage data available
          </div>
        </div>
      </div>

      <!-- Usage by Model -->
      <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6">
        <h3 class="text-lg font-semibold text-white mb-4">Usage by Model</h3>

        <div v-if="globalUsage.byModel.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div
            v-for="model in globalUsage.byModel"
            :key="model.modelId"
            class="p-4 rounded-xl bg-slate-700/30 border border-slate-700/50"
          >
            <div class="flex items-center gap-2 mb-2">
              <span :class="['w-2 h-2 rounded-full', getProviderColor(model.provider)]" />
              <span class="text-sm font-medium text-white truncate">{{ model.modelId }}</span>
            </div>
            <div :class="['text-xs mb-3', getProviderTextColor(model.provider)]">{{ model.provider }}</div>
            <div class="grid grid-cols-3 gap-2 text-center">
              <div>
                <p class="text-lg font-bold text-white">{{ formatTokens(model.tokensUsed) }}</p>
                <p class="text-xs text-slate-500">tokens</p>
              </div>
              <div>
                <p class="text-lg font-bold text-emerald-400">{{ formatCost(model.costCents) }}</p>
                <p class="text-xs text-slate-500">cost</p>
              </div>
              <div>
                <p class="text-lg font-bold text-white">{{ model.responseCount }}</p>
                <p class="text-xs text-slate-500">responses</p>
              </div>
            </div>
          </div>
        </div>

        <div v-else class="py-8 text-center text-slate-500">
          No model usage data available
        </div>
      </div>

      <!-- Usage by Organization -->
      <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6">
        <h3 class="text-lg font-semibold text-white mb-4">Usage by Organization</h3>

        <div v-if="globalUsage.byOrganization.length > 0" class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="text-left text-slate-400 text-xs border-b border-slate-700">
                <th class="pb-3 font-medium">Organization</th>
                <th class="pb-3 font-medium text-right">Responses</th>
                <th class="pb-3 font-medium text-right">Tokens</th>
                <th class="pb-3 font-medium text-right">Cost</th>
                <th class="pb-3 font-medium text-right">Share</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-700/50">
              <tr v-for="org in globalUsage.byOrganization" :key="org.organizationId" class="text-sm">
                <td class="py-4">
                  <span class="text-white font-medium">{{ org.organizationName }}</span>
                </td>
                <td class="py-4 text-right text-slate-400">{{ org.responseCount.toLocaleString() }}</td>
                <td class="py-4 text-right text-slate-400">{{ formatTokens(org.tokensUsed) }}</td>
                <td class="py-4 text-right text-emerald-400 font-medium">{{ formatCost(org.costCents) }}</td>
                <td class="py-4 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <div class="w-16 h-1.5 bg-slate-700 rounded-full overflow-hidden">
                      <div
                        class="h-full bg-purple-500 rounded-full"
                        :style="{
                          width: `${(org.tokensUsed / globalUsage.totalTokens) * 100}%`
                        }"
                      />
                    </div>
                    <span class="text-slate-500 text-xs w-12 text-right">
                      {{ getPercentage(org.tokensUsed, globalUsage.totalTokens) }}
                    </span>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-else class="py-8 text-center text-slate-500">
          No organization usage data available
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-12 text-center">
      <div class="w-16 h-16 rounded-full bg-purple-500/20 flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      </div>
      <h3 class="text-lg font-semibold text-white mb-2">No Global Usage Data Yet</h3>
      <p class="text-slate-400 max-w-md mx-auto">
        AI usage data will appear here once organizations start using AI agents in conversations.
      </p>
    </div>
  </div>
</template>
