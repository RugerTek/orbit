<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { resources, isLoading, fetchResources } = useOperations()

// Fetch resources on mount
onMounted(async () => {
  await fetchResources()
})

// Status colors
const statusColor = (status: string) => {
  switch (status) {
    case 'active': return 'bg-emerald-500/20 text-emerald-300'
    case 'inactive': return 'bg-amber-500/20 text-amber-300'
    case 'archived': return 'bg-slate-500/20 text-slate-300'
    default: return 'bg-blue-500/20 text-blue-300'
  }
}

// Resource type icons
const resourceTypeLabel = (type: string) => {
  switch (type) {
    case 'person': return 'Person'
    case 'tool': return 'Tool'
    case 'partner': return 'Partner'
    case 'asset': return 'Asset'
    default: return type
  }
}

// Group resources by type
const groupedResources = computed(() => {
  const groups: Record<string, typeof resources.value> = {}
  for (const resource of resources.value) {
    const type = resource.resourceType || 'other'
    if (!groups[type]) groups[type] = []
    groups[type].push(resource)
  }
  return groups
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Resource Registry</h1>
        <p class="orbitos-text">People, tools, partners, and assets powering operations.</p>
      </div>
      <button type="button" class="orbitos-btn-primary py-2 px-4 text-sm">
        Add Resource
      </button>
    </div>

    <!-- Stats -->
    <div class="grid gap-4 md:grid-cols-4">
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Total Resources</div>
        <div class="mt-1 text-2xl font-semibold text-white">{{ resources.length }}</div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">People</div>
        <div class="mt-1 text-2xl font-semibold text-purple-300">
          {{ resources.filter(r => r.resourceType === 'person').length }}
        </div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Tools</div>
        <div class="mt-1 text-2xl font-semibold text-blue-300">
          {{ resources.filter(r => r.resourceType === 'tool').length }}
        </div>
      </div>
      <div class="orbitos-card-static">
        <div class="text-xs uppercase text-white/40">Active</div>
        <div class="mt-1 text-2xl font-semibold text-emerald-300">
          {{ resources.filter(r => r.status === 'active').length }}
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <div class="orbitos-spinner orbitos-spinner-md"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="resources.length === 0" class="orbitos-card-static p-12 text-center">
      <div class="orbitos-text">No resources registered yet.</div>
      <button type="button" class="mt-4 rounded-lg bg-purple-500/20 px-4 py-2 text-sm text-purple-300 hover:bg-purple-500/30 transition-colors">
        Add your first resource
      </button>
    </div>

    <!-- Resources Table -->
    <div v-else class="orbitos-glass-subtle overflow-hidden">
      <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <div class="text-sm text-white/70">All Resources</div>
        <div class="text-xs text-white/40">{{ resources.length }} total</div>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-black/20 text-xs uppercase text-white/40">
            <tr>
              <th class="px-6 py-3">Resource</th>
              <th class="px-6 py-3">Type</th>
              <th class="px-6 py-3">Description</th>
              <th class="px-6 py-3">Status</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/10">
            <tr v-for="resource in resources" :key="resource.id" class="hover:bg-white/5 transition-colors">
              <td class="px-6 py-4 text-white font-semibold">{{ resource.name }}</td>
              <td class="px-6 py-4 text-white/70">
                <span class="rounded bg-white/10 px-2 py-1 text-xs">
                  {{ resourceTypeLabel(resource.resourceType) }}
                </span>
              </td>
              <td class="px-6 py-4 text-white/60 max-w-xs truncate">{{ resource.description || '-' }}</td>
              <td class="px-6 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-medium', statusColor(resource.status)]">
                  {{ resource.status }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
