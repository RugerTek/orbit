<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

import type { OrgChartResource, OrgChartViewMode } from '~/types/operations'

const {
  orgChart,
  orgChartMetrics,
  isLoading,
  fetchOrgChart,
  fetchOrgChartMetrics,
  people,
  fetchPeople,
  resourceSubtypes,
  fetchResourceSubtypes,
} = useOperations()

// View mode toggle
const viewMode = ref<OrgChartViewMode>('tree')

// Dialog states
const showAddVacancyDialog = ref(false)
const showEditReportingDialog = ref(false)
const selectedPerson = ref<OrgChartResource | null>(null)

onMounted(async () => {
  await Promise.all([
    fetchOrgChart(),
    fetchOrgChartMetrics(),
    fetchPeople(),
    fetchResourceSubtypes(),
  ])
})

// Flatten tree for list/card views
const flattenedPeople = computed(() => {
  if (!orgChart.value) return []
  const result: OrgChartResource[] = []

  const flatten = (nodes: OrgChartResource[], depth = 0) => {
    for (const node of nodes) {
      result.push({ ...node, managementDepth: depth })
      if (node.directReports?.length) {
        flatten(node.directReports, depth + 1)
      }
    }
  }

  flatten(orgChart.value.rootNodes)
  return result
})

// Get person subtypes for vacancy creation
const personSubtypes = computed(() =>
  resourceSubtypes.value.filter(s => s.resourceType === 'person')
)

const handlePersonSelect = (person: OrgChartResource) => {
  selectedPerson.value = person
  showEditReportingDialog.value = true
}

const handleVacancyCreated = () => {
  showAddVacancyDialog.value = false
}

const handleReportingUpdated = () => {
  showEditReportingDialog.value = false
  selectedPerson.value = null
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Organization Chart</h1>
        <p class="text-sm text-white/60">Visualize reporting relationships and span of control.</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- View Toggle -->
        <div class="flex rounded-lg bg-white/5 p-1">
          <button
            v-for="mode in (['tree', 'list', 'card'] as const)"
            :key="mode"
            @click="viewMode = mode"
            :class="[
              'px-3 py-1.5 text-sm rounded-md transition-colors capitalize',
              viewMode === mode
                ? 'bg-purple-500/30 text-white'
                : 'text-white/60 hover:text-white'
            ]"
          >
            {{ mode }}
          </button>
        </div>
        <button
          @click="showAddVacancyDialog = true"
          class="orbitos-btn-primary py-2 px-4 text-sm"
        >
          Add Vacancy
        </button>
      </div>
    </div>

    <!-- Metrics Cards -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-glass-subtle rounded-xl p-4">
        <div class="text-xs uppercase text-white/40">Total People</div>
        <div class="mt-1 text-2xl font-semibold text-white">
          {{ orgChartMetrics?.totalPeople || 0 }}
        </div>
      </div>
      <div class="orbitos-glass-subtle rounded-xl p-4">
        <div class="text-xs uppercase text-white/40">Open Vacancies</div>
        <div class="mt-1 text-2xl font-semibold text-amber-300">
          {{ orgChartMetrics?.totalVacancies || 0 }}
        </div>
      </div>
      <div class="orbitos-glass-subtle rounded-xl p-4">
        <div class="text-xs uppercase text-white/40">Org Depth</div>
        <div class="mt-1 text-2xl font-semibold text-white">
          {{ orgChartMetrics?.maxDepth || 0 }} levels
        </div>
      </div>
      <div class="orbitos-glass-subtle rounded-xl p-4">
        <div class="text-xs uppercase text-white/40">Avg Span of Control</div>
        <div class="mt-1 text-2xl font-semibold text-white">
          {{ (orgChartMetrics?.averageSpanOfControl || 0).toFixed(1) }}
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="!orgChart?.rootNodes?.length" class="orbitos-glass-subtle rounded-xl p-12 text-center">
      <div class="text-4xl mb-4">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16 mx-auto text-white/20" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
        </svg>
      </div>
      <p class="text-white/70">No organizational structure defined yet.</p>
      <p class="text-sm text-white/40 mt-2">Add people and set their reporting relationships to build your org chart.</p>
      <button
        @click="showAddVacancyDialog = true"
        class="orbitos-btn-primary mt-6 py-2 px-4 text-sm"
      >
        Add First Position
      </button>
    </div>

    <!-- Views -->
    <template v-else>
      <!-- Tree View -->
      <OrgChartTree
        v-if="viewMode === 'tree'"
        :nodes="orgChart.rootNodes"
        @select="handlePersonSelect"
      />

      <!-- List View -->
      <OrgChartList
        v-else-if="viewMode === 'list'"
        :people="flattenedPeople"
        @select="handlePersonSelect"
      />

      <!-- Card View -->
      <OrgChartCards
        v-else
        :people="flattenedPeople"
        @select="handlePersonSelect"
      />
    </template>

    <!-- Dialogs -->
    <OrgChartVacancyDialog
      v-model="showAddVacancyDialog"
      :people="flattenedPeople"
      :subtypes="personSubtypes"
      @created="handleVacancyCreated"
    />
    <OrgChartReportingDialog
      v-model="showEditReportingDialog"
      :person="selectedPerson"
      :people="flattenedPeople"
      @updated="handleReportingUpdated"
    />
  </div>
</template>
