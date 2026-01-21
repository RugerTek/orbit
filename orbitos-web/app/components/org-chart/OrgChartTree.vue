<script setup lang="ts">
import { VueFlow, useVueFlow } from '@vue-flow/core'
import { Background } from '@vue-flow/background'
import { Controls } from '@vue-flow/controls'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import '@vue-flow/controls/dist/style.css'
import type { OrgChartResource } from '~/types/operations'
import { markRaw } from 'vue'
import OrgChartNode from './OrgChartNode.vue'

const props = defineProps<{
  nodes: OrgChartResource[]
}>()

const emit = defineEmits<{
  select: [person: OrgChartResource]
}>()

const nodeTypes = {
  person: markRaw(OrgChartNode),
}

// Convert org chart tree to Vue Flow nodes/edges
const flowData = computed(() => {
  const nodes: any[] = []
  const edges: any[] = []

  // Layout constants
  const NODE_WIDTH = 160
  const NODE_HEIGHT = 80
  const HORIZONTAL_GAP = 60
  const VERTICAL_GAP = 80

  // Calculate subtree width
  const getSubtreeWidth = (person: OrgChartResource): number => {
    if (!person.directReports?.length) return NODE_WIDTH
    const childrenWidth = person.directReports.reduce((sum, child) =>
      sum + getSubtreeWidth(child) + HORIZONTAL_GAP, -HORIZONTAL_GAP)
    return Math.max(NODE_WIDTH, childrenWidth)
  }

  // Process nodes recursively with proper positioning
  const processNode = (
    person: OrgChartResource,
    x: number,
    y: number,
  ) => {
    const nodeId = person.id

    nodes.push({
      id: nodeId,
      type: 'person',
      position: { x, y },
      data: { person },
    })

    // Add edge from manager
    if (person.reportsToResourceId) {
      edges.push({
        id: `edge-${person.reportsToResourceId}-${nodeId}`,
        source: person.reportsToResourceId,
        target: nodeId,
        type: 'smoothstep',
        style: { stroke: 'rgba(168, 85, 247, 0.5)', strokeWidth: 2 },
      })
    }

    // Process children
    if (person.directReports?.length) {
      const totalChildrenWidth = person.directReports.reduce((sum, child) =>
        sum + getSubtreeWidth(child) + HORIZONTAL_GAP, -HORIZONTAL_GAP)
      let currentX = x + NODE_WIDTH / 2 - totalChildrenWidth / 2

      for (const child of person.directReports) {
        const childWidth = getSubtreeWidth(child)
        const childX = currentX + childWidth / 2 - NODE_WIDTH / 2
        processNode(child, childX, y + NODE_HEIGHT + VERTICAL_GAP)
        currentX += childWidth + HORIZONTAL_GAP
      }
    }
  }

  // Process root nodes
  if (props.nodes.length > 0) {
    const totalRootWidth = props.nodes.reduce((sum, root) =>
      sum + getSubtreeWidth(root) + HORIZONTAL_GAP, -HORIZONTAL_GAP)
    let currentX = -totalRootWidth / 2

    for (const root of props.nodes) {
      const rootWidth = getSubtreeWidth(root)
      const rootX = currentX + rootWidth / 2 - NODE_WIDTH / 2
      processNode(root, rootX, 0)
      currentX += rootWidth + HORIZONTAL_GAP
    }
  }

  return { nodes, edges }
})

const onNodeClick = (event: any) => {
  // Vue Flow passes the node as the first argument in newer versions
  const node = event.node || event
  if (node?.data?.person) {
    emit('select', node.data.person)
  }
}
</script>

<template>
  <div class="h-[600px] w-full orbitos-glass-subtle rounded-xl overflow-hidden">
    <VueFlow
      :nodes="flowData.nodes"
      :edges="flowData.edges"
      :node-types="nodeTypes"
      :fit-view-on-init="true"
      :pan-on-drag="true"
      :zoom-on-scroll="true"
      :default-viewport="{ x: 0, y: 0, zoom: 0.8 }"
      @node-click="onNodeClick"
    >
      <Background pattern-color="rgba(255, 255, 255, 0.03)" :gap="20" />
      <Controls class="!bg-gray-800/80 !border-white/10 !rounded-lg" />
    </VueFlow>
  </div>
</template>

<style>
/* Vue Flow styling overrides */
.vue-flow__controls {
  background: rgba(31, 41, 55, 0.8) !important;
  border: 1px solid rgba(255, 255, 255, 0.1) !important;
  border-radius: 0.5rem !important;
}

.vue-flow__controls-button {
  background: transparent !important;
  border: none !important;
  color: rgba(255, 255, 255, 0.7) !important;
}

.vue-flow__controls-button:hover {
  background: rgba(255, 255, 255, 0.1) !important;
}
</style>
