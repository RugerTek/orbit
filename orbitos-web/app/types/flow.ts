// Vue Flow type definitions for process flow canvas

import type { Node, Edge } from '@vue-flow/core'
import type { OpsActivity, OpsActivityEdge, ActivityWithDetails } from './operations'

// Custom node data structures
export interface ActivityNodeData {
  activity: ActivityWithDetails
  isSelected: boolean
}

export interface StartNodeData {
  trigger?: string
}

export interface EndNodeData {
  output?: string
}

export interface DecisionNodeData {
  activity: ActivityWithDetails
  isSelected: boolean
}

// Vue Flow node types for activities
export type ActivityNode = Node<ActivityNodeData, any, 'activity'>
export type DecisionNode = Node<DecisionNodeData, any, 'decision'>
export type StartNode = Node<StartNodeData, any, 'start'>
export type EndNode = Node<EndNodeData, any, 'end'>

export type ProcessNode = ActivityNode | DecisionNode | StartNode | EndNode

// Vue Flow edge type for activity connections
export interface ActivityEdgeData {
  label?: string
  animated?: boolean
}

export type ProcessEdge = Edge<ActivityEdgeData>

// Edge type mapping from API (number) to Vue Flow (string)
export const edgeTypeFromApi: Record<number, string> = {
  0: 'default',
  1: 'step',
  2: 'smoothstep',
  3: 'straight',
}

export const edgeTypeToApi: Record<string, number> = {
  default: 0,
  step: 1,
  smoothstep: 2,
  straight: 3,
}

// Activity type colors for node styling
export const activityTypeColors: Record<string, string> = {
  manual: 'bg-blue-500',
  automated: 'bg-emerald-500',
  hybrid: 'bg-cyan-500',
  decision: 'bg-amber-500',
  handoff: 'bg-purple-500',
}

// Activity type icons
export const activityTypeIcons: Record<string, string> = {
  manual: 'mdi-account',
  automated: 'mdi-robot',
  hybrid: 'mdi-account-cog',
  decision: 'mdi-help-rhombus',
  handoff: 'mdi-arrow-right-bold',
}

// Layout constants
export const FLOW_LAYOUT = {
  VERTICAL_SPACING: 150,
  HORIZONTAL_CENTER: 300,
  NODE_WIDTH: 200,
  DECISION_SIZE: 120,
} as const
