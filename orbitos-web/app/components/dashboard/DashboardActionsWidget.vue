<script setup lang="ts">
interface NextAction {
  action: string
  href?: string
}

const { get } = useApi()
const { currentOrganizationId } = useOrganizations()

const isLoading = ref(true)
const error = ref<string | null>(null)
const nextActions = ref<NextAction[]>([])

const getOrgId = (): string => {
  if (currentOrganizationId.value) return currentOrganizationId.value
  if (typeof window !== 'undefined') {
    return localStorage.getItem('currentOrganizationId') || '11111111-1111-1111-1111-111111111111'
  }
  return '11111111-1111-1111-1111-111111111111'
}

const fetchActions = async () => {
  try {
    isLoading.value = true
    error.value = null

    const response = await get<{ nextActions: NextAction[] }>(
      `/api/organizations/${getOrgId()}/dashboard/actions`
    )

    nextActions.value = response.nextActions
  } catch (e) {
    console.error('Failed to fetch next actions:', e)
    error.value = 'Failed to load actions'
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchActions()
})
</script>

<template>
  <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
    <h2 class="text-lg font-semibold text-white">Next Actions</h2>

    <!-- Skeleton loaders -->
    <div v-if="isLoading" class="mt-4 space-y-3">
      <div v-for="i in 4" :key="i" class="flex items-start gap-3">
        <div class="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-slate-700/50 animate-pulse" />
        <div class="h-4 flex-1 bg-slate-700/50 rounded animate-pulse" />
      </div>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="mt-4 text-center">
      <p class="text-sm text-red-300">{{ error }}</p>
      <button @click="fetchActions" class="mt-2 text-xs text-red-400 hover:text-red-300">
        Retry
      </button>
    </div>

    <!-- Loaded actions -->
    <ul v-else-if="nextActions.length > 0" class="mt-4 space-y-3 text-sm text-slate-300">
      <li v-for="(action, index) in nextActions" :key="index" class="flex items-start gap-3">
        <span class="mt-1 h-2 w-2 shrink-0 rounded-full bg-purple-400" />
        <NuxtLink v-if="action.href" :to="action.href" class="hover:text-purple-300 transition">
          {{ action.action }}
        </NuxtLink>
        <span v-else>{{ action.action }}</span>
      </li>
    </ul>

    <!-- Empty state -->
    <p v-else class="mt-4 text-sm text-slate-500">No actions needed. Great job!</p>
  </div>
</template>
