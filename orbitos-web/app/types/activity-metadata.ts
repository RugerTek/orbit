// Type-specific metadata interfaces for IE (Industrial Engineering) symbols
// These define the additional fields available for each activity type
// Aligned with ASME Y15.3 and lean/six sigma analysis requirements

/** Common time study fields - can be added to any activity type */
export interface TimeStudyFields {
  /** Setup/changeover time in minutes (for SMED analysis) */
  setupTimeMinutes?: number
  /** Standard time per unit in minutes (from time study) */
  standardTimeMinutes?: number
  /** Number of time observations taken (for statistical validity) */
  observationCount?: number
  /** Standard deviation of cycle time (for variability analysis) */
  cycleTimeStdDev?: number
  /** Personal, Fatigue, Delay allowance percentage */
  pfdAllowance?: number
}

/** Common cost fields */
export interface CostFields {
  /** Labor cost per occurrence */
  laborCost?: number
  /** Material cost per occurrence */
  materialCost?: number
  /** Equipment/overhead cost per occurrence */
  overheadCost?: number
  /** Currency code (USD, EUR, etc.) */
  currency?: string
}

/** Operation symbol - primary value-adding activity (ASME standard) */
export interface OperationMetadata extends TimeStudyFields, CostFields {
  /** Type of operation */
  operationType?: 'fabrication' | 'assembly' | 'processing' | 'packaging' | 'service' | 'other'
  /** Machine or workstation used */
  workstation?: string
  /** Batch size if applicable */
  batchSize?: number
  /** Transfer batch size (may differ from process batch) */
  transferBatchSize?: number
  /** Pieces per hour (for capacity planning) */
  piecesPerHour?: number
}

/** Inspection symbol - quality check/verification */
export interface InspectionMetadata extends TimeStudyFields, CostFields {
  /** What criteria are being checked (e.g., "Visual + dimensional check") */
  inspectionCriteria?: string
  /** Pass rate percentage (0-100) - first pass yield */
  passRate?: number
  /** Types of defects being checked for */
  defectTypes?: string[]
  /** Sampling rate if not 100% inspection */
  samplingRate?: number
  /** Equipment used for inspection */
  inspectionEquipment?: string
  /** Defects per unit (DPU) */
  defectsPerUnit?: number
  /** Defects per million opportunities (DPMO) for Six Sigma */
  dpmo?: number
  /** Process capability index (Cpk) for continuous measurements */
  cpk?: number
  /** What happens to failed items */
  failureDisposition?: 'rework' | 'scrap' | 'downgrade' | 'review' | 'other'
  /** Rework percentage of failures */
  reworkPercent?: number
  /** Scrap percentage of failures */
  scrapPercent?: number
}

/** Transport symbol - movement of materials/information (non-value-adding) */
export interface TransportMetadata extends TimeStudyFields, CostFields {
  /** Distance traveled */
  distance?: number
  /** Unit of distance (meters, feet, etc.) */
  distanceUnit?: 'meters' | 'feet' | 'kilometers' | 'miles'
  /** Mode of transport */
  transportMode?: 'manual' | 'forklift' | 'conveyor' | 'cart' | 'truck' | 'agv' | 'other'
  /** Starting location/station */
  origin?: string
  /** Ending location/station */
  destination?: string
  /** Frequency of transport if batch */
  frequency?: string
  /** Number of trips per day/shift */
  tripsPerShift?: number
  /** Load/unload time in minutes */
  loadUnloadMinutes?: number
}

/** Delay symbol - wait/queue time (non-value-adding unless technically required) */
export interface DelayMetadata extends CostFields {
  /** Reason for the delay */
  delayReason?: 'queue' | 'batch' | 'approval' | 'curing' | 'drying' | 'cooling' | 'scheduling' | 'other'
  /** Average wait time in minutes */
  averageWaitMinutes?: number
  /** Maximum observed wait time */
  maxWaitMinutes?: number
  /** Minimum wait time (if there's a required wait) */
  minWaitMinutes?: number
  /** Standard deviation of wait time (for variability analysis) */
  waitTimeStdDev?: number
  /** Custom reason description if 'other' */
  customReason?: string
  /** Is this a technically necessary delay (curing, cooling) vs. pure waste (queue)? */
  isTechnicallyRequired?: boolean
  /** Average queue length (WIP) at this point */
  averageQueueLength?: number
}

/** Storage symbol - controlled storage (ties up capital) */
export interface StorageMetadata extends CostFields {
  /** Type of inventory being stored */
  storageType?: 'raw' | 'wip' | 'finished' | 'tools' | 'consumables' | 'other'
  /** Storage capacity */
  capacity?: number
  /** Unit of capacity (units, pallets, etc.) */
  capacityUnit?: string
  /** Current inventory level */
  currentLevel?: number
  /** Physical location identifier */
  location?: string
  /** Storage conditions (temperature, humidity, etc.) */
  storageConditions?: string
  /** FIFO/LIFO/other method */
  inventoryMethod?: 'fifo' | 'lifo' | 'fefo' | 'other'
  /** Average days of inventory held */
  averageDaysOfInventory?: number
  /** Inventory carrying cost percentage per year */
  carryingCostPercent?: number
  /** Safety stock level */
  safetyStockLevel?: number
  /** Reorder point */
  reorderPoint?: number
}

/** Document symbol - paper/digital document */
export interface DocumentMetadata extends TimeStudyFields, CostFields {
  /** Type of document */
  documentType?: 'form' | 'report' | 'checklist' | 'certificate' | 'drawing' | 'specification' | 'other'
  /** URL to document template */
  templateUrl?: string
  /** Required fields in the document */
  requiredFields?: string[]
  /** Is this a controlled document */
  isControlled?: boolean
  /** Document number/code */
  documentCode?: string
  /** Retention period */
  retentionPeriod?: string
  /** Error rate in document completion */
  errorRate?: number
}

/** Database symbol - data store interaction */
export interface DatabaseMetadata extends TimeStudyFields, CostFields {
  /** Name of the system/database */
  systemName?: string
  /** Type of operation */
  operation?: 'read' | 'write' | 'both'
  /** Tables or entities involved */
  tables?: string[]
  /** Is this a transaction? */
  isTransaction?: boolean
  /** Connection/integration type */
  integrationType?: 'direct' | 'api' | 'file' | 'message'
  /** Average response time in milliseconds */
  avgResponseTimeMs?: number
  /** Error/failure rate percentage */
  failureRate?: number
}

/** Manual Input symbol - user data entry */
export interface ManualInputMetadata extends TimeStudyFields, CostFields {
  /** Fields that need to be entered */
  inputFields?: string[]
  /** Validation rules description */
  validationRules?: string
  /** Input device used */
  inputDevice?: 'keyboard' | 'scanner' | 'touchscreen' | 'voice' | 'other'
  /** Expected input time in seconds */
  expectedInputTime?: number
  /** Error rate if known */
  errorRate?: number
  /** Keystrokes per entry (for productivity metrics) */
  keystrokesPerEntry?: number
}

/** Display symbol - screen output */
export interface DisplayMetadata extends CostFields {
  /** Type of display */
  displayType?: 'dashboard' | 'report' | 'alert' | 'status' | 'instruction' | 'other'
  /** How often display updates (seconds) */
  refreshRateSeconds?: number
  /** Display device */
  displayDevice?: 'monitor' | 'andon' | 'mobile' | 'projector' | 'other'
  /** Location of display */
  displayLocation?: string
  /** Average viewing time in seconds */
  viewingTimeSeconds?: number
}

/** Automated/Machine activity - for OEE tracking */
export interface AutomatedMetadata extends TimeStudyFields, CostFields {
  /** Machine or equipment name */
  machineName?: string
  /** Availability rate (uptime %) for OEE */
  availabilityRate?: number
  /** Performance rate (actual vs. ideal speed %) for OEE */
  performanceRate?: number
  /** Quality rate (good units %) for OEE */
  qualityRate?: number
  /** Calculated OEE (Availability x Performance x Quality) */
  oee?: number
  /** Mean time between failures (hours) */
  mtbf?: number
  /** Mean time to repair (hours) */
  mttr?: number
  /** Planned maintenance schedule */
  maintenanceSchedule?: string
}

/** Decision symbol - already built-in, but can have metadata too */
export interface DecisionMetadata {
  /** The question being asked */
  decisionQuestion?: string
  /** Criteria for yes branch */
  yesCriteria?: string
  /** Criteria for no branch */
  noCriteria?: string
  /** Historical yes percentage */
  yesPercentage?: number
}

/** Union type for all metadata types */
export type ActivityMetadata =
  | OperationMetadata
  | InspectionMetadata
  | TransportMetadata
  | DelayMetadata
  | StorageMetadata
  | DocumentMetadata
  | DatabaseMetadata
  | ManualInputMetadata
  | DisplayMetadata
  | DecisionMetadata
  | AutomatedMetadata
  | (TimeStudyFields & CostFields) // For manual/hybrid/handoff

/** Maps activity type to its metadata interface */
export type MetadataByActivityType = {
  operation: OperationMetadata
  inspection: InspectionMetadata
  transport: TransportMetadata
  delay: DelayMetadata
  storage: StorageMetadata
  document: DocumentMetadata
  database: DatabaseMetadata
  manualInput: ManualInputMetadata
  display: DisplayMetadata
  decision: DecisionMetadata
  manual: TimeStudyFields & CostFields
  automated: AutomatedMetadata
  hybrid: TimeStudyFields & CostFields
  handoff: TimeStudyFields & CostFields
}

/** Helper to get default metadata for an activity type */
export function getDefaultMetadata(activityType: string): ActivityMetadata {
  switch (activityType) {
    case 'operation':
      return { operationType: 'processing', workstation: '', batchSize: 1, setupTimeMinutes: undefined, standardTimeMinutes: undefined }
    case 'inspection':
      return { inspectionCriteria: '', passRate: undefined, defectTypes: [], failureDisposition: 'rework', setupTimeMinutes: undefined }
    case 'transport':
      return { distance: undefined, distanceUnit: 'meters', transportMode: 'manual', origin: '', destination: '' }
    case 'delay':
      return { delayReason: 'queue', averageWaitMinutes: undefined, isTechnicallyRequired: false }
    case 'storage':
      return { storageType: 'wip', capacity: undefined, location: '', inventoryMethod: 'fifo' }
    case 'document':
      return { documentType: 'form', requiredFields: [], setupTimeMinutes: undefined }
    case 'database':
      return { systemName: '', operation: 'read', tables: [] }
    case 'manualInput':
      return { inputFields: [], inputDevice: 'keyboard', setupTimeMinutes: undefined }
    case 'display':
      return { displayType: 'status', displayDevice: 'monitor' }
    case 'decision':
      return { decisionQuestion: '' }
    case 'automated':
      return { machineName: '', availabilityRate: undefined, performanceRate: undefined, qualityRate: undefined, setupTimeMinutes: undefined }
    case 'manual':
    case 'hybrid':
    case 'handoff':
      return { setupTimeMinutes: undefined, standardTimeMinutes: undefined }
    default:
      return {}
  }
}

/** Parse metadata JSON string to typed object */
export function parseMetadata<T extends ActivityMetadata>(json: string | null | undefined): T | null {
  if (!json) return null
  try {
    return JSON.parse(json) as T
  } catch {
    return null
  }
}

/** Stringify metadata object to JSON */
export function stringifyMetadata(metadata: ActivityMetadata | null | undefined): string | undefined {
  if (!metadata || Object.keys(metadata).length === 0) return undefined
  return JSON.stringify(metadata)
}
