<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { healthScore, stats, fetchHealthScore } = useOperations()

onMounted(() => {
  fetchHealthScore()
})

const getScoreColor = (score: number) => {
  if (score >= 80) return 'text-emerald-400'
  if (score >= 60) return 'text-blue-400'
  if (score >= 40) return 'text-amber-400'
  return 'text-red-400'
}

const getScoreBgColor = (score: number) => {
  if (score >= 80) return 'bg-emerald-500'
  if (score >= 60) return 'bg-blue-500'
  if (score >= 40) return 'bg-amber-500'
  return 'bg-red-500'
}

const getSeverityColor = (severity: string) => {
  switch (severity) {
    case 'critical': return 'bg-red-500/20 text-red-300 border-red-500/30'
    case 'warning': return 'bg-amber-500/20 text-amber-300 border-amber-500/30'
    default: return 'bg-blue-500/20 text-blue-300 border-blue-500/30'
  }
}

const getSeverityIcon = (severity: string) => {
  switch (severity) {
    case 'critical': return 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z'
    case 'warning': return 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
    default: return 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Organization Health</h1>
        <p class="text-slate-400">Real-time health score and operational insights</p>
      </div>
      <div class="flex items-center gap-3">
        <span class="text-sm text-slate-400">
          Last updated: {{ new Date(healthScore.lastCalculated).toLocaleString() }}
        </span>
        <button class="rounded-xl border border-slate-700 bg-slate-800/70 px-4 py-2 text-sm text-slate-200">
          <svg class="h-4 w-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          Refresh
        </button>
      </div>
    </div>

    <!-- Main Score Card -->
    <div class="rounded-2xl border border-slate-700 bg-gradient-to-br from-slate-800/80 to-slate-900/80 p-8">
      <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-8">
        <!-- Overall Score -->
        <div class="flex items-center gap-6">
          <div class="relative">
            <svg class="w-32 h-32 transform -rotate-90">
              <circle
                cx="64"
                cy="64"
                r="56"
                fill="none"
                stroke="currentColor"
                stroke-width="12"
                class="text-slate-700"
              />
              <circle
                cx="64"
                cy="64"
                r="56"
                fill="none"
                stroke="currentColor"
                stroke-width="12"
                :stroke-dasharray="`${healthScore.overall * 3.52} 352`"
                stroke-linecap="round"
                :class="getScoreColor(healthScore.overall)"
              />
            </svg>
            <div class="absolute inset-0 flex items-center justify-center">
              <span :class="['text-4xl font-bold', getScoreColor(healthScore.overall)]">{{ healthScore.overall }}</span>
            </div>
          </div>
          <div>
            <h2 class="text-2xl font-bold text-white">Overall Health Score</h2>
            <p class="text-slate-400 mt-1">
              {{ healthScore.overall >= 80 ? 'Excellent' : healthScore.overall >= 60 ? 'Good' : healthScore.overall >= 40 ? 'Needs Attention' : 'Critical' }}
            </p>
          </div>
        </div>

        <!-- Dimension Scores -->
        <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div class="text-center">
            <div :class="['text-2xl font-bold', getScoreColor(healthScore.dimensions.coverage)]">
              {{ healthScore.dimensions.coverage }}%
            </div>
            <div class="text-xs text-slate-400 mt-1">Coverage</div>
          </div>
          <div class="text-center">
            <div class="text-2xl font-bold text-red-400">
              {{ healthScore.dimensions.spofCount }}
            </div>
            <div class="text-xs text-slate-400 mt-1">SPOFs</div>
          </div>
          <div class="text-center">
            <div :class="['text-2xl font-bold', getScoreColor(healthScore.dimensions.processHealth)]">
              {{ healthScore.dimensions.processHealth }}%
            </div>
            <div class="text-xs text-slate-400 mt-1">Process Health</div>
          </div>
          <div class="text-center">
            <div :class="['text-2xl font-bold', getScoreColor(healthScore.dimensions.goalProgress)]">
              {{ healthScore.dimensions.goalProgress }}%
            </div>
            <div class="text-xs text-slate-400 mt-1">Goal Progress</div>
          </div>
          <div class="text-center">
            <div :class="['text-2xl font-bold', getScoreColor(healthScore.dimensions.resourceUtilization)]">
              {{ healthScore.dimensions.resourceUtilization }}%
            </div>
            <div class="text-xs text-slate-400 mt-1">Utilization</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="grid gap-4 md:grid-cols-6">
      <NuxtLink to="/app/processes" class="rounded-xl border border-slate-700 bg-slate-800/60 p-4 hover:border-purple-500/40 transition-colors">
        <div class="text-xs uppercase text-slate-500">Processes</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ stats.processCount }}</div>
      </NuxtLink>
      <NuxtLink to="/app/functions" class="rounded-xl border border-slate-700 bg-slate-800/60 p-4 hover:border-purple-500/40 transition-colors">
        <div class="text-xs uppercase text-slate-500">Functions</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ stats.functionCount }}</div>
      </NuxtLink>
      <NuxtLink to="/app/roles" class="rounded-xl border border-slate-700 bg-slate-800/60 p-4 hover:border-purple-500/40 transition-colors">
        <div class="text-xs uppercase text-slate-500">Roles</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ stats.roleCount }}</div>
      </NuxtLink>
      <NuxtLink to="/app/people" class="rounded-xl border border-slate-700 bg-slate-800/60 p-4 hover:border-purple-500/40 transition-colors">
        <div class="text-xs uppercase text-slate-500">People</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ stats.peopleCount }}</div>
      </NuxtLink>
      <div class="rounded-xl border border-red-500/30 bg-red-500/10 p-4">
        <div class="text-xs uppercase text-red-400">SPOFs</div>
        <div class="mt-1 text-2xl font-semibold text-red-300">{{ stats.spofCount }}</div>
      </div>
      <div class="rounded-xl border border-amber-500/30 bg-amber-500/10 p-4">
        <div class="text-xs uppercase text-amber-400">At Risk</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">{{ stats.atRiskCount }}</div>
      </div>
    </div>

    <!-- Alerts Section -->
    <div>
      <h2 class="text-lg font-semibold text-white mb-4">Active Alerts</h2>
      <div class="space-y-3">
        <div
          v-for="alert in healthScore.alerts"
          :key="alert.id"
          :class="[
            'rounded-xl border p-4 flex items-start gap-4',
            getSeverityColor(alert.severity)
          ]"
        >
          <div :class="['flex-shrink-0 p-2 rounded-lg', alert.severity === 'critical' ? 'bg-red-500/20' : alert.severity === 'warning' ? 'bg-amber-500/20' : 'bg-blue-500/20']">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getSeverityIcon(alert.severity)" />
            </svg>
          </div>
          <div class="flex-1">
            <div class="flex items-center gap-2">
              <h3 class="font-semibold">{{ alert.title }}</h3>
              <span class="rounded-full px-2 py-0.5 text-xs uppercase" :class="getSeverityColor(alert.severity)">
                {{ alert.severity }}
              </span>
            </div>
            <p class="mt-1 text-sm opacity-80">{{ alert.description }}</p>
            <p v-if="alert.suggestedAction" class="mt-2 text-sm">
              <span class="font-medium">Suggested action:</span> {{ alert.suggestedAction }}
            </p>
          </div>
          <NuxtLink
            :to="`/app/${alert.entityType === 'function' ? 'functions' : alert.entityType === 'process' ? 'processes' : alert.entityType === 'role' ? 'roles' : 'goals'}/${alert.entityId}`"
            class="flex-shrink-0 rounded-lg bg-white/10 px-3 py-1.5 text-sm hover:bg-white/20 transition-colors"
          >
            View
          </NuxtLink>
        </div>
      </div>
    </div>

    <!-- AI Analysis -->
    <div class="rounded-2xl border border-purple-500/30 bg-purple-500/10 p-6">
      <div class="flex items-start gap-4">
        <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-purple-500/20">
          <svg class="h-6 w-6 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
          </svg>
        </div>
        <div class="flex-1">
          <h3 class="text-lg font-semibold text-purple-300">AI Health Analysis</h3>
          <div class="mt-3 space-y-3 text-sm text-slate-300">
            <p>
              <strong class="text-white">Top Priority:</strong> Address the "Approve Discount" SPOF in Finance.
              This function is used in 2 critical processes and creates a bottleneck when Jordan Lee is unavailable.
            </p>
            <p>
              <strong class="text-white">Quick Win:</strong> Cross-train Alex Chen on discount approval to eliminate
              this single point of failure. Estimated training time: 2 hours.
            </p>
            <p>
              <strong class="text-white">Trend:</strong> Health score improved 4 points since last week due to
              better process documentation coverage.
            </p>
          </div>
          <div class="mt-4 flex gap-3">
            <button class="rounded-lg bg-purple-500 px-4 py-2 text-sm font-medium text-white hover:bg-purple-600">
              Generate Action Plan
            </button>
            <button class="rounded-lg border border-purple-500/30 px-4 py-2 text-sm font-medium text-purple-300 hover:bg-purple-500/10">
              Ask Follow-up
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
