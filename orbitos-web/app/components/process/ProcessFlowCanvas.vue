<script setup lang="ts">
import { VueFlow, useVueFlow } from '@vue-flow/core'
import { Background } from '@vue-flow/background'
import { Controls } from '@vue-flow/controls'
import { MiniMap } from '@vue-flow/minimap'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import '@vue-flow/controls/dist/style.css'
import '@vue-flow/minimap/dist/style.css'

import type { ProcessWithActivities } from '~/types/operations'
import { useProcessFlow } from '~/composables/useProcessFlow'
import { useDebounceFn } from '@vueuse/core'

// Import custom nodes
import ActivityNode from './nodes/ActivityNode.vue'
import DecisionNode from './nodes/DecisionNode.vue'
import StartNode from './nodes/StartNode.vue'
import EndNode from './nodes/EndNode.vue'

const props = defineProps<{
  process: ProcessWithActivities
  isEditMode: boolean
}>()

const emit = defineEmits<{
  'node-click': [activityId: string]
  'node-double-click': [activityId: string]
  'positions-changed': [positions: Array<{ activityId: string; positionX: number; positionY: number }>]
  'edge-created': [edge: { sourceActivityId: string; targetActivityId: string; sourceHandle?: string }]
  'edge-deleted': [edgeId: string]
}>()

// Convert process to ref for composable
const processRef = computed(() => props.process)

// Get nodes and edges from composable
const { nodes: initialNodes, edges: initialEdges } = useProcessFlow(processRef)

// Vue Flow instance
const {
  nodes,
  edges,
  onNodesChange,
  onEdgesChange,
  onConnect,
  fitView,
  project,
} = useVueFlow({
  nodes: initialNodes.value,
  edges: initialEdges.value,
  fitViewOnInit: true,
  defaultEdgeOptions: {
    type: 'smoothstep',
    animated: false,
  },
})

// Watch for process changes and update nodes/edges
// Use a getter function that returns the activities length to ensure we detect new activities
watch(
  () => [props.process?.activities?.length, props.process?.activities?.map(a => a.id).join(',')],
  async () => {
    // Wait for next tick to ensure computed values are updated
    await nextTick()
    // Double-check the computed has recalculated by accessing it
    const newNodes = initialNodes.value
    const newEdges = initialEdges.value
    console.log('[ProcessFlowCanvas] Updating nodes:', newNodes.length, 'edges:', newEdges.length)
    nodes.value = newNodes
    edges.value = newEdges
    // Fit view after updating nodes
    setTimeout(() => fitView({ padding: 0.2 }), 50)
  },
  { deep: true, immediate: false }
)

// Custom node types
const nodeTypes = {
  activity: ActivityNode,
  decision: DecisionNode,
  start: StartNode,
  end: EndNode,
}

// Track positions that need to be saved
const pendingPositions = ref<Map<string, { x: number; y: number }>>(new Map())

// Debounced save for positions
const savePositions = useDebounceFn(() => {
  if (pendingPositions.value.size === 0) return

  const positions = Array.from(pendingPositions.value.entries())
    .filter(([id]) => id !== 'start' && id !== 'end') // Don't save start/end positions
    .map(([id, pos]) => ({
      activityId: id,
      positionX: pos.x,
      positionY: pos.y,
    }))

  if (positions.length > 0) {
    emit('positions-changed', positions)
  }
  pendingPositions.value.clear()
}, 500)

// Handle node drag end
const onNodeDragStop = (event: any) => {
  if (!props.isEditMode) return

  // Queue position updates
  for (const node of event.nodes) {
    if (node.id !== 'start' && node.id !== 'end') {
      pendingPositions.value.set(node.id, {
        x: node.position.x,
        y: node.position.y,
      })
    }
  }
  savePositions()
}

// Handle node click
const onNodeClick = (event: any) => {
  const nodeId = event.node.id
  if (nodeId !== 'start' && nodeId !== 'end') {
    emit('node-click', nodeId)
  }
}

// Handle node double click
const onNodeDoubleClick = (event: any) => {
  const nodeId = event.node.id
  if (nodeId !== 'start' && nodeId !== 'end') {
    emit('node-double-click', nodeId)
  }
}

// Handle new connection
const onConnectHandler = (params: any) => {
  if (!props.isEditMode) return

  // Don't allow connections from start or to end (those are managed automatically)
  if (params.source === 'start' || params.target === 'end') return

  emit('edge-created', {
    sourceActivityId: params.source,
    targetActivityId: params.target,
    sourceHandle: params.sourceHandle || undefined,
  })
}

// Handle edge removal
const onEdgesDelete = (deletedEdges: any[]) => {
  if (!props.isEditMode) return

  for (const edge of deletedEdges) {
    // Don't delete implicit start/end edges
    if (edge.id.startsWith('edge-start') || edge.id.startsWith('edge-end') || edge.id.includes('-end')) continue
    emit('edge-deleted', edge.id)
  }
}

// Fit view on mount
onMounted(() => {
  setTimeout(() => {
    fitView({ padding: 0.2 })
  }, 100)
})
</script>

<template>
  <div class="h-full w-full bg-gray-900/50 rounded-xl overflow-hidden">
    <VueFlow
      v-model:nodes="nodes"
      v-model:edges="edges"
      :node-types="nodeTypes"
      :nodes-draggable="isEditMode"
      :nodes-connectable="isEditMode"
      :edges-updatable="isEditMode"
      :pan-on-drag="true"
      :zoom-on-scroll="true"
      :zoom-on-pinch="true"
      :zoom-on-double-click="false"
      :prevent-scrolling="true"
      :min-zoom="0.2"
      :max-zoom="2"
      @node-drag-stop="onNodeDragStop"
      @node-click="onNodeClick"
      @node-double-click="onNodeDoubleClick"
      @connect="onConnectHandler"
      @edges-change="(changes) => changes.filter(c => c.type === 'remove').length > 0 && onEdgesDelete(changes.filter(c => c.type === 'remove').map(c => ({ id: c.id })))"
    >
      <!-- Background pattern -->
      <Background
        pattern-color="rgba(255, 255, 255, 0.03)"
        :gap="20"
      />

      <!-- Controls (zoom in/out, fit view) -->
      <Controls
        v-if="isEditMode"
        class="!bg-gray-800/80 !border-white/10 !rounded-lg [&>button]:!bg-transparent [&>button]:!border-white/10 [&>button:hover]:!bg-white/10"
      />

      <!-- Minimap -->
      <MiniMap
        class="!bg-gray-800/80 !border-white/10 !rounded-lg"
        :node-color="() => 'rgba(139, 92, 246, 0.6)'"
        :mask-color="'rgba(0, 0, 0, 0.5)'"
      />

      <!-- Edit mode indicator -->
      <div
        v-if="isEditMode"
        class="absolute top-4 left-4 z-10 px-3 py-1.5 rounded-lg bg-purple-500/20 border border-purple-500/30 text-purple-300 text-xs font-medium"
      >
        Edit Mode - Drag nodes to reposition
      </div>
    </VueFlow>
  </div>
</template>

<style>
/* Custom Vue Flow styles for dark theme */
.vue-flow__edge-path {
  stroke: rgba(255, 255, 255, 0.3);
  stroke-width: 2;
}

.vue-flow__edge.selected .vue-flow__edge-path {
  stroke: rgba(139, 92, 246, 0.8);
  stroke-width: 3;
}

.vue-flow__edge-text {
  fill: rgba(255, 255, 255, 0.7);
  font-size: 12px;
}

.vue-flow__connection-line {
  stroke: rgba(139, 92, 246, 0.5);
  stroke-width: 2;
  stroke-dasharray: 5;
}

.vue-flow__handle {
  opacity: 0;
  transition: opacity 0.2s;
}

.vue-flow__node:hover .vue-flow__handle,
.vue-flow__node.selected .vue-flow__handle {
  opacity: 1;
}

/* Make handles more visible in edit mode */
.vue-flow--editmode .vue-flow__handle {
  opacity: 0.7;
}
</style>
