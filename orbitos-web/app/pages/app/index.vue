<script setup lang="ts">
definePageMeta({
  layout: 'app'
})
</script>

<template>
  <div class="space-y-8">
    <div class="flex flex-col gap-2">
      <h1 class="text-2xl font-bold text-white">Operations Overview</h1>
      <p class="text-slate-400">A live snapshot of people, roles, processes, and resources.</p>
    </div>

    <!-- Stats Widget - loads independently -->
    <DashboardStatsWidget />

    <!-- Focus & Actions Widgets - load independently -->
    <div class="grid gap-4 lg:grid-cols-3">
      <div class="lg:col-span-2">
        <DashboardFocusWidget />
      </div>
      <DashboardActionsWidget />
    </div>
  </div>
</template>
