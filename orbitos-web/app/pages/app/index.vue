<script setup lang="ts">
import { useApi } from '~/composables/useApi'

definePageMeta({
  layout: 'app'
})

// Types for dashboard insights
interface DashboardStats {
  peopleCount: number
  peopleChange: string
  rolesCount: number
  rolesChange: string
  functionsCount: number
  functionsChange: string
  processesCount: number
  processesChange: string
}

interface FocusItem {
  title: string
  status: 'Bottleneck' | 'Review' | 'Stable'
  summary: string
  href: string
}

interface NextAction {
  action: string
  href?: string
}

interface DashboardInsightsResponse {
  stats: DashboardStats
  focusItems: FocusItem[]
  nextActions: NextAction[]
  error?: string
}

// Hardcoded org ID - TODO: Get from user's organization membership
const DEFAULT_ORG_ID = '11111111-1111-1111-1111-111111111111'

const { get } = useApi()
const isLoading = ref(true)
const error = ref<string | null>(null)

// Reactive data
const stats = ref<DashboardStats>({
  peopleCount: 0,
  peopleChange: 'Loading...',
  rolesCount: 0,
  rolesChange: 'Loading...',
  functionsCount: 0,
  functionsChange: 'Loading...',
  processesCount: 0,
  processesChange: 'Loading...',
})

const focusItems = ref<FocusItem[]>([])
const nextActions = ref<NextAction[]>([])

// Computed stats array for template
const quickStats = computed(() => [
  { label: 'People', value: String(stats.value.peopleCount), change: stats.value.peopleChange, href: '/app/people' },
  { label: 'Roles', value: String(stats.value.rolesCount), change: stats.value.rolesChange, href: '/app/roles' },
  { label: 'Functions', value: String(stats.value.functionsCount), change: stats.value.functionsChange, href: '/app/functions' },
  { label: 'Processes', value: String(stats.value.processesCount), change: stats.value.processesChange, href: '/app/processes' },
])

// Fetch dashboard insights
const fetchInsights = async () => {
  try {
    isLoading.value = true
    error.value = null

    const response = await get<DashboardInsightsResponse>(
      `/api/organizations/${DEFAULT_ORG_ID}/ai/dashboard-insights`
    )

    if (response.error) {
      error.value = response.error
    } else {
      stats.value = response.stats
      focusItems.value = response.focusItems
      nextActions.value = response.nextActions
    }
  } catch (e) {
    console.error('Failed to fetch dashboard insights:', e)
    error.value = 'Failed to load dashboard data'
  } finally {
    isLoading.value = false
  }
}

// Fetch on mount
onMounted(() => {
  fetchInsights()
})
</script>

<template>
  <div class="space-y-8">
    <div class="flex flex-col gap-2">
      <h1 class="text-2xl font-bold text-white">Operations Overview</h1>
      <p class="text-slate-400">A live snapshot of people, roles, processes, and resources.</p>
    </div>

    <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <NuxtLink
        v-for="stat in quickStats"
        :key="stat.label"
        :to="stat.href"
        class="rounded-2xl border border-slate-700 bg-slate-800/60 p-5 transition hover:border-purple-500/40 hover:bg-slate-800"
      >
        <div class="text-sm text-slate-400">{{ stat.label }}</div>
        <div class="mt-2 text-3xl font-semibold text-white">{{ stat.value }}</div>
        <div class="mt-2 text-xs text-purple-300">{{ stat.change }}</div>
      </NuxtLink>
    </div>

    <div class="grid gap-4 lg:grid-cols-3">
      <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6 lg:col-span-2">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-lg font-semibold text-white">Current Focus</h2>
            <p class="text-sm text-slate-400">Highest impact areas across the org graph.</p>
          </div>
          <NuxtLink to="/app/processes" class="text-xs font-semibold text-purple-300 hover:text-purple-200">View all</NuxtLink>
        </div>
        <div class="mt-6 space-y-4">
          <!-- Loading state -->
          <template v-if="isLoading">
            <div v-for="i in 3" :key="i" class="rounded-xl border border-slate-700/60 bg-slate-900/60 p-4">
              <div class="h-4 w-1/3 bg-slate-700/50 rounded animate-pulse mb-2" />
              <div class="h-3 w-2/3 bg-slate-700/50 rounded animate-pulse" />
            </div>
          </template>
          <!-- Loaded state -->
          <template v-else-if="focusItems.length > 0">
            <NuxtLink
              v-for="item in focusItems"
              :key="item.title"
              :to="item.href"
              class="flex items-start justify-between gap-4 rounded-xl border border-slate-700/60 bg-slate-900/60 p-4 transition hover:border-purple-500/40"
            >
              <div>
                <div class="text-sm font-semibold text-white">{{ item.title }}</div>
                <div class="mt-1 text-xs text-slate-400">{{ item.summary }}</div>
              </div>
              <span
                :class="[
                  'shrink-0 rounded-full px-3 py-1 text-xs font-medium',
                  item.status === 'Bottleneck'
                    ? 'bg-red-500/20 text-red-300'
                    : item.status === 'Review'
                      ? 'bg-amber-500/20 text-amber-300'
                      : 'bg-emerald-500/20 text-emerald-300'
                ]"
              >
                {{ item.status }}
              </span>
            </NuxtLink>
          </template>
          <!-- Empty state -->
          <div v-else class="rounded-xl border border-slate-700/60 bg-slate-900/60 p-4 text-center">
            <p class="text-sm text-slate-500">No focus areas identified. Add more data to get insights.</p>
          </div>
        </div>
      </div>

      <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
        <h2 class="text-lg font-semibold text-white">Next Actions</h2>
        <ul v-if="nextActions.length > 0" class="mt-4 space-y-3 text-sm text-slate-300">
          <li v-for="(action, index) in nextActions" :key="index" class="flex items-start gap-3">
            <span class="mt-1 h-2 w-2 shrink-0 rounded-full bg-purple-400" />
            <NuxtLink v-if="action.href" :to="action.href" class="hover:text-purple-300 transition">
              {{ action.action }}
            </NuxtLink>
            <span v-else>{{ action.action }}</span>
          </li>
        </ul>
        <div v-else-if="isLoading" class="mt-4 space-y-3">
          <div v-for="i in 3" :key="i" class="h-4 bg-slate-700/50 rounded animate-pulse" />
        </div>
        <p v-else class="mt-4 text-sm text-slate-500">No actions needed. Great job!</p>
      </div>
    </div>
  </div>
</template>
