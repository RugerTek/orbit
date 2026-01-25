<script setup lang="ts">
interface StatItem {
  label: string
  value: string
  change: string
  href: string
}

const { get } = useApi()
const { currentOrganizationId } = useOrganizations()

const isLoading = ref(true)
const error = ref<string | null>(null)
const stats = ref<StatItem[]>([])

const getOrgId = (): string => {
  if (currentOrganizationId.value) return currentOrganizationId.value
  if (typeof window !== 'undefined') {
    return localStorage.getItem('currentOrganizationId') || '11111111-1111-1111-1111-111111111111'
  }
  return '11111111-1111-1111-1111-111111111111'
}

const fetchStats = async () => {
  try {
    isLoading.value = true
    error.value = null

    const response = await get<{
      peopleCount: number
      peopleChange: string
      rolesCount: number
      rolesChange: string
      functionsCount: number
      functionsChange: string
      processesCount: number
      processesChange: string
    }>(`/api/organizations/${getOrgId()}/dashboard/stats`)

    stats.value = [
      { label: 'People', value: String(response.peopleCount), change: response.peopleChange, href: '/app/people' },
      { label: 'Roles', value: String(response.rolesCount), change: response.rolesChange, href: '/app/roles' },
      { label: 'Functions', value: String(response.functionsCount), change: response.functionsChange, href: '/app/functions' },
      { label: 'Processes', value: String(response.processesCount), change: response.processesChange, href: '/app/processes' },
    ]
  } catch (e) {
    console.error('Failed to fetch dashboard stats:', e)
    error.value = 'Failed to load stats'
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchStats()
})
</script>

<template>
  <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
    <!-- Skeleton loaders -->
    <template v-if="isLoading">
      <div
        v-for="i in 4"
        :key="i"
        class="rounded-2xl border border-slate-700 bg-slate-800/60 p-5"
      >
        <div class="h-4 w-16 bg-slate-700/50 rounded animate-pulse" />
        <div class="mt-3 h-8 w-12 bg-slate-700/50 rounded animate-pulse" />
        <div class="mt-3 h-3 w-24 bg-slate-700/50 rounded animate-pulse" />
      </div>
    </template>

    <!-- Error state -->
    <div v-else-if="error" class="col-span-full rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-center">
      <p class="text-red-300">{{ error }}</p>
      <button @click="fetchStats" class="mt-2 text-sm text-red-400 hover:text-red-300">
        Retry
      </button>
    </div>

    <!-- Loaded stats -->
    <template v-else>
      <NuxtLink
        v-for="stat in stats"
        :key="stat.label"
        :to="stat.href"
        class="rounded-2xl border border-slate-700 bg-slate-800/60 p-5 transition hover:border-purple-500/40 hover:bg-slate-800"
      >
        <div class="text-sm text-slate-400">{{ stat.label }}</div>
        <div class="mt-2 text-3xl font-semibold text-white">{{ stat.value }}</div>
        <div class="mt-2 text-xs text-purple-300">{{ stat.change }}</div>
      </NuxtLink>
    </template>
  </div>
</template>
