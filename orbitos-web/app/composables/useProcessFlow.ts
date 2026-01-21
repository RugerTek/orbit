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

    // Find the maximum Y position of activities that have stored positions
    // This is used to position new activities below the existing flow
    let maxStoredY = 0
    let maxStoredX = FLOW_LAYOUT.HORIZONTAL_CENTER - FLOW_LAYOUT.NODE_WIDTH / 2
    sortedActivities.forEach((activity) => {
      const hasStoredPosition = (activity.positionX !== undefined && activity.positionX !== 0) ||
                                 (activity.positionY !== undefined && activity.positionY !== 0)
      if (hasStoredPosition && activity.positionY && activity.positionY > maxStoredY) {
        maxStoredY = activity.positionY
        maxStoredX = activity.positionX || maxStoredX
      }
    })

    // Track the number of activities without positions (for stacking them)
    let newActivityCount = 0

    sortedActivities.forEach((activity, index) => {
      // Use stored position if available (and not both 0), otherwise calculate from order
      const hasStoredPosition = (activity.positionX !== undefined && activity.positionX !== 0) ||
                                 (activity.positionY !== undefined && activity.positionY !== 0)

      let position: { x: number; y: number }
      if (hasStoredPosition) {
        position = { x: activity.positionX || 0, y: activity.positionY || 0 }
      } else {
        // New activity without position
        // In explicit mode, place it offset to the side so it doesn't overlap the main flow
        // In implicit mode, place it in the sequential flow
        if (process.value!.useExplicitFlow) {
          // Place new activities to the right side of the canvas
          // Position them at about 1/3 height of the existing flow to be more visible
          // and clearly separate from the main connected flow
          const offsetX = FLOW_LAYOUT.HORIZONTAL_CENTER + FLOW_LAYOUT.NODE_WIDTH * 1.5
          // Start at a reasonable Y position (either middle of flow or first third)
          const midY = maxStoredY > 0 ? Math.max(FLOW_LAYOUT.VERTICAL_SPACING, maxStoredY / 2) : FLOW_LAYOUT.VERTICAL_SPACING * 2
          position = {
            x: offsetX,
            y: midY + (newActivityCount * FLOW_LAYOUT.VERTICAL_SPACING)
          }
          newActivityCount++
        } else {
          // Implicit mode: place in sequential flow based on order
          const calculatedY = (index + 1) * FLOW_LAYOUT.VERTICAL_SPACING
          position = {
            x: FLOW_LAYOUT.HORIZONTAL_CENTER - FLOW_LAYOUT.NODE_WIDTH / 2,
            y: calculatedY
          }
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

    // End node positioning:
    // - X position: Always centered horizontally (same as Start node) to maintain visual alignment
    // - Y position: Based on the lowest activity in the flow
    //   - In explicit mode with exitActivityId: below the exit activity
    //   - In explicit mode without exitActivityId: below the lowest activity with stored position
    //   - In implicit mode: below the last activity in the sequential flow
    //   - Fallback: default position
    let endNodeY = FLOW_LAYOUT.VERTICAL_SPACING
    // End node X is always centered (same as Start node) - it should not shift horizontally
    // when activities are moved around
    const endNodeX = FLOW_LAYOUT.HORIZONTAL_CENTER

    if (process.value.useExplicitFlow && process.value.exitActivityId) {
      // In explicit mode with exit activity defined, position End below the exit activity
      const exitActivity = sortedActivities.find(a => a.id === process.value!.exitActivityId)
      if (exitActivity) {
        const exitNode = result.find(n => n.id === exitActivity.id)
        if (exitNode) {
          endNodeY = exitNode.position.y + FLOW_LAYOUT.VERTICAL_SPACING
        }
      }
    } else if (process.value.useExplicitFlow && !process.value.exitActivityId) {
      // In explicit mode without exit activity, position End based on activities with stored positions only
      // This prevents newly added activities from shifting the End node
      if (maxStoredY > 0) {
        endNodeY = maxStoredY + FLOW_LAYOUT.VERTICAL_SPACING
      }
    } else if (!process.value.useExplicitFlow && sortedActivities.length > 0) {
      // In implicit mode, find the activity with highest Y that's roughly aligned with the main flow
      // (within reasonable horizontal distance from center)
      const mainFlowX = FLOW_LAYOUT.HORIZONTAL_CENTER - FLOW_LAYOUT.NODE_WIDTH / 2
      const horizontalTolerance = FLOW_LAYOUT.NODE_WIDTH * 2 // Activities within 2 node widths of center

      // Filter to activities on the main vertical axis
      const mainFlowActivities = result.filter(n => {
        if (n.id === 'start' || n.id === 'end') return false
        const xDistance = Math.abs(n.position.x - mainFlowX)
        return xDistance <= horizontalTolerance
      })

      if (mainFlowActivities.length > 0) {
        // Find the one with highest Y position
        const lowestActivity = mainFlowActivities.reduce((lowest, current) =>
          current.position.y > lowest.position.y ? current : lowest
        )
        endNodeY = lowestActivity.position.y + FLOW_LAYOUT.VERTICAL_SPACING
      } else {
        // All activities are off the main flow, use the last one by order
        const lastActivity = sortedActivities[sortedActivities.length - 1]
        if (lastActivity) {
          const lastNode = result.find(n => n.id === lastActivity.id)
          if (lastNode) {
            endNodeY = lastNode.position.y + FLOW_LAYOUT.VERTICAL_SPACING
          }
        }
      }
    } else if (sortedActivities.length > 0) {
      // Fallback: use maxStoredY (for edge cases)
      endNodeY = maxStoredY + FLOW_LAYOUT.VERTICAL_SPACING
    }

    result.push({
      id: 'end',
      type: 'end',
      position: { x: endNodeX, y: endNodeY },
      data: { output: process.value.output },
    })

    return result
  })

  // Convert API edges to Vue Flow edges, or generate implicit edges from order
  const edges = computed<ProcessFlowEdge[]>(() => {
    if (!process.value) return []

    const result: ProcessFlowEdge[] = []
    const sortedActivities = [...process.value.activities].sort((a, b) => a.order - b.order)

    // Determine if user is in "explicit flow mode"
    // The useExplicitFlow flag is set to true when user creates/deletes edges or sets flow endpoints
    // This flag persists even when all edges are deleted, preventing implicit edges from regenerating
    const isExplicitMode = process.value.useExplicitFlow

    console.log('[useProcessFlow] Computing edges:', {
      isExplicitMode,
      apiEdgeCount: process.value.edges?.length || 0,
      entryActivityId: process.value.entryActivityId,
      exitActivityId: process.value.exitActivityId,
      activityCount: sortedActivities.length
    })

    // If user has defined explicit flow, use their edges only
    if (isExplicitMode) {
      // Add edges from API - user is in full control of the flow
      if (process.value.edges) {
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
      }

      // Add Start edge if entryActivityId is set
      if (process.value.entryActivityId) {
        result.push({
          id: 'edge-start',
          source: 'start',
          target: process.value.entryActivityId,
          type: 'default',
        })
      }

      // Add End edge if exitActivityId is set
      if (process.value.exitActivityId) {
        result.push({
          id: 'edge-end',
          source: process.value.exitActivityId,
          target: 'end',
          type: 'default',
        })
      }
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

    console.log('[useProcessFlow] Computed edges:', result.map(e => `${e.id}: ${e.source} -> ${e.target}`))
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
