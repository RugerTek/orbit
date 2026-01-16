// Vue Flow composable for process editor
// Converts backend data to Vue Flow nodes and edges

import type { Node, Edge } from '@vue-flow/core'
import type { ProcessWithActivities, ActivityWithDetails, OpsActivityEdge } from '~/types/operations'
import { FLOW_LAYOUT, activityTypeColors } from '~/types/flow'

export interface ProcessFlowNode extends Node {
  data: {
    activity?: ActivityWithDetails
    trigger?: string
    output?: string
    isSelected?: boolean
  }
}

export interface ProcessFlowEdge extends Edge {
  data?: {
    label?: string
    animated?: boolean
  }
}

export function useProcessFlow(process: Ref<ProcessWithActivities | null>) {
  // Convert activities to Vue Flow nodes
  const nodes = computed<ProcessFlowNode[]>(() => {
    if (!process.value) return []

    console.log('[useProcessFlow] Computing nodes for', process.value.activities?.length || 0, 'activities')

    const result: ProcessFlowNode[] = []

    // Start node
    result.push({
      id: 'start',
      type: 'start',
      position: { x: FLOW_LAYOUT.HORIZONTAL_CENTER, y: 0 },
      data: { trigger: process.value.trigger },
    })

    // Activity nodes
    const sortedActivities = [...process.value.activities].sort((a, b) => a.order - b.order)

    // Find the maximum Y position of existing activities for positioning new ones
    let maxExistingY = 0
    sortedActivities.forEach((activity) => {
      if (activity.positionY && activity.positionY > maxExistingY) {
        maxExistingY = activity.positionY
      }
    })

    sortedActivities.forEach((activity, index) => {
      // Use stored position if available (and not both 0), otherwise calculate from order
      const hasStoredPosition = (activity.positionX !== undefined && activity.positionX !== 0) ||
                                 (activity.positionY !== undefined && activity.positionY !== 0)

      let position: { x: number; y: number }
      if (hasStoredPosition) {
        position = { x: activity.positionX || 0, y: activity.positionY || 0 }
      } else {
        // New activity without position - place below existing activities
        const calculatedY = maxExistingY > 0
          ? maxExistingY + FLOW_LAYOUT.VERTICAL_SPACING
          : (index + 1) * FLOW_LAYOUT.VERTICAL_SPACING
        position = {
          x: FLOW_LAYOUT.HORIZONTAL_CENTER - FLOW_LAYOUT.NODE_WIDTH / 2,
          y: calculatedY
        }
        // Update maxExistingY for next iteration
        if (calculatedY > maxExistingY) {
          maxExistingY = calculatedY
        }
      }

      result.push({
        id: activity.id,
        type: activity.activityType === 'decision' ? 'decision' : 'activity',
        position,
        data: {
          activity,
          isSelected: false,
        },
      })
    })

    // End node - position below the last activity
    const lastActivityY = sortedActivities.length > 0
      ? Math.max(...sortedActivities.map(a => a.positionY || (sortedActivities.indexOf(a) + 1) * FLOW_LAYOUT.VERTICAL_SPACING))
      : FLOW_LAYOUT.VERTICAL_SPACING

    result.push({
      id: 'end',
      type: 'end',
      position: { x: FLOW_LAYOUT.HORIZONTAL_CENTER, y: lastActivityY + FLOW_LAYOUT.VERTICAL_SPACING },
      data: { output: process.value.output },
    })

    return result
  })

  // Convert API edges to Vue Flow edges, or generate implicit edges from order
  const edges = computed<ProcessFlowEdge[]>(() => {
    if (!process.value) return []

    const result: ProcessFlowEdge[] = []
    const sortedActivities = [...process.value.activities].sort((a, b) => a.order - b.order)

    // If edges exist in the process, use them
    if (process.value.edges && process.value.edges.length > 0) {
      // Add edges from API
      process.value.edges.forEach(edge => {
        result.push({
          id: edge.id,
          source: edge.sourceActivityId,
          target: edge.targetActivityId,
          sourceHandle: edge.sourceHandle || undefined,
          targetHandle: edge.targetHandle || undefined,
          type: edge.label ? 'labeled' : 'default',
          label: edge.label || undefined,
          animated: edge.animated,
          data: {
            label: edge.label,
            animated: edge.animated,
          },
        })
      })

      // Check if we need start/end edges
      const hasStartEdge = result.some(e => e.source === 'start')
      const hasEndEdge = result.some(e => e.target === 'end')

      // Add implicit start edge if not present
      if (!hasStartEdge && sortedActivities.length > 0) {
        result.push({
          id: 'edge-start',
          source: 'start',
          target: sortedActivities[0].id,
          type: 'default',
        })
      }

      // Add implicit end edges for terminal nodes (no outgoing edges)
      const nodesWithOutgoing = new Set(result.map(e => e.source))
      sortedActivities.forEach(activity => {
        if (!nodesWithOutgoing.has(activity.id)) {
          result.push({
            id: `edge-${activity.id}-end`,
            source: activity.id,
            target: 'end',
            type: 'default',
          })
        }
      })
    } else {
      // No edges - generate implicit linear flow from order

      // Start -> first activity
      if (sortedActivities.length > 0) {
        result.push({
          id: 'edge-start',
          source: 'start',
          target: sortedActivities[0].id,
          type: 'default',
        })
      }

      // Activity chain based on order
      for (let i = 0; i < sortedActivities.length - 1; i++) {
        const current = sortedActivities[i]
        const next = sortedActivities[i + 1]

        // For decision nodes, create yes/no branches (simplified)
        if (current.activityType === 'decision') {
          result.push({
            id: `edge-${current.id}-${next.id}`,
            source: current.id,
            sourceHandle: 'yes',
            target: next.id,
            type: 'default',
            label: 'Yes',
          })
        } else {
          result.push({
            id: `edge-${current.id}-${next.id}`,
            source: current.id,
            target: next.id,
            type: 'default',
          })
        }
      }

      // Last activity -> end
      if (sortedActivities.length > 0) {
        result.push({
          id: 'edge-end',
          source: sortedActivities[sortedActivities.length - 1].id,
          target: 'end',
          type: 'default',
        })
      }
    }

    return result
  })

  // Update node position when dragged
  const updateNodePosition = (nodeId: string, x: number, y: number) => {
    // This will be called from the canvas component
    // The actual persistence happens via useFlowPersistence
  }

  return {
    nodes,
    edges,
    updateNodePosition,
  }
}
