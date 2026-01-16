// Operations Module Types - Based on Operations-Tool spec

// ENT-008: Function
export interface OpsFunction {
  id: string
  organizationId: string
  name: string
  purpose?: string
  description?: string
  category?: string
  complexity?: 'simple' | 'moderate' | 'complex'
  requiresApproval: boolean
  toolsRequired?: string[]
  estimatedDuration?: number // minutes
  instructions?: string
  status: 'active' | 'deprecated'
  createdAt: string
  updatedAt: string
}

// ENT-009: Role
export interface OpsRole {
  id: string
  organizationId: string
  name: string
  purpose?: string
  description?: string
  status: 'active' | 'deprecated'
  createdAt: string
  updatedAt: string
}

// ENT-007: Resource (Person, Tool, Partner, Asset)
export interface OpsResource {
  id: string
  organizationId: string
  name: string
  resourceType: 'person' | 'tool' | 'partner' | 'asset'
  description?: string
  status: 'active' | 'inactive' | 'archived'
  // Person-specific
  email?: string
  title?: string
  department?: string
  // Tool-specific
  vendor?: string
  monthlyCost?: number
  // Partner-specific
  contactName?: string
  contractValue?: number
  createdAt: string
  updatedAt: string
}

// ENT-012: Process
export interface OpsProcess {
  id: string
  organizationId: string
  businessUnitId?: string
  name: string
  purpose?: string
  description?: string
  ownerId?: string
  trigger?: string
  output?: string
  frequency?: 'daily' | 'weekly' | 'monthly' | 'on_demand' | 'continuous'
  status: 'draft' | 'active' | 'deprecated'
  stateType: 'current' | 'target'
  linkedProcessId?: string
  createdAt: string
  updatedAt: string
}

// ENT-013: Activity
export interface OpsActivity {
  id: string
  processId: string
  name: string
  description?: string
  order: number
  activityType: 'manual' | 'automated' | 'hybrid' | 'decision' | 'handoff'
  functionId?: string
  assignedResourceId?: string
  linkedProcessId?: string // Subprocess linking - drill down capability
  estimatedDuration?: number // minutes
  instructions?: string
  // Vue Flow canvas position
  positionX: number
  positionY: number
  createdAt: string
  updatedAt: string
}

// Activity Edge (Vue Flow connection between activities)
export type EdgeType = 'default' | 'step' | 'smoothstep' | 'straight'

export interface OpsActivityEdge {
  id: string
  processId: string
  sourceActivityId: string
  targetActivityId: string
  sourceHandle?: string // e.g., "yes", "no" for decision nodes
  targetHandle?: string
  edgeType: EdgeType
  label?: string
  animated: boolean
  createdAt: string
  updatedAt: string
}

// ENT-014: Metric
export interface OpsMetric {
  id: string
  organizationId: string
  name: string
  description?: string
  unit?: string
  targetValue?: number
  currentValue?: number
  direction: 'increase' | 'decrease' | 'maintain'
  frequency: 'daily' | 'weekly' | 'monthly' | 'quarterly'
  ownerId?: string
  status: 'active' | 'deprecated'
  createdAt: string
  updatedAt: string
}

// ENT-015: Goal (OKR)
export interface OpsGoal {
  id: string
  organizationId: string
  parentGoalId?: string
  name: string
  description?: string
  goalType: 'objective' | 'key_result'
  targetValue?: number
  currentValue?: number
  startDate?: string
  endDate?: string
  status: 'draft' | 'active' | 'completed' | 'cancelled'
  ownerId?: string
  createdAt: string
  updatedAt: string
}

// ENT-005: Canvas
export interface OpsCanvas {
  id: string
  organizationId: string
  name: string
  canvasType: 'bmc' | 'lean' | 'value_prop' | 'custom'
  status: 'draft' | 'active' | 'archived'
  content: Record<string, CanvasSection>
  createdAt: string
  updatedAt: string
}

export interface CanvasSection {
  title: string
  items: CanvasItem[]
}

export interface CanvasItem {
  id: string
  content: string
  color?: string
  createdAt: string
}

// Health Score Types
export interface OrgHealthScore {
  overall: number // 0-100
  dimensions: {
    coverage: number
    spofCount: number
    processHealth: number
    goalProgress: number
    resourceUtilization: number
  }
  alerts: HealthAlert[]
  lastCalculated: string
}

export interface HealthAlert {
  id: string
  severity: 'critical' | 'warning' | 'info'
  category: 'spof' | 'coverage' | 'process' | 'goal' | 'resource'
  title: string
  description: string
  entityType: string
  entityId: string
  suggestedAction?: string
}

// View Models (enriched for UI)
export interface ProcessWithActivities extends OpsProcess {
  activities: ActivityWithDetails[]
  edges: OpsActivityEdge[]
  owner?: OpsResource
  activityCount: number
  functionCount: number
}

export interface ActivityWithDetails extends OpsActivity {
  function?: OpsFunction
  assignedResource?: OpsResource
  linkedProcess?: { id: string; name: string } // Subprocess for drill-down
  status?: 'complete' | 'in_progress' | 'pending' | 'spof' | 'at_risk'
}

export interface FunctionWithUsage extends OpsFunction {
  usedInProcesses: Array<{ id: string; name: string; activityName: string }>
  capablePeople: Array<{ id: string; name: string; role: string; isPrimary: boolean }>
  tools: OpsResource[]
  coverageStatus: 'covered' | 'spof' | 'at_risk'
  coverageCount: number
}

export interface RoleWithAssignments extends OpsRole {
  functions: OpsFunction[]
  assignedPeople: OpsResource[]
  coverageStatus: 'covered' | 'understaffed' | 'critical'
  targetCount: number
  actualCount: number
}

export interface GoalWithProgress extends OpsGoal {
  keyResults?: GoalWithProgress[]
  progress: number // 0-100
  owner?: OpsResource
  linkedMetrics?: OpsMetric[]
}
