<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { goals, fetchGoals } = useOperations()

onMounted(() => {
  fetchGoals()
})

const getProgressColor = (progress: number) => {
  if (progress >= 80) return 'bg-emerald-500'
  if (progress >= 50) return 'bg-blue-500'
  if (progress >= 30) return 'bg-amber-500'
  return 'bg-red-500'
}

const getProgressTextColor = (progress: number) => {
  if (progress >= 80) return 'text-emerald-300'
  if (progress >= 50) return 'text-blue-300'
  if (progress >= 30) return 'text-amber-300'
  return 'text-red-300'
}

const selectedGoal = ref<string | null>(null)

const toggleGoal = (id: string) => {
  selectedGoal.value = selectedGoal.value === id ? null : id
}

const overallProgress = computed(() => {
  if (goals.value.length === 0) return 0
  return Math.round(goals.value.reduce((acc, g) => acc + g.progress, 0) / goals.value.length)
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Goals & OKRs</h1>
        <p class="text-slate-400">Track objectives and key results across the organization</p>
      </div>
      <div class="flex items-center gap-3">
        <button class="rounded-xl border border-slate-700 bg-slate-800/70 px-4 py-2 text-sm text-slate-200">
          Q1 2024
          <svg class="ml-2 inline h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </button>
        <button class="rounded-xl bg-gradient-to-r from-purple-500 to-blue-600 px-4 py-2 text-sm font-semibold text-white">
          <span class="flex items-center gap-2">
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            New Objective
          </span>
        </button>
      </div>
    </div>

    <!-- Summary Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">Overall Progress</div>
        <div class="mt-2 flex items-end gap-2">
          <span :class="['text-3xl font-bold', getProgressTextColor(overallProgress)]">{{ overallProgress }}%</span>
        </div>
        <div class="mt-2 h-2 rounded-full bg-slate-700">
          <div :class="['h-full rounded-full transition-all', getProgressColor(overallProgress)]" :style="{ width: `${overallProgress}%` }" />
        </div>
      </div>
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">Objectives</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ goals.length }}</div>
        <div class="mt-1 text-xs text-slate-400">{{ goals.filter(g => g.progress >= 70).length }} on track</div>
      </div>
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">Key Results</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ goals.reduce((acc, g) => acc + (g.keyResults?.length || 0), 0) }}</div>
        <div class="mt-1 text-xs text-slate-400">Across all objectives</div>
      </div>
      <div class="rounded-xl border border-slate-700 bg-slate-800/60 p-4">
        <div class="text-xs uppercase text-slate-500">At Risk</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">{{ goals.filter(g => g.progress < 50).length }}</div>
        <div class="mt-1 text-xs text-slate-400">Need attention</div>
      </div>
    </div>

    <!-- Objectives List -->
    <div class="space-y-4">
      <div
        v-for="goal in goals"
        :key="goal.id"
        class="rounded-2xl border border-slate-700 bg-slate-800/60 overflow-hidden"
      >
        <!-- Objective Header -->
        <button
          class="w-full p-6 text-left hover:bg-slate-800/80 transition-colors"
          @click="toggleGoal(goal.id)"
        >
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 text-lg font-bold text-white">
                  O
                </div>
                <div>
                  <h3 class="text-lg font-semibold text-white">{{ goal.name }}</h3>
                  <p class="text-sm text-slate-400">{{ goal.keyResults?.length || 0 }} Key Results</p>
                </div>
              </div>
            </div>
            <div class="flex items-center gap-4">
              <div class="text-right">
                <div :class="['text-2xl font-bold', getProgressTextColor(goal.progress)]">{{ goal.progress }}%</div>
                <div class="text-xs text-slate-400">Progress</div>
              </div>
              <svg
                :class="['h-5 w-5 text-slate-400 transition-transform', selectedGoal === goal.id ? 'rotate-180' : '']"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </div>
          </div>

          <!-- Progress Bar -->
          <div class="mt-4 h-2 rounded-full bg-slate-700">
            <div
              :class="['h-full rounded-full transition-all', getProgressColor(goal.progress)]"
              :style="{ width: `${goal.progress}%` }"
            />
          </div>
        </button>

        <!-- Key Results (Expanded) -->
        <div v-if="selectedGoal === goal.id && goal.keyResults" class="border-t border-slate-700 bg-slate-900/40">
          <div class="p-4 space-y-3">
            <div
              v-for="kr in goal.keyResults"
              :key="kr.id"
              class="rounded-xl border border-slate-700 bg-slate-800/60 p-4"
            >
              <div class="flex items-start justify-between mb-3">
                <div class="flex items-center gap-3">
                  <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-500/20 text-sm font-bold text-blue-300">
                    KR
                  </div>
                  <div>
                    <h4 class="font-medium text-white">{{ kr.name }}</h4>
                    <p v-if="kr.targetValue !== undefined" class="text-xs text-slate-400">
                      {{ kr.currentValue }} / {{ kr.targetValue }} target
                    </p>
                  </div>
                </div>
                <span :class="['text-lg font-semibold', getProgressTextColor(kr.progress)]">
                  {{ kr.progress }}%
                </span>
              </div>
              <div class="h-1.5 rounded-full bg-slate-700">
                <div
                  :class="['h-full rounded-full transition-all', getProgressColor(kr.progress)]"
                  :style="{ width: `${kr.progress}%` }"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- AI Insights -->
    <div class="rounded-2xl border border-purple-500/30 bg-purple-500/10 p-6">
      <div class="flex items-start gap-4">
        <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-purple-500/20">
          <svg class="h-5 w-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
          </svg>
        </div>
        <div class="flex-1">
          <h3 class="font-semibold text-purple-300">AI Goal Insights</h3>
          <p class="mt-1 text-sm text-slate-300">
            Based on current trajectory, "Expand 20 accounts by $10k+" is at risk of missing target.
            Consider: (1) Focus on top 5 expansion-ready accounts, (2) Accelerate QBR scheduling,
            (3) Bundle upsell with renewal conversations.
          </p>
          <button class="mt-3 text-sm text-purple-400 hover:text-purple-300 flex items-center gap-1">
            View detailed analysis
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
