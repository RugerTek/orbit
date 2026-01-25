<script setup lang="ts">
import type { CanvasType, CanvasScopeType, CanvasStatus } from '~/types/operations'

definePageMeta({
  layout: 'app',
})

const {
  isLoading,
  bmcCanvases,
  partners,
  channels,
  valuePropositions,
  customerRelationships,
  revenueStreams,
  fetchPartners,
  fetchChannels,
  fetchValuePropositions,
  fetchCustomerRelationships,
  fetchRevenueStreams,
  fetchBmcCanvases,
  createBmcCanvas,
} = useOperations()

// Modal state
const showCreateModal = ref(false)
const createForm = ref({
  name: '',
  description: '',
  canvasType: 'BusinessModel' as CanvasType,
  scopeType: 'Organization' as CanvasScopeType,
  productId: '',
  segmentId: '',
})

// Canvases data from API
const canvases = computed(() => bmcCanvases.value)
const isLoadingCanvases = ref(true)

// Load data on mount
onMounted(async () => {
  isLoadingCanvases.value = true
  try {
    await Promise.all([
      fetchBmcCanvases(),
      fetchPartners(),
      fetchChannels(),
      fetchValuePropositions(),
      fetchCustomerRelationships(),
      fetchRevenueStreams(),
    ])
  } finally {
    isLoadingCanvases.value = false
  }
})

// Computed: Company canvas (Organization scope)
// Note: API returns numeric enum (0=Organization, 1=Product, 2=Segment, 3=Initiative)
const companyCanvas = computed(() => {
  return canvases.value.find(c => c.scopeType === 'Organization' || c.scopeType === 0)
})

// Computed: Product canvases
const productCanvases = computed(() => {
  return canvases.value.filter(c => c.scopeType === 'Product' || c.scopeType === 1)
})

// Computed: Other canvases (Segment, Initiative)
const otherCanvases = computed(() => {
  return canvases.value.filter(c =>
    c.scopeType === 'Segment' || c.scopeType === 2 ||
    c.scopeType === 'Initiative' || c.scopeType === 3
  )
})

// Stats for company canvas preview
const companyStats = computed(() => ({
  partners: partners.value.length,
  channels: channels.value.length,
  valueProps: valuePropositions.value.length,
  relationships: customerRelationships.value.length,
  revenue: revenueStreams.value.length,
  total: partners.value.length + channels.value.length + valuePropositions.value.length + customerRelationships.value.length + revenueStreams.value.length,
}))

// Scope type display
const scopeTypeLabel = (scope: CanvasScopeType) => {
  const labels: Record<CanvasScopeType, string> = {
    Organization: 'Company',
    Product: 'Product',
    Segment: 'Segment',
    Initiative: 'Initiative',
  }
  return labels[scope] || scope
}

// Status color
const statusColor = (status: CanvasStatus) => {
  const colors: Record<CanvasStatus, string> = {
    Draft: 'bg-amber-500/20 text-amber-300 border-amber-500/30',
    Active: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30',
    Archived: 'bg-slate-500/20 text-slate-300 border-slate-500/30',
  }
  return colors[status] || colors.Draft
}

// Navigate to canvas
const openCanvas = (canvasId: string) => {
  navigateTo(`/app/business-canvas?canvasId=${canvasId}`)
}

// Create canvas
const isSaving = ref(false)
const handleCreateCanvas = async () => {
  if (!createForm.value.name.trim()) return

  isSaving.value = true
  try {
    const newCanvas = await createBmcCanvas({
      name: createForm.value.name,
      description: createForm.value.description,
      canvasType: createForm.value.canvasType,
      scopeType: createForm.value.scopeType,
      status: 'Draft',
      productId: createForm.value.productId || undefined,
      segmentId: createForm.value.segmentId || undefined,
    })

    showCreateModal.value = false

    // Reset form
    createForm.value = {
      name: '',
      description: '',
      canvasType: 'BusinessModel',
      scopeType: 'Product',
      productId: '',
      segmentId: '',
    }

    // Navigate to the new canvas
    openCanvas(newCanvas.id)
  } finally {
    isSaving.value = false
  }
}

// Close modal
const closeModal = () => {
  showCreateModal.value = false
  createForm.value = {
    name: '',
    description: '',
    canvasType: 'BusinessModel',
    scopeType: 'Product',
    productId: '',
    segmentId: '',
  }
}

// Block mini-preview colors
const blockColors = {
  partners: 'bg-blue-500/30',
  activities: 'bg-purple-500/30',
  resources: 'bg-emerald-500/30',
  propositions: 'bg-amber-500/30',
  relationships: 'bg-cyan-500/30',
  channels: 'bg-pink-500/30',
  segments: 'bg-orange-500/30',
  costs: 'bg-red-500/30',
  revenue: 'bg-teal-500/30',
}
</script>

<template>
  <div class="space-y-8">
    <!-- Header -->
    <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Business Model Canvases</h1>
        <p class="orbitos-text">Strategic business model planning across your organization and products</p>
      </div>
      <div class="flex items-center gap-3">
        <KnowledgeBaseGuideButton
          article-id="canvas/business-model-canvas"
          label="Canvas Guide"
        />
        <button
          class="orbitos-btn-primary px-6 py-3 text-sm"
          @click="showCreateModal = true"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Canvas
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoadingCanvases" class="flex items-center justify-center py-20">
      <div class="orbitos-spinner orbitos-spinner-lg"></div>
    </div>

    <template v-else>
      <!-- Company Canvas Section (Hero) -->
      <section v-if="companyCanvas">
        <div class="mb-4 flex items-center gap-2">
          <div class="h-3 w-3 rounded-full bg-gradient-to-r from-purple-500 to-blue-500"></div>
          <h2 class="text-lg font-semibold text-white">Company Canvas</h2>
          <span class="rounded-full bg-purple-500/20 border border-purple-500/30 px-2 py-0.5 text-xs text-purple-300">Master</span>
        </div>

        <div
          class="group relative cursor-pointer overflow-hidden rounded-2xl border border-purple-500/30 bg-gradient-to-br from-purple-500/10 to-blue-500/10 p-6 transition-all duration-300 hover:border-purple-500/50 hover:shadow-lg hover:shadow-purple-500/20"
          @click="openCanvas(companyCanvas.id)"
        >
          <!-- Glow effect on hover -->
          <div class="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-300 bg-gradient-to-br from-purple-500/5 to-blue-500/5"></div>

          <div class="relative flex flex-col lg:flex-row lg:items-start gap-6">
            <!-- Canvas Info -->
            <div class="flex-1">
              <div class="flex items-start justify-between mb-4">
                <div>
                  <h3 class="text-xl font-bold text-white group-hover:text-purple-200 transition-colors">
                    {{ companyCanvas.name }}
                  </h3>
                  <p class="text-sm text-white/60 mt-1">{{ companyCanvas.description }}</p>
                </div>
                <span :class="['rounded-full border px-3 py-1 text-xs font-medium', statusColor(companyCanvas.status)]">
                  {{ companyCanvas.status }}
                </span>
              </div>

              <!-- Quick Stats -->
              <div class="flex flex-wrap gap-3">
                <div class="flex items-center gap-2 rounded-lg bg-white/5 px-3 py-2">
                  <div class="h-2 w-2 rounded-full bg-blue-400"></div>
                  <span class="text-xs text-white/60">Partners</span>
                  <span class="text-sm font-semibold text-white">{{ companyStats.partners }}</span>
                </div>
                <div class="flex items-center gap-2 rounded-lg bg-white/5 px-3 py-2">
                  <div class="h-2 w-2 rounded-full bg-pink-400"></div>
                  <span class="text-xs text-white/60">Channels</span>
                  <span class="text-sm font-semibold text-white">{{ companyStats.channels }}</span>
                </div>
                <div class="flex items-center gap-2 rounded-lg bg-white/5 px-3 py-2">
                  <div class="h-2 w-2 rounded-full bg-amber-400"></div>
                  <span class="text-xs text-white/60">Value Props</span>
                  <span class="text-sm font-semibold text-white">{{ companyStats.valueProps }}</span>
                </div>
                <div class="flex items-center gap-2 rounded-lg bg-white/5 px-3 py-2">
                  <div class="h-2 w-2 rounded-full bg-cyan-400"></div>
                  <span class="text-xs text-white/60">Relationships</span>
                  <span class="text-sm font-semibold text-white">{{ companyStats.relationships }}</span>
                </div>
                <div class="flex items-center gap-2 rounded-lg bg-white/5 px-3 py-2">
                  <div class="h-2 w-2 rounded-full bg-teal-400"></div>
                  <span class="text-xs text-white/60">Revenue</span>
                  <span class="text-sm font-semibold text-white">{{ companyStats.revenue }}</span>
                </div>
              </div>
            </div>

            <!-- Mini BMC Preview -->
            <div class="hidden lg:block w-64 flex-shrink-0">
              <div class="rounded-xl border border-white/10 bg-black/20 p-3">
                <div class="grid gap-1" style="grid-template-columns: 1fr 1fr 2fr 1fr 1fr; grid-template-rows: 1fr 1fr 1fr;">
                  <!-- Partners -->
                  <div :class="['row-span-2 rounded', blockColors.partners]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Partners</div>
                  </div>
                  <!-- Activities -->
                  <div :class="['rounded', blockColors.activities]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Activities</div>
                  </div>
                  <!-- Value Props -->
                  <div :class="['row-span-2 rounded', blockColors.propositions]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Value</div>
                  </div>
                  <!-- Relationships -->
                  <div :class="['rounded', blockColors.relationships]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Relations</div>
                  </div>
                  <!-- Segments -->
                  <div :class="['row-span-2 rounded', blockColors.segments]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Segments</div>
                  </div>
                  <!-- Resources -->
                  <div :class="['rounded', blockColors.resources]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Resources</div>
                  </div>
                  <!-- Channels -->
                  <div :class="['rounded', blockColors.channels]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Channels</div>
                  </div>
                  <!-- Costs -->
                  <div :class="['col-span-2 rounded', blockColors.costs]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Costs</div>
                  </div>
                  <!-- Revenue -->
                  <div :class="['col-span-3 rounded', blockColors.revenue]">
                    <div class="p-1.5 text-[8px] text-white/70 font-medium">Revenue</div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Arrow indicator -->
          <div class="absolute right-6 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity">
            <svg class="h-6 w-6 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </div>
        </div>
      </section>

      <!-- No Company Canvas - Prompt to create -->
      <section v-else class="rounded-2xl border border-dashed border-purple-500/30 bg-purple-500/5 p-8 text-center">
        <div class="mx-auto max-w-md">
          <div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-purple-500 to-blue-500">
            <svg class="h-8 w-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
            </svg>
          </div>
          <h3 class="text-lg font-semibold text-white mb-2">Create Your Company Canvas</h3>
          <p class="text-sm text-white/60 mb-6">
            Start by creating a master company-level business model canvas. This will define shared resources, partners, and capabilities that can be linked to product canvases.
          </p>
          <button
            class="orbitos-btn-primary px-6 py-3 text-sm"
            @click="createForm.scopeType = 'Organization'; showCreateModal = true"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Create Company Canvas
          </button>
        </div>
      </section>

      <!-- Product Canvases Section -->
      <section v-if="productCanvases.length > 0 || companyCanvas">
        <div class="mb-4 flex items-center justify-between">
          <div class="flex items-center gap-2">
            <h2 class="text-lg font-semibold text-white">Product Canvases</h2>
            <span class="rounded-full bg-white/10 px-2 py-0.5 text-xs text-white/60">{{ productCanvases.length }}</span>
          </div>
          <button
            class="flex items-center gap-2 rounded-lg border border-white/10 bg-white/5 px-3 py-2 text-sm text-white/70 hover:bg-white/10 hover:text-white transition-colors"
            @click="createForm.scopeType = 'Product'; showCreateModal = true"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Add Product Canvas
          </button>
        </div>

        <!-- Product Canvas Grid -->
        <div v-if="productCanvases.length > 0" class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="canvas in productCanvases"
            :key="canvas.id"
            class="group cursor-pointer rounded-xl border border-white/10 bg-white/5 p-5 transition-all duration-200 hover:border-white/20 hover:bg-white/10"
            @click="openCanvas(canvas.id)"
          >
            <div class="flex items-start justify-between mb-3">
              <div>
                <h3 class="font-semibold text-white group-hover:text-purple-200 transition-colors">{{ canvas.name }}</h3>
                <p v-if="canvas.productName" class="text-xs text-white/40 mt-0.5">{{ canvas.productName }}</p>
              </div>
              <span :class="['rounded-full border px-2 py-0.5 text-xs', statusColor(canvas.status)]">
                {{ canvas.status }}
              </span>
            </div>
            <p v-if="canvas.description" class="text-sm text-white/60 mb-4 line-clamp-2">{{ canvas.description }}</p>

            <!-- Mini preview placeholder -->
            <div class="rounded-lg border border-white/10 bg-black/20 p-2">
              <div class="grid gap-0.5" style="grid-template-columns: repeat(5, 1fr); grid-template-rows: repeat(3, 12px);">
                <div class="row-span-2 rounded-sm bg-blue-500/30"></div>
                <div class="rounded-sm bg-purple-500/30"></div>
                <div class="row-span-2 rounded-sm bg-amber-500/30"></div>
                <div class="rounded-sm bg-cyan-500/30"></div>
                <div class="row-span-2 rounded-sm bg-orange-500/30"></div>
                <div class="rounded-sm bg-emerald-500/30"></div>
                <div class="rounded-sm bg-pink-500/30"></div>
                <div class="col-span-2 rounded-sm bg-red-500/30"></div>
                <div class="col-span-3 rounded-sm bg-teal-500/30"></div>
              </div>
            </div>

            <div class="mt-3 flex items-center justify-between text-xs text-white/40">
              <span>Updated {{ new Date(canvas.updatedAt).toLocaleDateString() }}</span>
              <svg class="h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </div>
          </div>
        </div>

        <!-- Empty state for product canvases -->
        <div v-else class="rounded-xl border border-dashed border-white/10 bg-white/5 p-8 text-center">
          <div class="mx-auto max-w-sm">
            <svg class="mx-auto h-12 w-12 text-white/20 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
            <h3 class="text-sm font-medium text-white mb-2">No Product Canvases Yet</h3>
            <p class="text-xs text-white/50 mb-4">
              Create canvases for each of your products to define their unique commercial strategy while linking shared resources from the company canvas.
            </p>
            <button
              class="text-sm text-purple-400 hover:text-purple-300 transition-colors"
              @click="createForm.scopeType = 'Product'; showCreateModal = true"
            >
              + Create your first product canvas
            </button>
          </div>
        </div>
      </section>

      <!-- Other Canvases (Segment, Initiative) -->
      <section v-if="otherCanvases.length > 0">
        <div class="mb-4 flex items-center gap-2">
          <h2 class="text-lg font-semibold text-white">Other Canvases</h2>
          <span class="rounded-full bg-white/10 px-2 py-0.5 text-xs text-white/60">{{ otherCanvases.length }}</span>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="canvas in otherCanvases"
            :key="canvas.id"
            class="group cursor-pointer rounded-xl border border-white/10 bg-white/5 p-5 transition-all duration-200 hover:border-white/20 hover:bg-white/10"
            @click="openCanvas(canvas.id)"
          >
            <div class="flex items-start justify-between mb-2">
              <div>
                <span class="text-xs text-white/40">{{ scopeTypeLabel(canvas.scopeType) }}</span>
                <h3 class="font-semibold text-white">{{ canvas.name }}</h3>
              </div>
              <span :class="['rounded-full border px-2 py-0.5 text-xs', statusColor(canvas.status)]">
                {{ canvas.status }}
              </span>
            </div>
            <p v-if="canvas.description" class="text-sm text-white/60 line-clamp-2">{{ canvas.description }}</p>
          </div>
        </div>
      </section>
    </template>

    <!-- Create Canvas Modal -->
    <BaseDialog
      v-model="showCreateModal"
      size="lg"
      title="Create New Canvas"
      @close="closeModal"
      @submit="handleCreateCanvas"
    >
      <div class="space-y-5">
        <!-- Scope Type Selection -->
        <div>
          <label class="orbitos-label">Canvas Type</label>
          <div class="grid grid-cols-2 gap-3">
            <button
              v-for="scope in [
                { value: 'Organization', label: 'Company', desc: 'Master organizational canvas', icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4', disabled: !!companyCanvas },
                { value: 'Product', label: 'Product', desc: 'Product-specific strategy', icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4', disabled: false },
                { value: 'Segment', label: 'Segment', desc: 'Customer segment focus', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z', disabled: false },
                { value: 'Initiative', label: 'Initiative', desc: 'Strategic experiment', icon: 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z', disabled: false },
              ]"
              :key="scope.value"
              type="button"
              :disabled="scope.disabled"
              :class="[
                'relative rounded-xl border p-4 text-left transition-all',
                createForm.scopeType === scope.value
                  ? 'border-purple-500 bg-purple-500/10'
                  : scope.disabled
                    ? 'border-white/5 bg-white/5 opacity-50 cursor-not-allowed'
                    : 'border-white/10 bg-white/5 hover:border-white/20'
              ]"
              @click="!scope.disabled && (createForm.scopeType = scope.value as CanvasScopeType)"
            >
              <div class="flex items-start gap-3">
                <svg class="h-5 w-5 text-purple-400 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="scope.icon" />
                </svg>
                <div>
                  <div class="font-medium text-white text-sm">{{ scope.label }}</div>
                  <div class="text-xs text-white/50 mt-0.5">{{ scope.desc }}</div>
                </div>
              </div>
              <div v-if="scope.disabled && scope.value === 'Organization'" class="absolute top-2 right-2">
                <span class="rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 px-2 py-0.5 text-[10px]">Exists</span>
              </div>
            </button>
          </div>
        </div>

        <!-- Name -->
        <div>
          <label class="orbitos-label">Canvas Name *</label>
          <input
            v-model="createForm.name"
            type="text"
            class="orbitos-input"
            :placeholder="createForm.scopeType === 'Organization' ? 'Company Business Model' : 'e.g., Product X Canvas'"
            autofocus
          />
        </div>

        <!-- Description -->
        <div>
          <label class="orbitos-label">Description</label>
          <textarea
            v-model="createForm.description"
            rows="2"
            class="orbitos-input"
            placeholder="Brief description of this canvas's purpose"
          ></textarea>
        </div>

        <!-- Smart suggestion -->
        <div v-if="createForm.scopeType === 'Product' && companyCanvas" class="rounded-xl bg-purple-500/10 border border-purple-500/30 p-4">
          <div class="flex items-start gap-3">
            <svg class="h-5 w-5 text-purple-400 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
            </svg>
            <div>
              <div class="text-sm font-medium text-purple-200">Smart Start</div>
              <div class="text-xs text-purple-300/70 mt-1">
                After creating, you'll be able to link shared items from your Company Canvas ({{ companyStats.total }} items available).
              </div>
            </div>
          </div>
        </div>
      </div>

      <template #footer="{ close }">
        <div class="flex justify-end gap-3">
          <button
            type="button"
            class="rounded-lg px-4 py-2.5 text-sm text-white/70 hover:text-white transition-colors"
            @click="close"
          >
            Cancel
          </button>
          <button
            type="button"
            class="orbitos-btn-primary px-6 py-2.5 text-sm"
            :disabled="isSaving || !createForm.name.trim()"
            @click="handleCreateCanvas"
          >
            {{ isSaving ? 'Creating...' : 'Create Canvas' }}
          </button>
        </div>
      </template>
    </BaseDialog>
  </div>
</template>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
