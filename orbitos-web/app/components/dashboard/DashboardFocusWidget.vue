<script setup lang="ts">
interface FocusItem {
  title: string
  status: 'Bottleneck' | 'Review' | 'Stable'
  summary: string
  href: string
}

const { get } = useApi()
const { currentOrganizationId } = useOrganizations()

const isLoading = ref(true)
const error = ref<string | null>(null)
const focusItems = ref<FocusItem[]>([])

const getOrgId = (): string => {
  if (currentOrganizationId.value) return currentOrganizationId.value
  if (typeof window !== 'undefined') {
    return localStorage.getItem('currentOrganizationId') || '11111111-1111-1111-1111-111111111111'
  }
  return '11111111-1111-1111-1111-111111111111'
}

const fetchFocusItems = async () => {
  try {
    isLoading.value = true
    error.value = null

    const response = await get<{ focusItems: FocusItem[] }>(
      `/api/organizations/${getOrgId()}/dashboard/focus`
    )

    focusItems.value = response.focusItems
  } catch (e) {
    console.error('Failed to fetch focus items:', e)
    error.value = 'Failed to load focus areas'
  } finally {
    isLoading.value = false
  }
}

const getStatusClasses = (status: string) => {
  switch (status) {
    case 'Bottleneck':
      return 'bg-red-500/20 text-red-300'
    case 'Review':
      return 'bg-amber-500/20 text-amber-300'
    default:
      return 'bg-emerald-500/20 text-emerald-300'
  }
}

onMounted(() => {
  fetchFocusItems()
})
</script>

<template>
  <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-lg font-semibold text-white">Current Focus</h2>
        <p class="text-sm text-slate-400">Highest impact areas across the org graph.</p>
      </div>
      <NuxtLink to="/app/processes" class="text-xs font-semibold text-purple-300 hover:text-purple-200">
        View all
      </NuxtLink>
    </div>

    <div class="mt-6 space-y-4">
      <!-- Skeleton loaders -->
      <template v-if="isLoading">
        <div v-for="i in 3" :key="i" class="rounded-xl border border-slate-700/60 bg-slate-900/60 p-4">
          <div class="flex items-start justify-between gap-4">
            <div class="flex-1">
              <div class="h-4 w-1/3 bg-slate-700/50 rounded animate-pulse mb-2" />
              <div class="h-3 w-2/3 bg-slate-700/50 rounded animate-pulse" />
            </div>
            <div class="h-6 w-20 bg-slate-700/50 rounded-full animate-pulse" />
          </div>
        </div>
      </template>

      <!-- Error state -->
      <div v-else-if="error" class="rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-center">
        <p class="text-sm text-red-300">{{ error }}</p>
        <button @click="fetchFocusItems" class="mt-2 text-xs text-red-400 hover:text-red-300">
          Retry
        </button>
      </div>

      <!-- Loaded items -->
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
              getStatusClasses(item.status)
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
</template>
