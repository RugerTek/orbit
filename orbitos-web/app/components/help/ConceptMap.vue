<script setup lang="ts">
/**
 * ConceptMap - Visual Entity Relationship Navigator
 *
 * Shows how OrbitOS concepts connect to each other
 * Built from entity relationships in specs
 * Click any node to learn more
 */

const { helpIndex, closeHelpPanel } = useHelp()
const route = useRoute()

const props = defineProps<{
  highlightConcept?: string
}>()

// Concept nodes with positions
const nodes = computed(() => [
  { id: 'ENT002', name: 'Organization', x: 50, y: 20, color: 'purple' },
  { id: 'ENT001', name: 'User', x: 20, y: 45, color: 'blue' },
  { id: 'ENT003', name: 'Role', x: 50, y: 45, color: 'emerald' },
  { id: 'ENT004', name: 'Function', x: 80, y: 45, color: 'amber' },
  { id: 'ENT005', name: 'Process', x: 35, y: 70, color: 'cyan' },
  { id: 'ENT006', name: 'Goal', x: 65, y: 70, color: 'pink' },
  { id: 'ENT007', name: 'Activity', x: 50, y: 90, color: 'orange' }
])

// Connections between concepts
const connections = [
  { from: 'ENT002', to: 'ENT001' },
  { from: 'ENT002', to: 'ENT003' },
  { from: 'ENT002', to: 'ENT004' },
  { from: 'ENT001', to: 'ENT003' },
  { from: 'ENT003', to: 'ENT004' },
  { from: 'ENT004', to: 'ENT005' },
  { from: 'ENT005', to: 'ENT007' },
  { from: 'ENT006', to: 'ENT007' },
  { from: 'ENT003', to: 'ENT005' }
]

// Detect current concept from route
const currentConcept = computed(() => {
  const path = route.path
  if (path.includes('people')) return 'ENT001'
  if (path.includes('roles')) return 'ENT003'
  if (path.includes('functions')) return 'ENT004'
  if (path.includes('processes')) return 'ENT005'
  if (path.includes('goals')) return 'ENT006'
  return props.highlightConcept || null
})

// Get node by ID
const getNode = (id: string) => nodes.value.find(n => n.id === id)

// Color classes
const getColorClasses = (color: string, isHighlighted: boolean) => {
  const baseColors: Record<string, { bg: string; border: string; text: string }> = {
    purple: { bg: 'bg-purple-500/20', border: 'border-purple-500', text: 'text-purple-300' },
    blue: { bg: 'bg-blue-500/20', border: 'border-blue-500', text: 'text-blue-300' },
    emerald: { bg: 'bg-emerald-500/20', border: 'border-emerald-500', text: 'text-emerald-300' },
    amber: { bg: 'bg-amber-500/20', border: 'border-amber-500', text: 'text-amber-300' },
    cyan: { bg: 'bg-cyan-500/20', border: 'border-cyan-500', text: 'text-cyan-300' },
    pink: { bg: 'bg-pink-500/20', border: 'border-pink-500', text: 'text-pink-300' },
    orange: { bg: 'bg-orange-500/20', border: 'border-orange-500', text: 'text-orange-300' }
  }

  const colors = baseColors[color] || baseColors.purple

  if (isHighlighted) {
    return `${colors.bg} ${colors.border} ${colors.text} ring-2 ring-${color}-500/50 scale-110`
  }

  return `${colors.bg} border-slate-600 ${colors.text}`
}

// Navigate to concept
const navigateToConcept = (conceptId: string) => {
  closeHelpPanel()
  navigateTo(`/help/concepts/${conceptId}`)
}
</script>

<template>
  <div class="relative w-full h-80 bg-slate-900/50 rounded-xl border border-slate-700 overflow-hidden">
    <!-- Title -->
    <div class="absolute top-3 left-3 z-10">
      <h4 class="text-sm font-semibold text-white">Concept Map</h4>
      <p class="text-xs text-slate-500">Click to explore</p>
    </div>

    <!-- Legend -->
    <div class="absolute top-3 right-3 z-10 flex items-center gap-2">
      <span class="flex items-center gap-1 text-xs text-slate-400">
        <span class="h-2 w-2 rounded-full bg-purple-500 animate-pulse"></span>
        Current
      </span>
    </div>

    <!-- SVG for connections -->
    <svg class="absolute inset-0 w-full h-full" style="pointer-events: none;">
      <defs>
        <marker
          id="arrowhead"
          markerWidth="10"
          markerHeight="7"
          refX="9"
          refY="3.5"
          orient="auto"
        >
          <polygon
            points="0 0, 10 3.5, 0 7"
            fill="#475569"
          />
        </marker>
      </defs>

      <!-- Connection lines -->
      <line
        v-for="(conn, index) in connections"
        :key="index"
        :x1="`${getNode(conn.from)?.x}%`"
        :y1="`${getNode(conn.from)?.y}%`"
        :x2="`${getNode(conn.to)?.x}%`"
        :y2="`${getNode(conn.to)?.y}%`"
        stroke="#475569"
        stroke-width="1.5"
        stroke-dasharray="4 2"
        class="transition-all duration-300"
        :class="[
          (currentConcept === conn.from || currentConcept === conn.to)
            ? 'stroke-purple-500 stroke-2'
            : ''
        ]"
      />
    </svg>

    <!-- Nodes -->
    <button
      v-for="node in nodes"
      :key="node.id"
      :style="{
        left: `${node.x}%`,
        top: `${node.y}%`,
        transform: 'translate(-50%, -50%)'
      }"
      :class="[
        'absolute flex flex-col items-center gap-1 p-2 rounded-lg border transition-all duration-300 hover:scale-110 cursor-pointer',
        getColorClasses(node.color, currentConcept === node.id)
      ]"
      @click="navigateToConcept(node.id)"
    >
      <!-- Icon -->
      <div
        :class="[
          'flex h-8 w-8 items-center justify-center rounded-lg',
          `bg-${node.color}-500/30`
        ]"
      >
        <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
          />
        </svg>
      </div>

      <!-- Label -->
      <span class="text-xs font-medium whitespace-nowrap">{{ node.name }}</span>

      <!-- Current indicator -->
      <span
        v-if="currentConcept === node.id"
        class="absolute -top-1 -right-1 flex h-3 w-3"
      >
        <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-purple-400 opacity-75"></span>
        <span class="relative inline-flex h-3 w-3 rounded-full bg-purple-500"></span>
      </span>
    </button>

    <!-- Instructions -->
    <div class="absolute bottom-3 left-1/2 -translate-x-1/2 text-center">
      <p class="text-xs text-slate-500">
        <span v-if="currentConcept">You are here: <strong class="text-slate-300">{{ getNode(currentConcept)?.name }}</strong></span>
        <span v-else>Click any concept to learn more</span>
      </p>
    </div>
  </div>
</template>
