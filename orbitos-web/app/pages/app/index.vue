<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const quickStats = [
  { label: 'People', value: '14', change: '+2 this month', href: '/app/people' },
  { label: 'Roles', value: '9', change: '2 understaffed', href: '/app/roles' },
  { label: 'Functions', value: '38', change: 'All mapped', href: '/app/functions' },
  { label: 'Processes', value: '12', change: '3 need review', href: '/app/processes' },
]

const focusItems = [
  {
    title: 'Quote to Cash',
    status: 'Bottleneck',
    summary: 'Approval step relies on a single owner. Add backup and automation.',
    href: '/app/processes/diagram'
  },
  {
    title: 'Support Coverage',
    status: 'Stable',
    summary: 'Even workload across tiers with clear escalation paths.',
    href: '/app/people'
  },
  {
    title: 'Resource Spend',
    status: 'Review',
    summary: 'Top 5 tools account for 72% of monthly spend.',
    href: '/app/resources'
  }
]
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
                'rounded-full px-3 py-1 text-xs font-medium',
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
        </div>
      </div>

      <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
        <h2 class="text-lg font-semibold text-white">Next Actions</h2>
        <ul class="mt-4 space-y-3 text-sm text-slate-300">
          <li class="flex items-start gap-3">
            <span class="mt-1 h-2 w-2 rounded-full bg-purple-400" />
            Finalize role coverage for Sales Ops.
          </li>
          <li class="flex items-start gap-3">
            <span class="mt-1 h-2 w-2 rounded-full bg-purple-400" />
            Map automations in onboarding process.
          </li>
          <li class="flex items-start gap-3">
            <span class="mt-1 h-2 w-2 rounded-full bg-purple-400" />
            Review resource spend spike in Q3 tools.
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>
