<script setup lang="ts">
import { VueFlow, useVueFlow, type NodeTypesObject } from '@vue-flow/core'
import { markRaw } from 'vue'
import { Background } from '@vue-flow/background'
import { Controls } from '@vue-flow/controls'
import { MiniMap } from '@vue-flow/minimap'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import '@vue-flow/controls/dist/style.css'
import '@vue-flow/minimap/dist/style.css'

import type { ProcessWithActivities } from '~/types/operations'
import { useProcessFlow, type ProcessFlowEdge } from '~/composables/useProcessFlow'
import { useDebounceFn } from '@vueuse/core'

// Import custom nodes
import ActivityNode from './nodes/ActivityNode.vue'
import DecisionNode from './nodes/DecisionNode.vue'
import StartNode from './nodes/StartNode.vue'
import EndNode from './nodes/EndNode.vue'
// IE (Industrial Engineering) symbol nodes
import OperationNode from './nodes/OperationNode.vue'
import InspectionNode from './nodes/InspectionNode.vue'
import TransportNode from './nodes/TransportNode.vue'
import DelayNode from './nodes/DelayNode.vue'
import StorageNode from './nodes/StorageNode.vue'
import DocumentNode from './nodes/DocumentNode.vue'
import DatabaseNode from './nodes/DatabaseNode.vue'
import ManualInputNode from './nodes/ManualInputNode.vue'
import DisplayNode from './nodes/DisplayNode.vue'

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
  'implicit-edge-deleted': [edge: { sourceActivityId: string; targetActivityId: string }]
}>()

// Convert process to ref for composable
const processRef = computed(() => props.process)

// Get nodes and edges from composable
const { nodes: initialNodes, edges: initialEdges } = useProcessFlow(processRef)

// Create a key that changes ONLY when the process ID changes
// This prevents zoom reset when activities are added/removed
const flowKey = computed(() => {
  return `flow-${props.process?.id}`
})

// Use VueFlow composable to update nodes and edges without re-mounting
const { setEdges, setNodes, getEdges, applyEdgeChanges } = useVueFlow()

// Watch for node changes (activities added/removed) and update VueFlow's internal state
// This prevents the component from re-mounting (which would reset zoom)
watch(
  () => props.process?.activities?.map(a => a.id).sort().join(',') || '',
  () => {
    // Use nextTick to ensure the computed initialNodes has updated
    nextTick(() => {
      console.log('[ProcessFlowCanvas] Updating nodes:', initialNodes.value.length, 'activities')
      setNodes(initialNodes.value as any)
    })
  }
)

// Watch for edge changes and update VueFlow's internal state
// Watch both edges array and useExplicitFlow flag since either can affect computed edges
watch(
  () => ({
    edgeIds: props.process?.edges?.map(e => e.id).join(',') || '',
    useExplicitFlow: props.process?.useExplicitFlow,
    entryActivityId: props.process?.entryActivityId,
    exitActivityId: props.process?.exitActivityId,
  }),
  () => {
    // Use nextTick to ensure the computed initialEdges has updated
    nextTick(() => {
      console.log('[ProcessFlowCanvas] Updating edges:', initialEdges.value.length, 'useExplicitFlow:', props.process?.useExplicitFlow)
      setEdges(initialEdges.value as any)
    })
  },
  { deep: true }
)

// Custom node types - use markRaw to prevent Vue from making components reactive
// This is required by VueFlow to avoid "component made reactive" warnings and potential issues
const nodeTypes: NodeTypesObject = {
  activity: markRaw(ActivityNode),
  decision: markRaw(DecisionNode),
  start: markRaw(StartNode),
  end: markRaw(EndNode),
  // IE (Industrial Engineering) symbol nodes
  operation: markRaw(OperationNode),
  inspection: markRaw(InspectionNode),
  transport: markRaw(TransportNode),
  delay: markRaw(DelayNode),
  storage: markRaw(StorageNode),
  document: markRaw(DocumentNode),
  database: markRaw(DatabaseNode),
  manualInput: markRaw(ManualInputNode),
  display: markRaw(DisplayNode),
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

  // Allow connections from start and to end - these are user-managed
  // Note: start/end nodes use special IDs that the parent component should handle
  emit('edge-created', {
    sourceActivityId: params.source,
    targetActivityId: params.target,
    sourceHandle: params.sourceHandle || undefined,
  })
}

// Keep track of edges for lookup when VueFlow removes them
const lastKnownEdges = ref<Map<string, { source: string; target: string }>>(new Map())

// Update edge cache whenever initialEdges changes
// Clear old entries and add new ones to keep cache in sync
watch(initialEdges, (newEdges) => {
  // Build set of current edge IDs
  const currentIds = new Set(newEdges.map(e => (e as any).id as string).filter((id): id is string => !!id))

  // Remove edges no longer present
  for (const id of lastKnownEdges.value.keys()) {
    if (!currentIds.has(id)) {
      lastKnownEdges.value.delete(id)
    }
  }

  // Add/update current edges
  newEdges.forEach((edge) => {
    const e = edge as any
    if (e.id && e.source && e.target) {
      lastKnownEdges.value.set(e.id, { source: e.source, target: e.target })
    }
  })
}, { immediate: true, deep: true })

// Handle edge changes from VueFlow
const handleEdgesChange = (changes: any[]) => {
  if (!props.isEditMode) {
    // Not in edit mode - apply all changes (shouldn't have remove changes anyway)
    applyEdgeChanges(changes)
    return
  }

  const removeChanges = changes.filter(c => c.type === 'remove')
  const otherChanges = changes.filter(c => c.type !== 'remove')

  // Apply non-remove changes immediately
  if (otherChanges.length > 0) {
    applyEdgeChanges(otherChanges)
  }

  if (removeChanges.length === 0) return

  console.log('[ProcessFlowCanvas] Edge changes:', removeChanges.length, 'removals')
  console.log('[ProcessFlowCanvas] Process useExplicitFlow:', props.process.useExplicitFlow)
  console.log('[ProcessFlowCanvas] Cached edges:', lastKnownEdges.value.size)

  // Get edges from multiple sources - VueFlow may have already removed them
  const vueFlowEdges = getEdges.value
  const computedEdges = initialEdges.value as any[]

  for (const change of removeChanges) {
    const edgeId = change.id
    console.log('[ProcessFlowCanvas] Processing removal of edge:', edgeId)

    // Look up the actual edge to get source/target - check multiple sources
    let edgeData = lastKnownEdges.value.get(edgeId)
    if (!edgeData) {
      const vfEdge = vueFlowEdges.find(e => e.id === edgeId)
      if (vfEdge) {
        edgeData = { source: vfEdge.source, target: vfEdge.target }
      }
    }
    if (!edgeData) {
      const compEdge = computedEdges.find(e => e.id === edgeId)
      if (compEdge) {
        edgeData = { source: compEdge.source, target: compEdge.target }
      }
    }

    // Implicit edges have IDs that start with 'edge-' (e.g., 'edge-start', 'edge-end', 'edge-{activityId}-{activityId}')
    // Real database edges have GUID IDs
    const isImplicitEdge = edgeId.startsWith('edge-')
    console.log('[ProcessFlowCanvas] Edge is implicit:', isImplicitEdge, 'found data:', !!edgeData)

    if (isImplicitEdge) {
      // Emit implicit-edge-deleted so parent can switch to explicit mode
      // This edge doesn't exist in DB, but we need to tell the backend to switch modes
      if (edgeData) {
        console.log('[ProcessFlowCanvas] Emitting implicit-edge-deleted:', edgeId, edgeData.source, edgeData.target)
        emit('implicit-edge-deleted', {
          sourceActivityId: edgeData.source,
          targetActivityId: edgeData.target,
        })
        // Apply the removal change to VueFlow AFTER emitting
        // This ensures the edge is removed from the UI immediately
        applyEdgeChanges([change])
      } else {
        console.error('[ProcessFlowCanvas] Could not find edge data for:', edgeId)
      }
      // Remove from cache
      lastKnownEdges.value.delete(edgeId)
      continue
    }
    console.log('[ProcessFlowCanvas] Emitting edge-deleted for DB edge:', edgeId)
    emit('edge-deleted', edgeId)
    // Apply the removal change for DB edges too
    applyEdgeChanges([change])
    lastKnownEdges.value.delete(edgeId)
  }
}
</script>

<template>
  <div class="process-flow-wrapper">
    <VueFlow
      :key="flowKey"
      :nodes="initialNodes"
      :edges="initialEdges"
      :node-types="nodeTypes"
      :nodes-draggable="isEditMode"
      :nodes-connectable="isEditMode"
      :edges-updatable="isEditMode"
      :elements-selectable="isEditMode"
      :delete-key-code="isEditMode ? ['Backspace', 'Delete'] : null"
      :pan-on-drag="true"
      :zoom-on-scroll="true"
      :zoom-on-pinch="true"
      :zoom-on-double-click="false"
      :prevent-scrolling="true"
      :min-zoom="0.2"
      :max-zoom="2"
      :fit-view-on-init="true"
      :fit-view-options="{ padding: 0.3, minZoom: 0.5, maxZoom: 1.2 }"
      :default-edge-options="{ type: 'smoothstep', animated: false }"
      @node-drag-stop="onNodeDragStop"
      @node-click="onNodeClick"
      @node-double-click="onNodeDoubleClick"
      @connect="onConnectHandler"
      @edges-change="handleEdgesChange"
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
/* Ensure the wrapper takes full height of parent */
.process-flow-wrapper {
  width: 100%;
  height: 100%;
  background: rgba(17, 24, 39, 0.5);
  border-radius: 0.75rem;
  overflow: hidden;
}

/* Make sure VueFlow container fills the wrapper */
.process-flow-wrapper .vue-flow {
  width: 100%;
  height: 100%;
}

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
