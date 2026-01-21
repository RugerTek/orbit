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
  functionId?: string
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
  linkedProcessId?: string | null // Subprocess linking - drill down capability (null to clear)
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
  /** The activity connected from the Start node */
  entryActivityId?: string
  /** The activity connected to the End node */
  exitActivityId?: string
  /** When true, the process uses explicit flow mode (user-defined edges only) */
  useExplicitFlow: boolean
}

/** Summary of a linked subprocess for "portal" display in activity details */
export interface LinkedProcessSummary {
  id: string
  name: string
  purpose?: string
  trigger?: string
  output?: string
  activityCount: number
}

export interface ActivityWithDetails extends OpsActivity {
  function?: OpsFunction
  assignedResource?: OpsResource
  /** Full subprocess details for "portal" display - includes purpose, trigger, output, activityCount */
  linkedProcess?: LinkedProcessSummary
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

// Function Capability - links people to functions they can perform
export type CapabilityLevel = 'learning' | 'capable' | 'proficient' | 'expert' | 'trainer'

export interface FunctionCapability {
  id: string
  resourceId: string
  resourceName: string
  functionId: string
  functionName: string
  level: CapabilityLevel
  certifiedDate?: string
  expiresAt?: string
  notes?: string
  createdAt: string
}

// Person with their function capabilities
export interface PersonWithCapabilities extends OpsResource {
  capabilities: FunctionCapability[]
  roleAssignments: Array<{ id: string; roleId: string; roleName: string; allocationPercentage?: number; isPrimary: boolean }>
}

// ===========================================
// Org Chart Types
// ===========================================

export type OrgChartViewMode = 'tree' | 'list' | 'card'

/** Resource with org chart hierarchy information */
export interface OrgChartResource {
  id: string
  name: string
  description?: string
  status: 'planned' | 'active' | 'deprecated'
  organizationId: string
  resourceSubtypeId: string
  resourceSubtypeName: string
  resourceType: 'person' | 'tool' | 'partner' | 'asset'
  linkedUserId?: string
  linkedUserName?: string
  createdAt: string
  updatedAt: string
  // Org Chart hierarchy fields
  reportsToResourceId?: string
  managerName?: string
  isVacant: boolean
  vacantPositionTitle?: string
  // Computed metrics
  directReportsCount: number
  indirectReportsCount: number
  managementDepth: number
  // Children for tree view
  directReports?: OrgChartResource[]
}

/** Full org chart tree structure for visualization */
export interface OrgChartTree {
  /** Top-level resources with no manager */
  rootNodes: OrgChartResource[]
  /** Total count of people (excluding vacancies) */
  totalPeople: number
  /** Total count of unfilled positions */
  totalVacancies: number
  /** Maximum depth of the org chart hierarchy */
  maxDepth: number
  /** Count of people at each level (depth -> count) */
  peopleByDepth: Record<number, number>
}

/** Summary metrics for organizational health and span of control */
export interface OrgChartMetrics {
  totalPeople: number
  totalVacancies: number
  maxDepth: number
  /** Average number of direct reports per manager */
  averageSpanOfControl: number
  /** Breakdown of span of control for each manager */
  spanOfControlByManager: SpanOfControlEntry[]
}

export interface SpanOfControlEntry {
  managerId: string
  managerName: string
  directReports: number
  indirectReports: number
  depth: number
}

// ===========================================
// Business Model Canvas Types
// ===========================================

// Partner Types
export type PartnerType = 'Supplier' | 'Distributor' | 'Strategic' | 'Technology' | 'Agency' | 'Reseller' | 'Affiliate' | 'JointVenture'
export type PartnerStatus = 'Prospective' | 'Active' | 'OnHold' | 'Terminated'
export type StrategicValue = 'Critical' | 'High' | 'Medium' | 'Low'

export interface Partner {
  id: string
  organizationId: string
  name: string
  slug?: string
  description?: string
  type: PartnerType
  status: PartnerStatus
  strategicValue: StrategicValue
  relationshipStrength?: number // 1-5
  website?: string
  contactJson?: string
  contractJson?: string
  servicesProvidedJson?: string
  servicesReceivedJson?: string
  costJson?: string
  tagsJson?: string
  createdAt: string
  updatedAt: string
  channelCount: number
  canvasReferenceCount: number
}

// Channel Types
export type ChannelType = 'Direct' | 'Indirect' | 'Digital' | 'Physical' | 'Hybrid'
export type ChannelCategory = 'Sales' | 'Marketing' | 'Distribution' | 'Support' | 'Communication'
export type ChannelStatus = 'Planned' | 'Active' | 'Optimizing' | 'Sunset' | 'Inactive'
export type ChannelOwnership = 'Owned' | 'Partner' | 'ThirdParty'

export interface Channel {
  id: string
  organizationId: string
  name: string
  slug?: string
  description?: string
  type: ChannelType
  category: ChannelCategory
  status: ChannelStatus
  ownership: ChannelOwnership
  phasesJson?: string
  metricsJson?: string
  costJson?: string
  integrationJson?: string
  tagsJson?: string
  partnerId?: string
  partnerName?: string
  createdAt: string
  updatedAt: string
  canvasReferenceCount: number
}

// Value Proposition Types
export type ValuePropositionStatus = 'Draft' | 'Validated' | 'Active' | 'Testing' | 'Retired'

export interface ValueProposition {
  id: string
  organizationId: string
  name: string
  slug?: string
  headline: string
  description?: string
  status: ValuePropositionStatus
  customerJobsJson?: string
  painsJson?: string
  gainsJson?: string
  painRelieversJson?: string
  gainCreatorsJson?: string
  productsServicesJson?: string
  differentiatorsJson?: string
  validationJson?: string
  tagsJson?: string
  productId?: string
  productName?: string
  segmentId?: string
  segmentName?: string
  createdAt: string
  updatedAt: string
  canvasReferenceCount: number
}

// Customer Relationship Types
export type CustomerRelationshipType = 'PersonalAssistance' | 'DedicatedAssistance' | 'SelfService' | 'AutomatedService' | 'Communities' | 'CoCreation'
export type CustomerRelationshipStatus = 'Planned' | 'Active' | 'Optimizing' | 'Sunset'

export interface CustomerRelationship {
  id: string
  organizationId: string
  name: string
  slug?: string
  description?: string
  type: CustomerRelationshipType
  status: CustomerRelationshipStatus
  purposeJson?: string
  touchpointsJson?: string
  lifecycleJson?: string
  metricsJson?: string
  costJson?: string
  expectationsJson?: string
  tagsJson?: string
  segmentId?: string
  segmentName?: string
  createdAt: string
  updatedAt: string
  canvasReferenceCount: number
}

// Revenue Stream Types
export type RevenueStreamType = 'AssetSale' | 'UsageFee' | 'Subscription' | 'Licensing' | 'Brokerage' | 'Advertising' | 'Leasing' | 'Commission'
export type RevenueStreamStatus = 'Planned' | 'Active' | 'Growing' | 'Mature' | 'Declining' | 'Sunset'
export type PricingMechanism = 'Fixed' | 'Dynamic' | 'Negotiated' | 'Auction' | 'MarketDependent' | 'VolumeDependent'

export interface RevenueStream {
  id: string
  organizationId: string
  name: string
  slug?: string
  description?: string
  type: RevenueStreamType
  status: RevenueStreamStatus
  pricingMechanism: PricingMechanism
  pricingJson?: string
  revenueJson?: string
  metricsJson?: string
  willingnessToPayJson?: string
  tagsJson?: string
  productId?: string
  productName?: string
  segmentId?: string
  segmentName?: string
  createdAt: string
  updatedAt: string
  canvasReferenceCount: number
}

// Block Reference Types (Canvas to Entity linking)
export type ReferenceEntityType = 'Resource' | 'Process' | 'Activity' | 'Product' | 'Segment' | 'Function' | 'Partner' | 'Channel' | 'ValueProposition' | 'CustomerRelationship' | 'RevenueStream'
export type ReferenceRole = 'Primary' | 'Supporting' | 'Enabling' | 'Dependency'
export type ReferenceLinkType = 'Linked' | 'Copied'

export interface BlockReference {
  id: string
  organizationId: string
  canvasBlockId: string
  entityType: ReferenceEntityType
  entityId: string
  entityName?: string
  role: ReferenceRole
  linkType: ReferenceLinkType
  contextNote?: string
  sortOrder: number
  isHighlighted: boolean
  metricsJson?: string
  tagsJson?: string
  createdAt: string
  updatedAt: string
}

// Canvas Block Types (9 BMC blocks)
export type CanvasBlockType = 'KeyPartners' | 'KeyActivities' | 'KeyResources' | 'ValuePropositions' | 'CustomerRelationships' | 'Channels' | 'CustomerSegments' | 'CostStructure' | 'RevenueStreams'

export interface CanvasBlock {
  id: string
  organizationId: string
  canvasId: string
  blockType: CanvasBlockType
  title?: string
  summaryNote?: string
  content?: string
  positionJson?: string
  aiInsightsJson?: string
  sortOrder: number
  displayOrder: number
  createdAt: string
  updatedAt: string
  references?: BlockReference[]
}

// Enhanced Canvas Types
export type CanvasType = 'BusinessModel' | 'Lean' | 'ValueProposition' | 'Custom'
export type CanvasScopeType = 'Organization' | 'Product' | 'Segment' | 'Initiative'
export type CanvasStatus = 'Draft' | 'Active' | 'Archived'

export interface Canvas {
  id: string
  organizationId: string
  name: string
  slug?: string
  description?: string
  canvasType: CanvasType
  scopeType: CanvasScopeType
  status: CanvasStatus
  version: number
  versionNote?: string
  aiSummary?: string
  aiMetadataJson?: string
  parentCanvasId?: string
  productId?: string
  segmentId?: string
  createdAt: string
  updatedAt: string
  blocks?: CanvasBlock[]
  childCanvases?: Canvas[]
}

// ===========================================
// Process Documentation Export Types
// ===========================================

/** Drill-down level for subprocess inclusion in documentation */
export type DrillDownLevel = 0 | 1 | 2 | 3 | 'full'

/** Options for document generation */
export interface DocumentExportOptions {
  drillDownLevel: DrillDownLevel
  format: 'markdown' | 'html'
  includeInstructions: boolean
}

/** Tree node representing a process and its subprocesses for documentation */
export interface ProcessDocumentNode {
  process: ProcessWithActivities
  subprocesses: ProcessDocumentNode[]
  depth: number
}
