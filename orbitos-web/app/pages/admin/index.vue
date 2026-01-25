<script setup lang="ts">
import { useSuperAdmin, type DashboardStats } from '~/composables/useSuperAdmin'

definePageMeta({
  layout: 'admin'
})

const { getDashboardStats } = useSuperAdmin()

const stats = ref<DashboardStats | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    stats.value = await getDashboardStats()
  } catch (e) {
    error.value = 'Failed to load dashboard stats'
    console.error(e)
  } finally {
    loading.value = false
  }
})

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const statCards = computed(() => {
  if (!stats.value) return []
  return [
    { name: 'Total Users', value: stats.value.totalUsers, change: stats.value.usersLast30Days, icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z', color: 'purple' },
    { name: 'Organizations', value: stats.value.totalOrganizations, change: stats.value.orgsLast30Days, icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4', color: 'blue' },
    { name: 'Roles', value: stats.value.totalRoles, icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z', color: 'green' },
    { name: 'Functions', value: stats.value.totalFunctions, icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4', color: 'amber' },
  ]
})

const colorClasses: Record<string, { bg: string; icon: string; text: string }> = {
  purple: { bg: 'bg-purple-500/20', icon: 'text-purple-400', text: 'text-purple-300' },
  blue: { bg: 'bg-blue-500/20', icon: 'text-blue-400', text: 'text-blue-300' },
  green: { bg: 'bg-green-500/20', icon: 'text-green-400', text: 'text-green-300' },
  amber: { bg: 'bg-amber-500/20', icon: 'text-amber-400', text: 'text-amber-300' },
}
</script>

<template>
  <div>
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-white">Dashboard</h1>
      <p class="mt-1 text-slate-400">System overview and recent activity</p>
    </div>

    <!-- Loading state -->
    <div v-if="loading" class="flex items-center justify-center py-12">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-purple-500/30 border-t-purple-500"></div>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="rounded-xl bg-red-500/10 border border-red-500/20 p-4">
      <p class="text-red-400">{{ error }}</p>
    </div>

    <!-- Content -->
    <div v-else-if="stats">
      <!-- Stats grid -->
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-8">
        <div
          v-for="stat in statCards"
          :key="stat.name"
          class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6"
        >
          <div class="flex items-center gap-4">
            <div :class="['rounded-xl p-3', colorClasses[stat.color].bg]">
              <svg :class="['h-6 w-6', colorClasses[stat.color].icon]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="stat.icon" />
              </svg>
            </div>
            <div>
              <p class="text-sm font-medium text-slate-400">{{ stat.name }}</p>
              <p class="text-2xl font-bold text-white">{{ stat.value }}</p>
            </div>
          </div>
          <div v-if="stat.change !== undefined" class="mt-4 flex items-center gap-1 text-sm">
            <span class="text-green-400">+{{ stat.change }}</span>
            <span class="text-slate-500">last 30 days</span>
          </div>
        </div>
      </div>

      <!-- Quick actions -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
        <NuxtLink
          to="/admin/users"
          class="group rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6 hover:border-purple-500/50 transition-colors"
        >
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-lg font-semibold text-white">Manage Users</h3>
              <p class="mt-1 text-sm text-slate-400">Create, edit, and delete user accounts</p>
            </div>
            <svg class="h-5 w-5 text-slate-500 group-hover:text-purple-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </NuxtLink>

        <NuxtLink
          to="/admin/organizations"
          class="group rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6 hover:border-purple-500/50 transition-colors"
        >
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-lg font-semibold text-white">Manage Organizations</h3>
              <p class="mt-1 text-sm text-slate-400">Create and configure organizations</p>
            </div>
            <svg class="h-5 w-5 text-slate-500 group-hover:text-purple-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </NuxtLink>

        <NuxtLink
          to="/admin/roles"
          class="group rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6 hover:border-purple-500/50 transition-colors"
        >
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-lg font-semibold text-white">Manage Roles</h3>
              <p class="mt-1 text-sm text-slate-400">Define roles and permissions</p>
            </div>
            <svg class="h-5 w-5 text-slate-500 group-hover:text-purple-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </NuxtLink>

        <NuxtLink
          to="/admin/usage"
          class="group rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6 hover:border-purple-500/50 transition-colors"
        >
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-lg font-semibold text-white">AI Usage Analytics</h3>
              <p class="mt-1 text-sm text-slate-400">Monitor global token usage and costs</p>
            </div>
            <svg class="h-5 w-5 text-slate-500 group-hover:text-purple-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </NuxtLink>
      </div>

      <!-- Recent activity -->
      <div class="rounded-2xl bg-slate-800/50 border border-slate-700/50 p-6">
        <h2 class="text-lg font-semibold text-white mb-4">Recent Activity</h2>
        <div v-if="stats.recentActivity.length === 0" class="text-center py-8">
          <p class="text-slate-400">No recent activity</p>
        </div>
        <div v-else class="space-y-4">
          <div
            v-for="(activity, index) in stats.recentActivity"
            :key="index"
            class="flex items-center gap-4 rounded-xl bg-slate-700/30 p-4"
          >
            <div :class="[
              'rounded-lg p-2',
              activity.type === 'User' ? 'bg-purple-500/20' : 'bg-blue-500/20'
            ]">
              <svg
                :class="[
                  'h-5 w-5',
                  activity.type === 'User' ? 'text-purple-400' : 'text-blue-400'
                ]"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  v-if="activity.type === 'User'"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
                <path
                  v-else
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
                />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm text-white">{{ activity.description }}</p>
              <p class="text-xs text-slate-400">{{ formatDate(activity.timestamp) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
