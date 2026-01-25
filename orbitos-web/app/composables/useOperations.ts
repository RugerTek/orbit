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
  FunctionCapability,
  CapabilityLevel,
  Partner,
  PartnerType,
  PartnerStatus,
  StrategicValue,
  Channel,
  ChannelType,
  ChannelCategory,
  ChannelStatus,
  ChannelOwnership,
  ValueProposition,
  ValuePropositionStatus,
  CustomerRelationship,
  CustomerRelationshipType,
  CustomerRelationshipStatus,
  RevenueStream,
  RevenueStreamType,
  RevenueStreamStatus,
  PricingMechanism,
  BlockReference,
  ReferenceEntityType,
  ReferenceRole,
  ReferenceLinkType,
  Canvas,
  CanvasType,
  CanvasScopeType,
  CanvasStatus as NewCanvasStatus,
  CanvasBlock,
  CanvasBlockType,
  OrgChartResource,
  OrgChartTree,
  OrgChartMetrics,
} from '~/types/operations'
import { useApi } from '~/composables/useApi'
import { useOrganizations } from '~/composables/useOrganizations'

// Default organization ID - used as fallback when no org is selected
const DEFAULT_ORG_ID = '11111111-1111-1111-1111-111111111111'

// Get current organization ID reactively from useOrganizations, falling back to localStorage
const getOrganizationId = (): string => {
  // Try to get from the reactive useOrganizations composable first
  try {
    const { currentOrganizationId } = useOrganizations()
    if (currentOrganizationId.value) {
      return currentOrganizationId.value
    }
  } catch {
    // Composable not available (e.g., outside Vue context)
  }

  // Fallback to localStorage
  if (typeof window !== 'undefined') {
    return localStorage.getItem('currentOrganizationId') || DEFAULT_ORG_ID
  }
  return DEFAULT_ORG_ID
}

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
  functionId?: string
  functionName?: string
  ownerId?: string
  ownerName?: string
  linkedProcessId?: string
  linkedProcessName?: string
  entryActivityId?: string
  exitActivityId?: string
  useExplicitFlow: boolean
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
  metadataJson?: string
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
  resourceType: number | string  // API returns string ("Person", "Tool", etc.) due to JsonStringEnumConverter
  linkedUserId?: string
  linkedUserName?: string
  createdAt: string
  updatedAt: string
  roleAssignmentCount: number
  functionCapabilityCount: number
}

interface ApiResourceSubtype {
  id: string
  name: string
  description?: string
  resourceType: number | string  // API returns string due to JsonStringEnumConverter
  icon?: string
  organizationId: string
  createdAt: string
  resourceCount: number
}

export interface ResourceSubtype {
  id: string
  name: string
  description?: string
  resourceType: 'person' | 'team' | 'tool' | 'automation' | 'partner' | 'asset'
  icon?: string
  organizationId: string
  createdAt: string
  resourceCount: number
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

// Map API enum values (PascalCase strings from JsonStringEnumConverter) to frontend lowercase
const activityTypeMap: Record<string, OpsActivity['activityType']> = {
  'Manual': 'manual',
  'Automated': 'automated',
  'Hybrid': 'hybrid',
  'Decision': 'decision',
  'Handoff': 'handoff',
  // IE symbols (ASME Y15.3)
  'Operation': 'operation',
  'Inspection': 'inspection',
  'Transport': 'transport',
  'Delay': 'delay',
  'Storage': 'storage',
  'Document': 'document',
  'Database': 'database',
  'ManualInput': 'manualInput',
  'Display': 'display',
  // Legacy number support (in case API ever returns numbers)
  '0': 'manual',
  '1': 'automated',
  '2': 'hybrid',
  '3': 'decision',
  '4': 'handoff',
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

const resourceStatusToNum: Record<OpsResource['status'], number> = {
  inactive: 0,
  active: 1,
  archived: 2,
}

const resourceSubtypeTypeMap: Record<number, ResourceSubtype['resourceType']> = {
  0: 'person',
  1: 'team',
  2: 'tool',
  3: 'automation',
  4: 'partner',
  5: 'asset',
}

// Map for string enum values from API (JsonStringEnumConverter)
const resourceSubtypeStringMap: Record<string, ResourceSubtype['resourceType']> = {
  Person: 'person',
  Team: 'team',
  Tool: 'tool',
  Automation: 'automation',
  Partner: 'partner',
  Asset: 'asset',
}

const resourceTypeToNum: Record<string, number> = {
  person: 0,
  team: 1,
  tool: 2,
  automation: 3,
  partner: 4,
  asset: 5,
}

function transformResourceSubtype(api: ApiResourceSubtype): ResourceSubtype {
  // Handle both number and string resourceType from API
  const resourceType = typeof api.resourceType === 'string'
    ? resourceSubtypeStringMap[api.resourceType] || 'person'
    : resourceSubtypeTypeMap[api.resourceType] || 'person'

  return {
    id: api.id,
    name: api.name,
    description: api.description,
    resourceType,
    icon: api.icon,
    organizationId: api.organizationId,
    createdAt: api.createdAt,
    resourceCount: api.resourceCount,
  }
}

// Transform API response to frontend types
function transformProcess(api: ApiProcess): ProcessWithActivities {
  return {
    id: api.id,
    organizationId: api.organizationId,
    functionId: api.functionId,
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
    entryActivityId: api.entryActivityId,
    exitActivityId: api.exitActivityId,
    useExplicitFlow: api.useExplicitFlow,
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
      metadataJson: a.metadataJson,
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

interface ApiRoleFunction {
  id: string
  roleId: string
  roleName: string
  roleDepartment?: string
  functionId: string
  functionName: string
  functionCategory?: string
  createdAt: string
}

export interface RoleFunction {
  id: string
  roleId: string
  roleName: string
  roleDepartment?: string
  functionId: string
  functionName: string
  functionCategory?: string
  createdAt: string
}

interface ApiFunctionCapability {
  id: string
  resourceId: string
  resourceName: string
  functionId: string
  functionName: string
  level: number | string // API may return number or string enum name
  certifiedDate?: string
  expiresAt?: string
  notes?: string
  createdAt: string
}

// Capability level mappings (number to string)
const capabilityLevelMap: Record<number, CapabilityLevel> = {
  0: 'learning',
  1: 'capable',
  2: 'proficient',
  3: 'expert',
  4: 'trainer',
}

// Capability level mappings (string enum name to lowercase)
const capabilityLevelStringMap: Record<string, CapabilityLevel> = {
  Learning: 'learning',
  Capable: 'capable',
  Proficient: 'proficient',
  Expert: 'expert',
  Trainer: 'trainer',
}

const capabilityLevelToNum: Record<CapabilityLevel, number> = {
  learning: 0,
  capable: 1,
  proficient: 2,
  expert: 3,
  trainer: 4,
}

function transformFunctionCapability(api: ApiFunctionCapability): FunctionCapability {
  // Handle both numeric and string enum values from API
  let level: CapabilityLevel = 'capable'
  if (typeof api.level === 'number') {
    level = capabilityLevelMap[api.level] || 'capable'
  } else if (typeof api.level === 'string') {
    level = capabilityLevelStringMap[api.level] || (api.level.toLowerCase() as CapabilityLevel) || 'capable'
  }

  return {
    id: api.id,
    resourceId: api.resourceId,
    resourceName: api.resourceName,
    functionId: api.functionId,
    functionName: api.functionName,
    level,
    certifiedDate: api.certifiedDate,
    expiresAt: api.expiresAt,
    notes: api.notes,
    createdAt: api.createdAt,
  }
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

// Shared state (singleton pattern - state persists across component instances)
const isLoading = ref(false)
const error = ref<string | null>(null)
const organizationId = ref(getOrganizationId())
const processes = ref<ProcessWithActivities[]>([])
const functions = ref<FunctionWithUsage[]>([])
const roles = ref<RoleWithAssignments[]>([])
const roleAssignments = ref<ApiRoleAssignment[]>([])
const functionCapabilities = ref<FunctionCapability[]>([])
const people = ref<OpsResource[]>([])
const resources = ref<OpsResource[]>([])
const resourceSubtypes = ref<ResourceSubtype[]>([])
const goals = ref<GoalWithProgress[]>([])
const canvases = ref<OpsCanvas[]>([])
const healthScore = ref<OrgHealthScore>(mockHealthScore)

// Business Model Canvas entities state
const partners = ref<Partner[]>([])
const channels = ref<Channel[]>([])
const valuePropositions = ref<ValueProposition[]>([])
const customerRelationships = ref<CustomerRelationship[]>([])
const revenueStreams = ref<RevenueStream[]>([])
const bmcCanvases = ref<Canvas[]>([])

// Org Chart state (shared across components)
const orgChart = ref<OrgChartTree | null>(null)
const orgChartMetrics = ref<OrgChartMetrics | null>(null)

export function useOperations() {
  const { get, post, put, patch, delete: del } = useApi()

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
      functionId: process.functionId,
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
      functionId: process.functionId,
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
    // Map lowercase frontend type to PascalCase enum string for backend (JsonStringEnumConverter)
    const typeToPascal: Record<string, string> = {
      manual: 'Manual', automated: 'Automated', hybrid: 'Hybrid', decision: 'Decision', handoff: 'Handoff',
      // IE symbols (ASME Y15.3)
      operation: 'Operation', inspection: 'Inspection', transport: 'Transport', delay: 'Delay', storage: 'Storage',
      document: 'Document', database: 'Database', manualInput: 'ManualInput', display: 'Display'
    }
    const activityTypeStr = activity.activityType ? typeToPascal[activity.activityType] : 'Manual'

    const data = await post<ApiActivity>(`/api/organizations/${organizationId.value}/operations/processes/${processId}/activities`, {
      name: activity.name,
      description: activity.description,
      order: activity.order,
      activityType: activityTypeStr,
      estimatedDurationMinutes: activity.estimatedDuration,
      instructions: activity.instructions,
      functionId: activity.functionId,
      assignedResourceId: activity.assignedResourceId,
      linkedProcessId: activity.linkedProcessId,
      positionX: activity.positionX || 0,
      positionY: activity.positionY || 0,
      metadataJson: activity.metadataJson,
    })
    return data
  }

  const updateActivity = async (processId: string, activityId: string, activity: Partial<OpsActivity>) => {
    // Map lowercase frontend type to PascalCase enum string for backend (JsonStringEnumConverter)
    const typeToPascal: Record<string, string> = {
      manual: 'Manual', automated: 'Automated', hybrid: 'Hybrid', decision: 'Decision', handoff: 'Handoff',
      // IE symbols (ASME Y15.3)
      operation: 'Operation', inspection: 'Inspection', transport: 'Transport', delay: 'Delay', storage: 'Storage',
      document: 'Document', database: 'Database', manualInput: 'ManualInput', display: 'Display'
    }
    const activityTypeStr = activity.activityType ? typeToPascal[activity.activityType] : undefined

    // Build request body - explicitly include linkedProcessId even when null to clear it
    const requestBody: Record<string, unknown> = {
      name: activity.name,
      description: activity.description,
      order: activity.order,
      activityType: activityTypeStr,
      estimatedDurationMinutes: activity.estimatedDuration,
      instructions: activity.instructions,
      functionId: activity.functionId,
      assignedResourceId: activity.assignedResourceId,
      positionX: activity.positionX || 0,
      positionY: activity.positionY || 0,
      metadataJson: activity.metadataJson,
    }

    // Explicitly include linkedProcessId to ensure null values are sent to clear the field
    if ('linkedProcessId' in activity) {
      requestBody.linkedProcessId = activity.linkedProcessId
    }

    const data = await put<ApiActivity>(`/api/organizations/${organizationId.value}/operations/processes/${processId}/activities/${activityId}`, requestBody)
    return data
  }

  const deleteActivity = async (processId: string, activityId: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/processes/${processId}/activities/${activityId}`)
  }

  // Activity Positions (Vue Flow bulk update)
  const updateActivityPositions = async (processId: string, positions: Array<{ activityId: string; positionX: number; positionY: number }>) => {
    const config = useRuntimeConfig()
    await fetch(`${config.public.apiBaseUrl}/api/organizations/${organizationId.value}/operations/processes/${processId}/activities/positions`, {
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

  // Flow Endpoints (Start/End node connections)
  const updateFlowEndpoints = async (processId: string, updates: { entryActivityId?: string; exitActivityId?: string; clearEntry?: boolean; clearExit?: boolean }) => {
    const config = useRuntimeConfig()
    const response = await fetch(`${config.public.apiBaseUrl}/api/organizations/${organizationId.value}/operations/processes/${processId}/flow-endpoints`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        entryActivityId: updates.entryActivityId,
        exitActivityId: updates.exitActivityId,
        clearEntry: updates.clearEntry || false,
        clearExit: updates.clearExit || false,
      }),
    })
    if (!response.ok) {
      throw new Error('Failed to update flow endpoints')
    }
    return response.json()
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

  const deleteGoal = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/goals/${id}`)
    // Remove from goals array (handles both root goals and nested KRs)
    const removeGoal = (goalsList: GoalWithProgress[]): GoalWithProgress[] => {
      return goalsList.filter(g => {
        if (g.id === id) return false
        if (g.keyResults) {
          g.keyResults = removeGoal(g.keyResults)
        }
        return true
      })
    }
    goals.value = removeGoal(goals.value)
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
      // Filter people (Person type = 0 or "Person" string from API with JsonStringEnumConverter)
      people.value = data.filter(r => r.resourceType === 0 || r.resourceType === 'Person').map(transformResource)
    } catch (e) {
      console.error('Failed to fetch resources:', e)
      error.value = 'Failed to fetch resources'
    } finally {
      isLoading.value = false
    }
  }

  const fetchResourceSubtypes = async () => {
    try {
      const data = await get<ApiResourceSubtype[]>(`/api/organizations/${organizationId.value}/operations/resources/subtypes`)
      resourceSubtypes.value = data.map(transformResourceSubtype)
      return resourceSubtypes.value
    } catch (e) {
      console.error('Failed to fetch resource subtypes:', e)
      return []
    }
  }

  const createResourceSubtype = async (subtype: { name: string; description?: string; resourceType: string; icon?: string }) => {
    const data = await post<ApiResourceSubtype>(`/api/organizations/${organizationId.value}/operations/resources/subtypes`, {
      name: subtype.name,
      description: subtype.description,
      resourceType: resourceTypeToNum[subtype.resourceType] ?? 0,
      icon: subtype.icon,
    })
    const transformed = transformResourceSubtype(data)
    resourceSubtypes.value.push(transformed)
    return transformed
  }

  const createResource = async (resource: { name: string; description?: string; status?: OpsResource['status']; resourceSubtypeId: string; linkedUserId?: string; metadata?: string }) => {
    const data = await post<ApiResource>(`/api/organizations/${organizationId.value}/operations/resources`, {
      name: resource.name,
      description: resource.description,
      status: resource.status ? resourceStatusToNum[resource.status] : 1, // Default to active
      resourceSubtypeId: resource.resourceSubtypeId,
      linkedUserId: resource.linkedUserId,
      metadata: resource.metadata,
    })
    const transformed = transformResource(data)
    resources.value.push(transformed)
    // Update people list if it's a person type
    if (data.resourceType === 0) {
      people.value.push(transformed)
    }
    return transformed
  }

  const updateResource = async (id: string, resource: { name: string; description?: string; status?: OpsResource['status']; linkedUserId?: string; metadata?: string }) => {
    const data = await put<ApiResource>(`/api/organizations/${organizationId.value}/operations/resources/${id}`, {
      name: resource.name,
      description: resource.description,
      status: resource.status ? resourceStatusToNum[resource.status] : 1,
      linkedUserId: resource.linkedUserId,
      metadata: resource.metadata,
    })
    const transformed = transformResource(data)
    const index = resources.value.findIndex(r => r.id === id)
    if (index !== -1) {
      resources.value[index] = transformed
    }
    // Update people list if it's a person type
    if (data.resourceType === 0) {
      const peopleIndex = people.value.findIndex(p => p.id === id)
      if (peopleIndex !== -1) {
        people.value[peopleIndex] = transformed
      }
    }
    return transformed
  }

  const deleteResource = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/resources/${id}`)
    resources.value = resources.value.filter(r => r.id !== id)
    people.value = people.value.filter(p => p.id !== id)
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

  // Role-Function Assignments (bidirectional)
  const fetchRoleFunctions = async (roleId: string): Promise<RoleFunction[]> => {
    const data = await get<ApiRoleFunction[]>(`/api/organizations/${organizationId.value}/operations/roles/${roleId}/functions`)
    return data
  }

  const assignFunctionToRole = async (roleId: string, functionId: string): Promise<RoleFunction> => {
    const data = await post<ApiRoleFunction>(`/api/organizations/${organizationId.value}/operations/roles/${roleId}/functions`, {
      functionId,
    })
    return data
  }

  const unassignFunctionFromRole = async (roleId: string, functionId: string): Promise<void> => {
    await del(`/api/organizations/${organizationId.value}/operations/roles/${roleId}/functions/${functionId}`)
  }

  const fetchFunctionRoles = async (functionId: string): Promise<RoleFunction[]> => {
    const data = await get<ApiRoleFunction[]>(`/api/organizations/${organizationId.value}/operations/functions/${functionId}/roles`)
    return data
  }

  const assignRoleToFunction = async (functionId: string, roleId: string): Promise<RoleFunction> => {
    const data = await post<ApiRoleFunction>(`/api/organizations/${organizationId.value}/operations/functions/${functionId}/roles`, {
      roleId,
    })
    return data
  }

  const unassignRoleFromFunction = async (functionId: string, roleId: string): Promise<void> => {
    await del(`/api/organizations/${organizationId.value}/operations/functions/${functionId}/roles/${roleId}`)
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

  // Function Capabilities
  const fetchFunctionCapabilities = async (resourceId?: string, functionId?: string) => {
    let url = `/api/organizations/${organizationId.value}/operations/resources/function-capabilities`
    const params = new URLSearchParams()
    if (resourceId) params.append('resourceId', resourceId)
    if (functionId) params.append('functionId', functionId)
    if (params.toString()) url += `?${params.toString()}`

    const data = await get<ApiFunctionCapability[]>(url)
    const transformed = data.map(transformFunctionCapability)
    functionCapabilities.value = transformed
    return transformed
  }

  const createFunctionCapability = async (capability: {
    resourceId: string
    functionId: string
    level?: CapabilityLevel
    certifiedDate?: string
    expiresAt?: string
    notes?: string
  }) => {
    const data = await post<ApiFunctionCapability>(`/api/organizations/${organizationId.value}/operations/resources/function-capabilities`, {
      resourceId: capability.resourceId,
      functionId: capability.functionId,
      level: capability.level ? capabilityLevelToNum[capability.level] : 1, // Default to 'capable'
      certifiedDate: capability.certifiedDate,
      expiresAt: capability.expiresAt,
      notes: capability.notes,
    })
    const transformed = transformFunctionCapability(data)
    functionCapabilities.value.push(transformed)
    return transformed
  }

  const deleteFunctionCapability = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/resources/function-capabilities/${id}`)
    functionCapabilities.value = functionCapabilities.value.filter(fc => fc.id !== id)
  }

  const updateFunctionCapability = async (id: string, updates: {
    level?: CapabilityLevel
    certifiedDate?: string
    expiresAt?: string
    notes?: string
  }) => {
    const data = await put<ApiFunctionCapability>(
      `/api/organizations/${organizationId.value}/operations/resources/function-capabilities/${id}`,
      {
        level: updates.level ? capabilityLevelToNum[updates.level] : undefined,
        certifiedDate: updates.certifiedDate,
        expiresAt: updates.expiresAt,
        notes: updates.notes,
      }
    )
    const transformed = transformFunctionCapability(data)
    // Use splice for proper Vue reactivity when updating array item
    const index = functionCapabilities.value.findIndex(fc => fc.id === id)
    if (index !== -1) {
      functionCapabilities.value.splice(index, 1, transformed)
    }
    return transformed
  }

  // Helper to get capabilities for a specific person
  const getPersonCapabilities = (resourceId: string) => {
    return computed(() => functionCapabilities.value.filter(fc => fc.resourceId === resourceId))
  }

  // Helper to get capable people for a specific function
  const getFunctionCapablePeople = (functionId: string) => {
    return computed(() => functionCapabilities.value.filter(fc => fc.functionId === functionId))
  }

  // People (subset of resources)
  const fetchPeople = async () => {
    await fetchResources()
  }

  // Health Score (mock for now - would need calculation endpoint)
  const fetchHealthScore = async () => {
    healthScore.value = mockHealthScore
  }

  // ===========================================
  // Business Model Canvas API Methods
  // ===========================================

  // Partners
  const fetchPartners = async (filters?: { type?: PartnerType; status?: PartnerStatus }) => {
    let url = `/api/organizations/${organizationId.value}/operations/partners`
    const params = new URLSearchParams()
    if (filters?.type) params.append('type', filters.type)
    if (filters?.status) params.append('status', filters.status)
    if (params.toString()) url += `?${params.toString()}`

    const data = await get<Partner[]>(url)
    partners.value = data
    return data
  }

  const getPartner = async (id: string): Promise<Partner | null> => {
    try {
      return await get<Partner>(`/api/organizations/${organizationId.value}/operations/partners/${id}`)
    } catch {
      return null
    }
  }

  const createPartner = async (partner: Partial<Partner>): Promise<Partner> => {
    const data = await post<Partner>(`/api/organizations/${organizationId.value}/operations/partners`, partner)
    partners.value.push(data)
    return data
  }

  const updatePartner = async (id: string, partner: Partial<Partner>): Promise<Partner> => {
    const data = await put<Partner>(`/api/organizations/${organizationId.value}/operations/partners/${id}`, partner)
    const index = partners.value.findIndex(p => p.id === id)
    if (index !== -1) partners.value[index] = data
    return data
  }

  const deletePartner = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/partners/${id}`)
    partners.value = partners.value.filter(p => p.id !== id)
  }

  // Channels
  const fetchChannels = async (filters?: { type?: ChannelType; category?: ChannelCategory; status?: ChannelStatus }) => {
    let url = `/api/organizations/${organizationId.value}/operations/channels`
    const params = new URLSearchParams()
    if (filters?.type) params.append('type', filters.type)
    if (filters?.category) params.append('category', filters.category)
    if (filters?.status) params.append('status', filters.status)
    if (params.toString()) url += `?${params.toString()}`

    const data = await get<Channel[]>(url)
    channels.value = data
    return data
  }

  const getChannel = async (id: string): Promise<Channel | null> => {
    try {
      return await get<Channel>(`/api/organizations/${organizationId.value}/operations/channels/${id}`)
    } catch {
      return null
    }
  }

  const createChannel = async (channel: Partial<Channel>): Promise<Channel> => {
    const data = await post<Channel>(`/api/organizations/${organizationId.value}/operations/channels`, channel)
    channels.value.push(data)
    return data
  }

  const updateChannel = async (id: string, channel: Partial<Channel>): Promise<Channel> => {
    const data = await put<Channel>(`/api/organizations/${organizationId.value}/operations/channels/${id}`, channel)
    const index = channels.value.findIndex(c => c.id === id)
    if (index !== -1) channels.value[index] = data
    return data
  }

  const deleteChannel = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/channels/${id}`)
    channels.value = channels.value.filter(c => c.id !== id)
  }

  // Value Propositions
  const fetchValuePropositions = async (filters?: { status?: ValuePropositionStatus; productId?: string; segmentId?: string }) => {
    let url = `/api/organizations/${organizationId.value}/operations/valuepropositions`
    const params = new URLSearchParams()
    if (filters?.status) params.append('status', filters.status)
    if (filters?.productId) params.append('productId', filters.productId)
    if (filters?.segmentId) params.append('segmentId', filters.segmentId)
    if (params.toString()) url += `?${params.toString()}`

    const data = await get<ValueProposition[]>(url)
    valuePropositions.value = data
    return data
  }

  const getValueProposition = async (id: string): Promise<ValueProposition | null> => {
    try {
      return await get<ValueProposition>(`/api/organizations/${organizationId.value}/operations/valuepropositions/${id}`)
    } catch {
      return null
    }
  }

  const createValueProposition = async (vp: Partial<ValueProposition>): Promise<ValueProposition> => {
    const data = await post<ValueProposition>(`/api/organizations/${organizationId.value}/operations/valuepropositions`, vp)
    valuePropositions.value.push(data)
    return data
  }

  const updateValueProposition = async (id: string, vp: Partial<ValueProposition>): Promise<ValueProposition> => {
    const data = await put<ValueProposition>(`/api/organizations/${organizationId.value}/operations/valuepropositions/${id}`, vp)
    const index = valuePropositions.value.findIndex(v => v.id === id)
    if (index !== -1) valuePropositions.value[index] = data
    return data
  }

  const deleteValueProposition = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/valuepropositions/${id}`)
    valuePropositions.value = valuePropositions.value.filter(v => v.id !== id)
  }

  // Customer Relationships
  const fetchCustomerRelationships = async (filters?: { type?: CustomerRelationshipType; status?: CustomerRelationshipStatus; segmentId?: string }) => {
    let url = `/api/organizations/${organizationId.value}/operations/customerrelationships`
    const params = new URLSearchParams()
    if (filters?.type) params.append('type', filters.type)
    if (filters?.status) params.append('status', filters.status)
    if (filters?.segmentId) params.append('segmentId', filters.segmentId)
    if (params.toString()) url += `?${params.toString()}`

    const data = await get<CustomerRelationship[]>(url)
    customerRelationships.value = data
    return data
  }

  const getCustomerRelationship = async (id: string): Promise<CustomerRelationship | null> => {
    try {
      return await get<CustomerRelationship>(`/api/organizations/${organizationId.value}/operations/customerrelationships/${id}`)
    } catch {
      return null
    }
  }

  const createCustomerRelationship = async (cr: Partial<CustomerRelationship>): Promise<CustomerRelationship> => {
    const data = await post<CustomerRelationship>(`/api/organizations/${organizationId.value}/operations/customerrelationships`, cr)
    customerRelationships.value.push(data)
    return data
  }

  const updateCustomerRelationship = async (id: string, cr: Partial<CustomerRelationship>): Promise<CustomerRelationship> => {
    const data = await put<CustomerRelationship>(`/api/organizations/${organizationId.value}/operations/customerrelationships/${id}`, cr)
    const index = customerRelationships.value.findIndex(c => c.id === id)
    if (index !== -1) customerRelationships.value[index] = data
    return data
  }

  const deleteCustomerRelationship = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/customerrelationships/${id}`)
    customerRelationships.value = customerRelationships.value.filter(c => c.id !== id)
  }

  // BMC Canvases (Business Model Canvas CRUD)
  const fetchBmcCanvases = async (filters?: { scopeType?: CanvasScopeType; status?: NewCanvasStatus; productId?: string }) => {
    let url = `/api/organizations/${organizationId.value}/operations/canvases/bmc`
    const params = new URLSearchParams()
    if (filters?.scopeType) params.append('scopeType', filters.scopeType)
    if (filters?.status) params.append('status', filters.status)
    if (filters?.productId) params.append('productId', filters.productId)
    if (params.toString()) url += `?${params.toString()}`

    const data = await get<Canvas[]>(url)
    bmcCanvases.value = data
    return data
  }

  const getBmcCanvas = async (id: string): Promise<Canvas | null> => {
    try {
      return await get<Canvas>(`/api/organizations/${organizationId.value}/operations/canvases/bmc/${id}`)
    } catch {
      return null
    }
  }

  const createBmcCanvas = async (canvas: Partial<Canvas>): Promise<Canvas> => {
    const data = await post<Canvas>(`/api/organizations/${organizationId.value}/operations/canvases/bmc`, canvas)
    bmcCanvases.value.push(data)
    return data
  }

  const updateBmcCanvas = async (id: string, canvas: Partial<Canvas>): Promise<Canvas> => {
    const data = await put<Canvas>(`/api/organizations/${organizationId.value}/operations/canvases/bmc/${id}`, canvas)
    const index = bmcCanvases.value.findIndex(c => c.id === id)
    if (index !== -1) bmcCanvases.value[index] = data
    return data
  }

  const deleteBmcCanvas = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/canvases/bmc/${id}`)
    bmcCanvases.value = bmcCanvases.value.filter(c => c.id !== id)
  }

  // Revenue Streams
  const fetchRevenueStreams = async (filters?: { type?: RevenueStreamType; status?: RevenueStreamStatus; productId?: string; segmentId?: string }) => {
    let url = `/api/organizations/${organizationId.value}/operations/revenuestreams`
    const params = new URLSearchParams()
    if (filters?.type) params.append('type', filters.type)
    if (filters?.status) params.append('status', filters.status)
    if (filters?.productId) params.append('productId', filters.productId)
    if (filters?.segmentId) params.append('segmentId', filters.segmentId)
    if (params.toString()) url += `?${params.toString()}`

    const data = await get<RevenueStream[]>(url)
    revenueStreams.value = data
    return data
  }

  const getRevenueStream = async (id: string): Promise<RevenueStream | null> => {
    try {
      return await get<RevenueStream>(`/api/organizations/${organizationId.value}/operations/revenuestreams/${id}`)
    } catch {
      return null
    }
  }

  const createRevenueStream = async (rs: Partial<RevenueStream>): Promise<RevenueStream> => {
    const data = await post<RevenueStream>(`/api/organizations/${organizationId.value}/operations/revenuestreams`, rs)
    revenueStreams.value.push(data)
    return data
  }

  const updateRevenueStream = async (id: string, rs: Partial<RevenueStream>): Promise<RevenueStream> => {
    const data = await put<RevenueStream>(`/api/organizations/${organizationId.value}/operations/revenuestreams/${id}`, rs)
    const index = revenueStreams.value.findIndex(r => r.id === id)
    if (index !== -1) revenueStreams.value[index] = data
    return data
  }

  const deleteRevenueStream = async (id: string) => {
    await del(`/api/organizations/${organizationId.value}/operations/revenuestreams/${id}`)
    revenueStreams.value = revenueStreams.value.filter(r => r.id !== id)
  }

  // ===========================================
  // Org Chart Functions
  // ===========================================

  const fetchOrgChart = async (): Promise<OrgChartTree> => {
    const data = await get<OrgChartTree>(`/api/organizations/${organizationId.value}/operations/resources/org-chart`)
    orgChart.value = data
    return data
  }

  const fetchOrgChartMetrics = async (): Promise<OrgChartMetrics> => {
    const data = await get<OrgChartMetrics>(`/api/organizations/${organizationId.value}/operations/resources/org-chart/metrics`)
    orgChartMetrics.value = data
    return data
  }

  const updateReporting = async (resourceId: string, reportsToResourceId: string | null): Promise<OrgChartResource> => {
    const data = await patch<OrgChartResource>(
      `/api/organizations/${organizationId.value}/operations/resources/${resourceId}/reporting`,
      { reportsToResourceId }
    )
    // Refresh org chart after update
    await fetchOrgChart()
    return data
  }

  const createVacancy = async (vacancy: {
    vacantPositionTitle: string
    reportsToResourceId?: string
    resourceSubtypeId: string
    description?: string
  }): Promise<OrgChartResource> => {
    const data = await post<OrgChartResource>(
      `/api/organizations/${organizationId.value}/operations/resources/vacancies`,
      vacancy
    )
    // Refresh org chart after creating vacancy
    await fetchOrgChart()
    return data
  }

  const fillVacancy = async (vacancyId: string, personData: {
    name: string
    description?: string
    linkedUserId?: string
  }): Promise<OrgChartResource> => {
    const data = await post<OrgChartResource>(
      `/api/organizations/${organizationId.value}/operations/resources/vacancies/${vacancyId}/fill`,
      personData
    )
    // Refresh org chart after filling vacancy
    await fetchOrgChart()
    return data
  }

  // ===========================================
  // Block Reference Functions (Canvas entity linking)
  // ===========================================

  /**
   * Map numeric CanvasBlockType to API string
   */
  const blockTypeToApiString = (blockType: CanvasBlockType | number): string => {
    const mapping: Record<string, string> = {
      KeyPartners: 'KeyPartners',
      KeyActivities: 'KeyActivities',
      KeyResources: 'KeyResources',
      ValuePropositions: 'ValuePropositions',
      CustomerRelationships: 'CustomerRelationships',
      Channels: 'Channels',
      CustomerSegments: 'CustomerSegments',
      CostStructure: 'CostStructure',
      RevenueStreams: 'RevenueStreams',
    }
    // If it's a number, convert to string name
    if (typeof blockType === 'number') {
      const numMapping: Record<number, string> = {
        0: 'KeyPartners',
        1: 'KeyActivities',
        2: 'KeyResources',
        3: 'ValuePropositions',
        4: 'CustomerRelationships',
        5: 'Channels',
        6: 'CustomerSegments',
        7: 'CostStructure',
        8: 'RevenueStreams',
      }
      return numMapping[blockType] || String(blockType)
    }
    return mapping[blockType] || String(blockType)
  }

  /**
   * Fetch all block references for a canvas block
   */
  const fetchBlockReferences = async (canvasId: string, blockType: CanvasBlockType | number): Promise<BlockReference[]> => {
    const blockTypeStr = blockTypeToApiString(blockType)
    const data = await get<BlockReference[]>(
      `/api/organizations/${organizationId.value}/operations/canvases/${canvasId}/blocks/${blockTypeStr}/references`
    )
    return data
  }

  /**
   * Add a reference to a canvas block (link an entity)
   */
  const addBlockReference = async (
    canvasId: string,
    blockType: CanvasBlockType | number,
    reference: {
      entityType: ReferenceEntityType
      entityId: string
      role?: ReferenceRole
      linkType?: ReferenceLinkType
      contextNote?: string
      sortOrder?: number
      isHighlighted?: boolean
    }
  ): Promise<BlockReference> => {
    const blockTypeStr = blockTypeToApiString(blockType)
    const data = await post<BlockReference>(
      `/api/organizations/${organizationId.value}/operations/canvases/${canvasId}/blocks/${blockTypeStr}/references`,
      {
        entityType: reference.entityType,
        entityId: reference.entityId,
        role: reference.role || 'Primary',
        linkType: reference.linkType || 'Linked',
        contextNote: reference.contextNote,
        sortOrder: reference.sortOrder || 0,
        isHighlighted: reference.isHighlighted || false,
      }
    )
    return data
  }

  /**
   * Update a block reference
   */
  const updateBlockReference = async (
    canvasId: string,
    blockType: CanvasBlockType | number,
    referenceId: string,
    updates: Partial<BlockReference>
  ): Promise<BlockReference> => {
    const blockTypeStr = blockTypeToApiString(blockType)
    const data = await put<BlockReference>(
      `/api/organizations/${organizationId.value}/operations/canvases/${canvasId}/blocks/${blockTypeStr}/references/${referenceId}`,
      updates
    )
    return data
  }

  /**
   * Remove a block reference (unlink an entity)
   */
  const removeBlockReference = async (
    canvasId: string,
    blockType: CanvasBlockType | number,
    referenceId: string
  ): Promise<void> => {
    const blockTypeStr = blockTypeToApiString(blockType)
    await del(
      `/api/organizations/${organizationId.value}/operations/canvases/${canvasId}/blocks/${blockTypeStr}/references/${referenceId}`
    )
  }

  /**
   * Reorder block references within a block
   */
  const reorderBlockReferences = async (
    canvasId: string,
    blockType: CanvasBlockType | number,
    referenceIds: string[]
  ): Promise<void> => {
    const blockTypeStr = blockTypeToApiString(blockType)
    await put(
      `/api/organizations/${organizationId.value}/operations/canvases/${canvasId}/blocks/${blockTypeStr}/references/reorder`,
      referenceIds
    )
  }

  /**
   * Get canvas with enriched block references (includes people under roles, activities in processes)
   * This is the endpoint for AI consumption and detailed UI views.
   */
  const getCanvasEnriched = async (canvasId: string): Promise<Canvas | null> => {
    try {
      return await get<Canvas>(`/api/organizations/${organizationId.value}/operations/canvases/${canvasId}/enriched`)
    } catch {
      return null
    }
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

  // Clear all cached data (call when organization changes)
  const clearAllData = () => {
    processes.value = []
    functions.value = []
    roles.value = []
    roleAssignments.value = []
    functionCapabilities.value = []
    people.value = []
    resources.value = []
    resourceSubtypes.value = []
    goals.value = []
    canvases.value = []
    partners.value = []
    channels.value = []
    valuePropositions.value = []
    customerRelationships.value = []
    revenueStreams.value = []
    bmcCanvases.value = []
    orgChart.value = null
    orgChartMetrics.value = null
    error.value = null
  }

  // Refresh organization ID from localStorage (call when org changes)
  const refreshOrganizationId = () => {
    organizationId.value = getOrganizationId()
  }

  // Watch for organization changes and update internal state
  // This ensures useOperations always uses the current organization
  const { currentOrganizationId: watchedOrgId } = useOrganizations()
  watch(watchedOrgId, (newOrgId) => {
    if (newOrgId && newOrgId !== organizationId.value) {
      // Clear all cached data from old organization
      clearAllData()
      // Update to new organization ID
      organizationId.value = newOrgId
    }
  }, { immediate: true })

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
    // Flow Endpoints (Start/End connections)
    updateFlowEndpoints,
    // Goal Actions
    fetchGoals,
    createGoal,
    updateGoal,
    deleteGoal,
    // Canvas Actions
    fetchCanvases,
    fetchCanvasById,
    updateCanvasBlock,
    // Resource Actions
    fetchResources,
    fetchResourceSubtypes,
    createResourceSubtype,
    createResource,
    updateResource,
    deleteResource,
    resourceSubtypes,
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
    // Role-Function Assignment Actions
    fetchRoleFunctions,
    assignFunctionToRole,
    unassignFunctionFromRole,
    fetchFunctionRoles,
    assignRoleToFunction,
    unassignRoleFromFunction,
    // Role Assignment Actions
    fetchRoleAssignments,
    createRoleAssignment,
    deleteRoleAssignment,
    // Function Capability Actions
    functionCapabilities,
    fetchFunctionCapabilities,
    createFunctionCapability,
    updateFunctionCapability,
    deleteFunctionCapability,
    getPersonCapabilities,
    getFunctionCapablePeople,
    // Other Actions
    fetchPeople,
    fetchHealthScore,
    // Business Model Canvas - State
    partners,
    channels,
    valuePropositions,
    customerRelationships,
    revenueStreams,
    bmcCanvases,
    // Business Model Canvas - Partner Actions
    fetchPartners,
    getPartner,
    createPartner,
    updatePartner,
    deletePartner,
    // Business Model Canvas - Channel Actions
    fetchChannels,
    getChannel,
    createChannel,
    updateChannel,
    deleteChannel,
    // Business Model Canvas - Value Proposition Actions
    fetchValuePropositions,
    getValueProposition,
    createValueProposition,
    updateValueProposition,
    deleteValueProposition,
    // Business Model Canvas - Customer Relationship Actions
    fetchCustomerRelationships,
    getCustomerRelationship,
    createCustomerRelationship,
    updateCustomerRelationship,
    deleteCustomerRelationship,
    // Business Model Canvas - Revenue Stream Actions
    fetchRevenueStreams,
    getRevenueStream,
    createRevenueStream,
    updateRevenueStream,
    deleteRevenueStream,
    // Business Model Canvas - Canvas CRUD Actions
    fetchBmcCanvases,
    getBmcCanvas,
    createBmcCanvas,
    updateBmcCanvas,
    deleteBmcCanvas,
    // Block Reference Actions (Canvas entity linking)
    fetchBlockReferences,
    addBlockReference,
    updateBlockReference,
    removeBlockReference,
    reorderBlockReferences,
    getCanvasEnriched,
    // Org Chart - State
    orgChart,
    orgChartMetrics,
    // Org Chart - Actions
    fetchOrgChart,
    fetchOrgChartMetrics,
    updateReporting,
    createVacancy,
    fillVacancy,
    // Organization
    refreshOrganizationId,
  }
}
