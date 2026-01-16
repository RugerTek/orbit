import type {
  OpsProcess,
  OpsActivity,
  OpsActivityEdge,
  EdgeType,
  OpsFunction,
  OpsRole,
  OpsResource,
  OpsGoal,
  OpsMetric,
  OpsCanvas,
  ProcessWithActivities,
  FunctionWithUsage,
  RoleWithAssignments,
  GoalWithProgress,
  OrgHealthScore,
} from '~/types/operations'
import { useApi } from '~/composables/useApi'

// Hardcoded organization ID from seed data - TODO: Get from user's organization membership
const DEFAULT_ORG_ID = '11111111-1111-1111-1111-111111111111'

// API Response Types
interface ApiProcess {
  id: string
  name: string
  purpose?: string
  description?: string
  trigger?: string
  output?: string
  frequency?: number
  status: number
  stateType: number
  organizationId: string
  ownerId?: string
  ownerName?: string
  linkedProcessId?: string
  linkedProcessName?: string
  createdAt: string
  updatedAt: string
  activityCount: number
  activities?: ApiActivity[]
  edges?: ApiActivityEdge[]
}

interface ApiActivity {
  id: string
  name: string
  description?: string
  order: number
  activityType: number
  estimatedDurationMinutes?: number
  instructions?: string
  processId: string
  functionId?: string
  functionName?: string
  assignedResourceId?: string
  assignedResourceName?: string
  linkedProcessId?: string
  linkedProcessName?: string
  positionX: number
  positionY: number
  createdAt: string
}

interface ApiActivityEdge {
  id: string
  processId: string
  sourceActivityId: string
  targetActivityId: string
  sourceHandle?: string
  targetHandle?: string
  edgeType: number
  label?: string
  animated: boolean
  createdAt: string
  updatedAt: string
}

interface ApiGoal {
  id: string
  name: string
  description?: string
  goalType: number
  status: number
  timeframeStart?: string
  timeframeEnd?: string
  targetValue?: number
  currentValue?: number
  unit?: string
  progress?: number
  organizationId: string
  parentId?: string
  parentName?: string
  ownerId?: string
  ownerName?: string
  createdAt: string
  updatedAt: string
  children?: ApiGoal[]
}

interface ApiCanvas {
  id: string
  name: string
  description?: string
  canvasType: number
  status: number
  organizationId: string
  createdAt: string
  updatedAt: string
  blocks?: ApiCanvasBlock[]
}

interface ApiCanvasBlock {
  id: string
  blockType: number
  content?: string
  displayOrder: number
  canvasId: string
}

interface ApiResource {
  id: string
  name: string
  description?: string
  status: number
  metadata?: string
  organizationId: string
  resourceSubtypeId: string
  resourceSubtypeName: string
  resourceType: number
  linkedUserId?: string
  linkedUserName?: string
  createdAt: string
  updatedAt: string
  roleAssignmentCount: number
  functionCapabilityCount: number
}

// Enum mappings
const frequencyMap: Record<number, OpsProcess['frequency']> = {
  0: 'daily',
  1: 'weekly',
  2: 'monthly',
  3: 'on_demand',
  4: 'continuous',
}

const processStatusMap: Record<number, OpsProcess['status']> = {
  0: 'draft',
  1: 'active',
  2: 'deprecated',
}

const stateTypeMap: Record<number, OpsProcess['stateType']> = {
  0: 'current',
  1: 'target',
}

const activityTypeMap: Record<number, OpsActivity['activityType']> = {
  0: 'manual',
  1: 'automated',
  2: 'hybrid',
  3: 'decision',
  4: 'handoff',
}

const edgeTypeMap: Record<number, EdgeType> = {
  0: 'default',
  1: 'step',
  2: 'smoothstep',
  3: 'straight',
}

const edgeTypeToNum: Record<EdgeType, number> = {
  default: 0,
  step: 1,
  smoothstep: 2,
  straight: 3,
}

const goalTypeMap: Record<number, OpsGoal['goalType']> = {
  0: 'objective',
  1: 'key_result',
}

const goalStatusMap: Record<number, OpsGoal['status']> = {
  0: 'active',
  1: 'completed',
  2: 'cancelled',
}

const resourceTypeMap: Record<number, OpsResource['resourceType']> = {
  0: 'person',
  1: 'person', // Team maps to person for now
  2: 'tool',
  3: 'tool', // Automation maps to tool
  4: 'partner',
  5: 'asset',
}

const resourceStatusMap: Record<number, OpsResource['status']> = {
  0: 'inactive', // Planned
  1: 'active',
  2: 'archived', // Deprecated
}

// Transform API response to frontend types
function transformProcess(api: ApiProcess): ProcessWithActivities {
  return {
    id: api.id,
    organizationId: api.organizationId,
    name: api.name,
    purpose: api.purpose,
    description: api.description,
    trigger: api.trigger,
    output: api.output,
    frequency: api.frequency !== undefined ? frequencyMap[api.frequency] : undefined,
    status: processStatusMap[api.status] || 'draft',
    stateType: stateTypeMap[api.stateType] || 'current',
    ownerId: api.ownerId,
    linkedProcessId: api.linkedProcessId,
    createdAt: api.createdAt,
    updatedAt: api.updatedAt,
    activityCount: api.activityCount,
    functionCount: api.activities?.filter(a => a.functionId).length || 0,
    activities: (api.activities || []).map(a => ({
      id: a.id,
      processId: a.processId,
      name: a.name,
      description: a.description,
      order: a.order,
      activityType: activityTypeMap[a.activityType] || 'manual',
      functionId: a.functionId,
      assignedResourceId: a.assignedResourceId,
      linkedProcessId: a.linkedProcessId,
      estimatedDuration: a.estimatedDurationMinutes,
      instructions: a.instructions,
      positionX: a.positionX || 0,
      positionY: a.positionY || 0,
      createdAt: a.createdAt,
      updatedAt: a.createdAt,
      function: a.functionName ? { id: a.functionId!, name: a.functionName } as OpsFunction : undefined,
      assignedResource: a.assignedResourceName ? { id: a.assignedResourceId!, name: a.assignedResourceName } as OpsResource : undefined,
      linkedProcess: a.linkedProcessName ? { id: a.linkedProcessId!, name: a.linkedProcessName } : undefined,
    })),
    edges: (api.edges || []).map(e => ({
      id: e.id,
      processId: e.processId,
      sourceActivityId: e.sourceActivityId,
      targetActivityId: e.targetActivityId,
      sourceHandle: e.sourceHandle,
      targetHandle: e.targetHandle,
      edgeType: edgeTypeMap[e.edgeType] || 'default',
      label: e.label,
      animated: e.animated,
      createdAt: e.createdAt,
      updatedAt: e.updatedAt,
    })),
    owner: api.ownerName ? { id: api.ownerId!, name: api.ownerName } as OpsResource : undefined,
  }
}

function transformGoal(api: ApiGoal): GoalWithProgress {
  return {
    id: api.id,
    organizationId: api.organizationId,
    parentGoalId: api.parentId,
    name: api.name,
    description: api.description,
    goalType: goalTypeMap[api.goalType] || 'objective',
    targetValue: api.targetValue,
    currentValue: api.currentValue,
    startDate: api.timeframeStart,
    endDate: api.timeframeEnd,
    status: goalStatusMap[api.status] || 'active',
    ownerId: api.ownerId,
    createdAt: api.createdAt,
    updatedAt: api.updatedAt,
    progress: api.progress || 0,
    keyResults: api.children?.map(transformGoal),
    owner: api.ownerName ? { id: api.ownerId!, name: api.ownerName } as OpsResource : undefined,
  }
}

function transformResource(api: ApiResource): OpsResource {
  return {
    id: api.id,
    organizationId: api.organizationId,
    name: api.name,
    resourceType: resourceTypeMap[api.resourceType] || 'person',
    description: api.description,
    status: resourceStatusMap[api.status] || 'active',
    createdAt: api.createdAt,
    updatedAt: api.updatedAt,
  }
}

// Canvas block type names for UI
const canvasBlockNames: Record<number, string> = {
  0: 'keyPartners',
  1: 'keyActivities',
  2: 'keyResources',
  3: 'valuePropositions',
  4: 'customerRelationships',
  5: 'channels',
  6: 'customerSegments',
  7: 'costStructure',
  8: 'revenueStreams',
}

function transformCanvas(api: ApiCanvas): OpsCanvas {
  const content: Record<string, { title: string; items: { id: string; content: string; createdAt: string }[] }> = {}

  for (const block of api.blocks || []) {
    const blockName = canvasBlockNames[block.blockType] || `block_${block.blockType}`
    let items: string[] = []
    try {
      items = block.content ? JSON.parse(block.content) : []
    } catch {
      items = []
    }

    content[blockName] = {
      title: blockName.replace(/([A-Z])/g, ' $1').trim(),
      items: items.map((item, i) => ({
        id: `${block.id}-${i}`,
        content: item,
        createdAt: api.createdAt,
      })),
    }
  }

  return {
    id: api.id,
    organizationId: api.organizationId,
    name: api.name,
    canvasType: api.canvasType === 0 ? 'bmc' : api.canvasType === 1 ? 'lean' : 'value_prop',
    status: api.status === 0 ? 'draft' : api.status === 1 ? 'active' : 'archived',
    content,
    createdAt: api.createdAt,
    updatedAt: api.updatedAt,
  }
}

// API Response types for Roles and Functions
interface ApiRole {
  id: string
  name: string
  description?: string
  purpose?: string
  department?: string
  organizationId: string
  createdAt: string
  updatedAt: string
  assignmentCount: number
  functionCount: number
}

interface ApiFunction {
  id: string
  name: string
  description?: string
  purpose?: string
  category?: string
  status: number
  organizationId: string
  createdAt: string
  updatedAt: string
  capabilityCount: number
  roleCount: number
}

interface ApiRoleAssignment {
  id: string
  resourceId: string
  resourceName: string
  roleId: string
  roleName: string
  allocationPercentage?: number
  isPrimary: boolean
  startDate?: string
  endDate?: string
  createdAt: string
}

// Transform functions for Roles and Functions
function transformRole(api: ApiRole): RoleWithAssignments {
  return {
    id: api.id,
    organizationId: api.organizationId,
    name: api.name,
    purpose: api.purpose,
    description: api.description,
    department: api.department,
    createdAt: api.createdAt,
    updatedAt: api.updatedAt,
    assignmentCount: api.assignmentCount,
    functionCount: api.functionCount,
    coverageStatus: api.assignmentCount === 0 ? 'uncovered' : api.assignmentCount === 1 ? 'at_risk' : 'covered',
  }
}

function transformFunctionApi(api: ApiFunction): FunctionWithUsage {
  return {
    id: api.id,
    organizationId: api.organizationId,
    name: api.name,
    purpose: api.purpose,
    description: api.description,
    category: api.category,
    createdAt: api.createdAt,
    updatedAt: api.updatedAt,
    capabilityCount: api.capabilityCount,
    processCount: 0, // Would need to be calculated
    coverageStatus: api.capabilityCount === 0 ? 'uncovered' : api.capabilityCount === 1 ? 'spof' : 'covered',
  }
}

const mockHealthScore: OrgHealthScore = {
  overall: 0,
  dimensions: {
    coverage: 0,
    spofCount: 0,
    processHealth: 0,
    goalProgress: 0,
    resourceUtilization: 0,
  },
  alerts: [],
  lastCalculated: new Date().toISOString(),
}

export function useOperations() {
  const { get, post, put, delete: del } = useApi()

  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const organizationId = ref(DEFAULT_ORG_ID)

  // State
  const processes = ref<ProcessWithActivities[]>([])
  const functions = ref<FunctionWithUsage[]>([])
  const roles = ref<RoleWithAssignments[]>([])
  const roleAssignments = ref<ApiRoleAssignment[]>([])
  const people = ref<OpsResource[]>([])
  const resources = ref<OpsResource[]>([])
  const goals = ref<GoalWithProgress[]>([])
  const canvases = ref<OpsCanvas[]>([])
  const healthScore = ref<OrgHealthScore>(mockHealthScore)

  // Processes
  const fetchProcesses = async () => {
    isLoading.value = true
    error.value = null
    try {
      const data = await get<ApiProcess[]>(`/api/organizations/${organizationId.value}/operations/processes`)
      processes.value = data.map(transformProcess)
    } catch (e) {
      console.error('Failed to fetch processes:', e)
      error.value = 'Failed to fetch processes'
    } finally {
      isLoading.value = false
    }
  }

  const getProcess = (id: string) => {
    return computed(() => processes.value.find(p => p.id === id))
  }

  const fetchProcessById = async (id: string): Promise<ProcessWithActivities | null> => {
    try {
      const data = await get<ApiProcess>(`/api/organizations/${organizationId.value}/operations/processes/${id}`)
      return transformProcess(data)
    } catch (e) {
      console.error('Failed to fetch process:', e)
      return null
    }
  }

  const createProcess = async (process: Partial<OpsProcess>) => {
    // Map string status to number: draft=0, active=1, deprecated=2
    const statusToNum: Record<string, number> = { draft: 0, active: 1, deprecated: 2 }
    const stateToNum: Record<string, number> = { current: 0, target: 1 }
    const freqToNum: Record<string, number> = { daily: 0, weekly: 1, monthly: 2, on_demand: 3, continuous: 4 }

    const data = await post<ApiProcess>(`/api/organizations/${organizationId.value}/operations/processes`, {
      name: process.name,
      purpose: process.purpose,
      description: process.description,
      trigger: process.trigger,
      output: process.output,
      frequency: process.frequency ? freqToNum[process.frequency] : undefined,
      status: process.status ? statusToNum[process.status] : 0,
      stateType: process.stateType ? stateToNum[process.stateType] : 0,
      ownerId: process.ownerId,
    })
    const transformed = transformProcess(data)
    processes.value.push(transformed)
    return transformed
  }

  const updateProcess = async (id: string, process: Partial<OpsProcess>) => {
    const data = await put<ApiProcess>(`/api/organizations/${organizationId.value}/operations/processes/${id}`, {
      name: process.name,
      purpose: process.purpose,
      description: process.description,
      trigger: process.trigger,
      output: process.output,
      frequency: process.frequency ? Object.entries(frequencyMap).find(([, v]) => v === process.frequency)?.[0] : undefined,
      status: process.status ? Object.entries(processStatusMap).find(([, v]) => v === process.status)?.[0] : 1,
      stateType: process.stateType ? Object.entries(stateTypeMap).find(([, v]) => v === process.stateType)?.[0] : 0,
      ownerId: process.ownerId,
    })
    const index = processes.value.findIndex(p => p.id === id)
    if (index !== -1) {
      processes.value[index] = transformProcess(data)
    }
    return processes.value[index]
  }

  const deleteProcess = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/processes/${id}`)
    processes.value = processes.value.filter(p => p.id !== id)
  }

  // Activities
  const createActivity = async (processId: string, activity: Partial<OpsActivity>) => {
    // Map string type to number: manual=0, automated=1, hybrid=2, decision=3, handoff=4
    const typeToNum: Record<string, number> = { manual: 0, automated: 1, hybrid: 2, decision: 3, handoff: 4 }
    const activityTypeNum = activity.activityType ? typeToNum[activity.activityType] : 0

    const data = await post<ApiActivity>(`/api/organizations/${organizationId.value}/operations/processes/${processId}/activities`, {
      name: activity.name,
      description: activity.description,
      order: activity.order,
      activityType: activityTypeNum,
      estimatedDurationMinutes: activity.estimatedDuration,
      instructions: activity.instructions,
      functionId: activity.functionId,
      assignedResourceId: activity.assignedResourceId,
      linkedProcessId: activity.linkedProcessId,
      positionX: activity.positionX || 0,
      positionY: activity.positionY || 0,
    })
    return data
  }

  const updateActivity = async (processId: string, activityId: string, activity: Partial<OpsActivity>) => {
    // Map string type to number: manual=0, automated=1, hybrid=2, decision=3, handoff=4
    const typeToNum: Record<string, number> = { manual: 0, automated: 1, hybrid: 2, decision: 3, handoff: 4 }
    const activityTypeNum = activity.activityType ? typeToNum[activity.activityType] : undefined

    const data = await put<ApiActivity>(`/api/organizations/${organizationId.value}/operations/processes/${processId}/activities/${activityId}`, {
      name: activity.name,
      description: activity.description,
      order: activity.order,
      activityType: activityTypeNum,
      estimatedDurationMinutes: activity.estimatedDuration,
      instructions: activity.instructions,
      functionId: activity.functionId,
      assignedResourceId: activity.assignedResourceId,
      linkedProcessId: activity.linkedProcessId,
      positionX: activity.positionX || 0,
      positionY: activity.positionY || 0,
    })
    return data
  }

  const deleteActivity = async (processId: string, activityId: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/processes/${processId}/activities/${activityId}`)
  }

  // Activity Positions (Vue Flow bulk update)
  const updateActivityPositions = async (processId: string, positions: Array<{ activityId: string; positionX: number; positionY: number }>) => {
    await fetch(`/api/organizations/${organizationId.value}/operations/processes/${processId}/activities/positions`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ positions }),
    })
  }

  // Activity Edges (Vue Flow connections)
  const createEdge = async (processId: string, edge: { sourceActivityId: string; targetActivityId: string; sourceHandle?: string; label?: string; edgeType?: EdgeType }) => {
    const data = await post<ApiActivityEdge>(`/api/organizations/${organizationId.value}/operations/processes/${processId}/edges`, {
      sourceActivityId: edge.sourceActivityId,
      targetActivityId: edge.targetActivityId,
      sourceHandle: edge.sourceHandle,
      edgeType: edge.edgeType ? edgeTypeToNum[edge.edgeType] : 0,
      label: edge.label,
      animated: false,
    })
    return {
      id: data.id,
      processId: data.processId,
      sourceActivityId: data.sourceActivityId,
      targetActivityId: data.targetActivityId,
      sourceHandle: data.sourceHandle,
      targetHandle: data.targetHandle,
      edgeType: edgeTypeMap[data.edgeType] || 'default',
      label: data.label,
      animated: data.animated,
      createdAt: data.createdAt,
      updatedAt: data.updatedAt,
    } as OpsActivityEdge
  }

  const deleteEdge = async (processId: string, edgeId: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/processes/${processId}/edges/${edgeId}`)
  }

  // Goals
  const fetchGoals = async () => {
    isLoading.value = true
    error.value = null
    try {
      const data = await get<ApiGoal[]>(`/api/organizations/${organizationId.value}/operations/goals?rootOnly=true`)
      goals.value = data.map(transformGoal)
    } catch (e) {
      console.error('Failed to fetch goals:', e)
      error.value = 'Failed to fetch goals'
    } finally {
      isLoading.value = false
    }
  }

  const createGoal = async (goal: Partial<OpsGoal>) => {
    const typeIndex = Object.entries(goalTypeMap).find(([, v]) => v === goal.goalType)?.[0] || 0
    const data = await post<ApiGoal>(`/api/organizations/${organizationId.value}/operations/goals`, {
      name: goal.name,
      description: goal.description,
      goalType: Number(typeIndex),
      status: 0, // Active
      timeframeStart: goal.startDate,
      timeframeEnd: goal.endDate,
      targetValue: goal.targetValue,
      currentValue: goal.currentValue,
      parentId: goal.parentGoalId,
      ownerId: goal.ownerId,
    })
    const transformed = transformGoal(data)
    if (!goal.parentGoalId) {
      goals.value.push(transformed)
    }
    return transformed
  }

  const updateGoal = async (id: string, goal: Partial<OpsGoal>) => {
    const typeIndex = Object.entries(goalTypeMap).find(([, v]) => v === goal.goalType)?.[0] || 0
    const statusIndex = Object.entries(goalStatusMap).find(([, v]) => v === goal.status)?.[0] || 0
    const data = await put<ApiGoal>(`/api/organizations/${organizationId.value}/operations/goals/${id}`, {
      name: goal.name,
      description: goal.description,
      goalType: Number(typeIndex),
      status: Number(statusIndex),
      timeframeStart: goal.startDate,
      timeframeEnd: goal.endDate,
      targetValue: goal.targetValue,
      currentValue: goal.currentValue,
      parentId: goal.parentGoalId,
      ownerId: goal.ownerId,
    })
    return transformGoal(data)
  }

  // Canvases
  const fetchCanvases = async () => {
    isLoading.value = true
    error.value = null
    try {
      const data = await get<ApiCanvas[]>(`/api/organizations/${organizationId.value}/operations/canvases`)
      canvases.value = data.map(transformCanvas)
    } catch (e) {
      console.error('Failed to fetch canvases:', e)
      error.value = 'Failed to fetch canvases'
    } finally {
      isLoading.value = false
    }
  }

  const fetchCanvasById = async (id: string): Promise<OpsCanvas | null> => {
    try {
      const data = await get<ApiCanvas>(`/api/organizations/${organizationId.value}/operations/canvases/${id}`)
      return transformCanvas(data)
    } catch (e) {
      console.error('Failed to fetch canvas:', e)
      return null
    }
  }

  const updateCanvasBlock = async (canvasId: string, blockType: number, content: string[]) => {
    await put(`/api/organizations/${organizationId.value}/operations/canvases/${canvasId}/blocks/${blockType}`, {
      blockType,
      content: JSON.stringify(content),
      displayOrder: blockType,
    })
  }

  // Resources
  const fetchResources = async () => {
    isLoading.value = true
    error.value = null
    try {
      const data = await get<ApiResource[]>(`/api/organizations/${organizationId.value}/operations/resources`)
      resources.value = data.map(transformResource)
      // Filter people (Person type = 0)
      people.value = data.filter(r => r.resourceType === 0).map(transformResource)
    } catch (e) {
      console.error('Failed to fetch resources:', e)
      error.value = 'Failed to fetch resources'
    } finally {
      isLoading.value = false
    }
  }

  // Functions
  const fetchFunctions = async () => {
    isLoading.value = true
    error.value = null
    try {
      const data = await get<ApiFunction[]>(`/api/organizations/${organizationId.value}/operations/functions`)
      functions.value = data.map(transformFunctionApi)
    } catch (e) {
      console.error('Failed to fetch functions:', e)
      error.value = 'Failed to fetch functions'
    } finally {
      isLoading.value = false
    }
  }

  const getFunction = (id: string) => {
    return computed(() => functions.value.find(f => f.id === id))
  }

  const createFunction = async (func: Partial<OpsFunction>) => {
    const data = await post<ApiFunction>(`/api/organizations/${organizationId.value}/operations/functions`, {
      name: func.name,
      description: func.description,
      purpose: func.purpose,
      category: func.category,
    })
    const transformed = transformFunctionApi(data)
    functions.value.push(transformed)
    return transformed
  }

  const updateFunction = async (id: string, func: Partial<OpsFunction>) => {
    const data = await put<ApiFunction>(`/api/organizations/${organizationId.value}/operations/functions/${id}`, {
      name: func.name,
      description: func.description,
      purpose: func.purpose,
      category: func.category,
    })
    const index = functions.value.findIndex(f => f.id === id)
    if (index !== -1) {
      functions.value[index] = transformFunctionApi(data)
    }
    return functions.value[index]
  }

  const deleteFunction = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/functions/${id}`)
    functions.value = functions.value.filter(f => f.id !== id)
  }

  // Roles
  const fetchRoles = async () => {
    isLoading.value = true
    error.value = null
    try {
      const data = await get<ApiRole[]>(`/api/organizations/${organizationId.value}/operations/roles`)
      roles.value = data.map(transformRole)
    } catch (e) {
      console.error('Failed to fetch roles:', e)
      error.value = 'Failed to fetch roles'
    } finally {
      isLoading.value = false
    }
  }

  const createRole = async (role: Partial<OpsRole>) => {
    const data = await post<ApiRole>(`/api/organizations/${organizationId.value}/operations/roles`, {
      name: role.name,
      description: role.description,
      purpose: role.purpose,
      department: role.department,
    })
    const transformed = transformRole(data)
    roles.value.push(transformed)
    return transformed
  }

  const updateRole = async (id: string, role: Partial<OpsRole>) => {
    const data = await put<ApiRole>(`/api/organizations/${organizationId.value}/operations/roles/${id}`, {
      name: role.name,
      description: role.description,
      purpose: role.purpose,
      department: role.department,
    })
    const index = roles.value.findIndex(r => r.id === id)
    if (index !== -1) {
      roles.value[index] = transformRole(data)
    }
    return roles.value[index]
  }

  const deleteRole = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/roles/${id}`)
    roles.value = roles.value.filter(r => r.id !== id)
  }

  // Role Assignments
  const fetchRoleAssignments = async (resourceId?: string) => {
    const url = resourceId
      ? `/api/organizations/${organizationId.value}/operations/resources/role-assignments?resourceId=${resourceId}`
      : `/api/organizations/${organizationId.value}/operations/resources/role-assignments`
    const data = await get<ApiRoleAssignment[]>(url)
    roleAssignments.value = data
    return data
  }

  const createRoleAssignment = async (assignment: { resourceId: string; roleId: string; allocationPercentage?: number; isPrimary?: boolean }) => {
    const data = await post<ApiRoleAssignment>(`/api/organizations/${organizationId.value}/operations/resources/role-assignments`, assignment)
    roleAssignments.value.push(data)
    return data
  }

  const deleteRoleAssignment = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/resources/role-assignments/${id}`)
    roleAssignments.value = roleAssignments.value.filter(ra => ra.id !== id)
  }

  // People (subset of resources)
  const fetchPeople = async () => {
    await fetchResources()
  }

  // Health Score (mock for now - would need calculation endpoint)
  const fetchHealthScore = async () => {
    healthScore.value = mockHealthScore
  }

  // Stats
  const stats = computed(() => ({
    processCount: processes.value.length,
    functionCount: functions.value.length,
    roleCount: roles.value.length,
    peopleCount: people.value.length,
    spofCount: functions.value.filter(f => f.coverageStatus === 'spof').length,
    atRiskCount: functions.value.filter(f => f.coverageStatus === 'at_risk').length,
    goalProgress: goals.value.length > 0
      ? Math.round(goals.value.reduce((acc, g) => acc + g.progress, 0) / goals.value.length)
      : 0,
  }))

  return {
    // State
    isLoading,
    error,
    organizationId,
    processes,
    functions,
    roles,
    roleAssignments,
    people,
    resources,
    goals,
    canvases,
    healthScore,
    stats,
    // Process Actions
    fetchProcesses,
    fetchProcessById,
    createProcess,
    updateProcess,
    deleteProcess,
    getProcess,
    // Activity Actions
    createActivity,
    updateActivity,
    deleteActivity,
    updateActivityPositions,
    // Edge Actions (Vue Flow)
    createEdge,
    deleteEdge,
    // Goal Actions
    fetchGoals,
    createGoal,
    updateGoal,
    // Canvas Actions
    fetchCanvases,
    fetchCanvasById,
    updateCanvasBlock,
    // Resource Actions
    fetchResources,
    // Function Actions
    fetchFunctions,
    createFunction,
    updateFunction,
    deleteFunction,
    getFunction,
    // Role Actions
    fetchRoles,
    createRole,
    updateRole,
    deleteRole,
    // Role Assignment Actions
    fetchRoleAssignments,
    createRoleAssignment,
    deleteRoleAssignment,
    // Other Actions
    fetchPeople,
    fetchHealthScore,
  }
}
