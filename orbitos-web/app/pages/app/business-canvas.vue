<script setup lang="ts">
import type {
  Partner,
  Channel,
  ValueProposition,
  CustomerRelationship,
  RevenueStream,
  Canvas,
  BlockReference,
  EnrichedBlockReference,
  ReferenceEntityType,
  CanvasBlockType,
} from '~/types/operations'

definePageMeta({
  layout: 'app',
})

const {
  isLoading,
  partners,
  channels,
  valuePropositions,
  customerRelationships,
  revenueStreams,
  roles,
  processes,
  fetchPartners,
  fetchChannels,
  fetchValuePropositions,
  fetchCustomerRelationships,
  fetchRevenueStreams,
  fetchRoles,
  fetchProcesses,
  // Canvas methods
  fetchBmcCanvases,
  createBmcCanvas,
  // Block reference methods
  addBlockReference,
  removeBlockReference,
  getCanvasEnriched,
} = useOperations()

// Current canvas state
const currentCanvas = ref<Canvas | null>(null)
const canvasLoading = ref(true)

// View mode state
type ViewMode = 'canvas' | 'kanban' | 'list'
const viewMode = ref<ViewMode>('canvas')

// Active block for expanded side panel
type BlockType = 'partners' | 'activities' | 'resources' | 'propositions' | 'relationships' | 'channels' | 'segments' | 'costs' | 'revenue'
const activeBlock = ref<BlockType | null>(null)

// Block references state - cached per block type
const blockReferences = ref<Record<string, BlockReference[]>>({})

// Entity modal state
const showEntityModal = ref(false)
const editingEntity = ref<Partner | Channel | ValueProposition | CustomerRelationship | RevenueStream | null>(null)
const entityType = ref<'partner' | 'channel' | 'valueProposition' | 'customerRelationship' | 'revenueStream'>('partner')

// Link entity dialog state
const showLinkDialog = ref(false)
const linkDialogBlockType = ref<BlockType | null>(null)

// Expanded role state (for showing people under roles)
const expandedRoleId = ref<string | null>(null)

// Block definitions with color schemes and entity type mapping
const blockDefinitions: Record<BlockType, {
  title: string
  color: string
  bgColor: string
  borderColor: string
  entityType: string
  apiBlockType: CanvasBlockType
  referenceEntityType?: ReferenceEntityType
}> = {
  partners: { title: 'Key Partners', color: 'blue', bgColor: 'bg-blue-500/10', borderColor: 'border-blue-500/30', entityType: 'partner', apiBlockType: 'KeyPartners', referenceEntityType: 'Partner' },
  activities: { title: 'Key Activities', color: 'purple', bgColor: 'bg-purple-500/10', borderColor: 'border-purple-500/30', entityType: 'process', apiBlockType: 'KeyActivities', referenceEntityType: 'Process' },
  resources: { title: 'Key Resources', color: 'emerald', bgColor: 'bg-emerald-500/10', borderColor: 'border-emerald-500/30', entityType: 'role', apiBlockType: 'KeyResources', referenceEntityType: 'Role' },
  propositions: { title: 'Value Propositions', color: 'amber', bgColor: 'bg-amber-500/10', borderColor: 'border-amber-500/30', entityType: 'valueProposition', apiBlockType: 'ValuePropositions', referenceEntityType: 'ValueProposition' },
  relationships: { title: 'Customer Relationships', color: 'cyan', bgColor: 'bg-cyan-500/10', borderColor: 'border-cyan-500/30', entityType: 'customerRelationship', apiBlockType: 'CustomerRelationships', referenceEntityType: 'CustomerRelationship' },
  channels: { title: 'Channels', color: 'pink', bgColor: 'bg-pink-500/10', borderColor: 'border-pink-500/30', entityType: 'channel', apiBlockType: 'Channels', referenceEntityType: 'Channel' },
  segments: { title: 'Customer Segments', color: 'orange', bgColor: 'bg-orange-500/10', borderColor: 'border-orange-500/30', entityType: 'segment', apiBlockType: 'CustomerSegments' },
  costs: { title: 'Cost Structure', color: 'red', bgColor: 'bg-red-500/10', borderColor: 'border-red-500/30', entityType: 'cost', apiBlockType: 'CostStructure' },
  revenue: { title: 'Revenue Streams', color: 'teal', bgColor: 'bg-teal-500/10', borderColor: 'border-teal-500/30', entityType: 'revenueStream', apiBlockType: 'RevenueStreams', referenceEntityType: 'RevenueStream' },
}

// Load or create default canvas, then load all data
onMounted(async () => {
  canvasLoading.value = true

  try {
    // First fetch the BMC canvases for this organization
    const canvases = await fetchBmcCanvases()

    if (canvases && canvases.length > 0) {
      // Use the first BMC canvas (could add canvas selection later)
      const firstCanvas = canvases[0]!
      const enrichedCanvas = await getCanvasEnriched(firstCanvas.id)
      currentCanvas.value = enrichedCanvas || firstCanvas

      // Extract block references from the canvas blocks
      if (currentCanvas.value?.blocks) {
        for (const block of currentCanvas.value.blocks) {
          const blockKey = getBlockKeyFromApiType(block.blockType)
          if (blockKey && block.references) {
            blockReferences.value[blockKey] = block.references
          }
        }
      }
    } else {
      // Create a default BMC canvas for this organization
      const newCanvas = await createBmcCanvas({
        name: 'Organization Business Model',
        description: 'Default Business Model Canvas for the organization',
        scopeType: 'Organization',
        status: 'Active',
      })
      currentCanvas.value = newCanvas
    }
  } catch (e) {
    console.error('Failed to load canvas:', e)
  }

  // Load all supporting data in parallel
  await Promise.all([
    fetchPartners(),
    fetchChannels(),
    fetchValuePropositions(),
    fetchCustomerRelationships(),
    fetchRevenueStreams(),
    fetchRoles(),
    fetchProcesses(),
  ])

  canvasLoading.value = false
})

// Helper to convert API block type to our BlockType key
const getBlockKeyFromApiType = (apiType: string): BlockType | null => {
  const mapping: Record<string, BlockType> = {
    KeyPartners: 'partners',
    KeyActivities: 'activities',
    KeyResources: 'resources',
    ValuePropositions: 'propositions',
    CustomerRelationships: 'relationships',
    Channels: 'channels',
    CustomerSegments: 'segments',
    CostStructure: 'costs',
    RevenueStreams: 'revenue',
  }
  return mapping[apiType] || null
}

// Block item type for consistent typing
interface BlockItem {
  id: string
  name: string
  description?: string
  status?: string
  type?: string
  // For enriched references
  assignedPeople?: Array<{ resourceId: string; resourceName: string; allocationPercentage?: number; isPrimary: boolean }>
  coverageStatus?: string
  activities?: Array<{ id: string; name: string; activityType: string }>
  processStatus?: string
  referenceId?: string // The BlockReference ID for unlinking
}

// Get items for a block - combines direct entities with linked references
const getBlockItems = (blockType: BlockType): BlockItem[] => {
  const refs = blockReferences.value[blockType] || []

  // For blocks that use references (Key Activities, Key Resources)
  if (blockType === 'activities') {
    return refs.map(ref => {
      const enriched = ref as EnrichedBlockReference
      return {
        id: ref.entityId,
        name: ref.entityName || 'Unknown Process',
        description: ref.contextNote,
        status: enriched.processStatus,
        activities: enriched.activities,
        referenceId: ref.id,
      }
    })
  }

  if (blockType === 'resources') {
    return refs.map(ref => {
      const enriched = ref as EnrichedBlockReference
      return {
        id: ref.entityId,
        name: ref.entityName || 'Unknown Role',
        description: ref.contextNote,
        status: enriched.coverageStatus,
        assignedPeople: enriched.assignedPeople,
        coverageStatus: enriched.coverageStatus,
        referenceId: ref.id,
      }
    })
  }

  // For other blocks, use direct entity data
  if (blockType === 'partners') {
    return partners.value.map(p => ({ id: p.id, name: p.name, description: p.description, status: p.status, type: p.type }))
  }
  if (blockType === 'propositions') {
    return valuePropositions.value.map(v => ({ id: v.id, name: v.name, description: v.headline, status: v.status }))
  }
  if (blockType === 'relationships') {
    return customerRelationships.value.map(c => ({ id: c.id, name: c.name, description: c.description, status: c.status, type: c.type }))
  }
  if (blockType === 'channels') {
    return channels.value.map(c => ({ id: c.id, name: c.name, description: c.description, status: c.status, type: c.type }))
  }
  if (blockType === 'revenue') {
    return revenueStreams.value.map(r => ({ id: r.id, name: r.name, description: r.description, status: r.status, type: r.type }))
  }

  return []
}

// Block items - computed from API data and references
const blockItems = computed<Record<BlockType, BlockItem[]>>(() => ({
  partners: getBlockItems('partners'),
  activities: getBlockItems('activities'),
  resources: getBlockItems('resources'),
  propositions: getBlockItems('propositions'),
  relationships: getBlockItems('relationships'),
  channels: getBlockItems('channels'),
  segments: [] as BlockItem[], // Coming soon
  costs: [] as BlockItem[], // Coming soon
  revenue: getBlockItems('revenue'),
}))

// Available items to link (not already linked)
const availableToLink = computed(() => {
  if (!linkDialogBlockType.value) return []

  const blockType = linkDialogBlockType.value
  const existingRefs = blockReferences.value[blockType] || []
  const existingIds = new Set(existingRefs.map(r => r.entityId))

  if (blockType === 'activities') {
    return processes.value
      .filter(p => !existingIds.has(p.id))
      .map(p => ({ id: p.id, name: p.name, description: p.purpose || p.description }))
  }

  if (blockType === 'resources') {
    return roles.value
      .filter(r => !existingIds.has(r.id))
      .map(r => ({ id: r.id, name: r.name, description: r.purpose || r.description }))
  }

  return []
})

// Color classes helper
const colorClasses: Record<string, { bg: string; border: string; text: string; badge: string }> = {
  blue: { bg: 'bg-blue-500/10', border: 'border-blue-500/30', text: 'text-blue-300', badge: 'bg-blue-500/20 text-blue-300' },
  purple: { bg: 'bg-purple-500/10', border: 'border-purple-500/30', text: 'text-purple-300', badge: 'bg-purple-500/20 text-purple-300' },
  emerald: { bg: 'bg-emerald-500/10', border: 'border-emerald-500/30', text: 'text-emerald-300', badge: 'bg-emerald-500/20 text-emerald-300' },
  amber: { bg: 'bg-amber-500/10', border: 'border-amber-500/30', text: 'text-amber-300', badge: 'bg-amber-500/20 text-amber-300' },
  cyan: { bg: 'bg-cyan-500/10', border: 'border-cyan-500/30', text: 'text-cyan-300', badge: 'bg-cyan-500/20 text-cyan-300' },
  pink: { bg: 'bg-pink-500/10', border: 'border-pink-500/30', text: 'text-pink-300', badge: 'bg-pink-500/20 text-pink-300' },
  orange: { bg: 'bg-orange-500/10', border: 'border-orange-500/30', text: 'text-orange-300', badge: 'bg-orange-500/20 text-orange-300' },
  red: { bg: 'bg-red-500/10', border: 'border-red-500/30', text: 'text-red-300', badge: 'bg-red-500/20 text-red-300' },
  teal: { bg: 'bg-teal-500/10', border: 'border-teal-500/30', text: 'text-teal-300', badge: 'bg-teal-500/20 text-teal-300' },
}

const defaultColorClass = { bg: 'bg-blue-500/10', border: 'border-blue-500/30', text: 'text-blue-300', badge: 'bg-blue-500/20 text-blue-300' }

const getColorClasses = (color: string) => {
  return colorClasses[color] ?? defaultColorClass
}

// Handle block click to expand side panel
const handleBlockClick = (block: BlockType) => {
  if (activeBlock.value === block) {
    activeBlock.value = null
  } else {
    activeBlock.value = block
  }
}

// Handle item click to open edit modal or navigate
const handleItemClick = (block: BlockType, item: BlockItem) => {
  const def = blockDefinitions[block]

  // For linked entities (activities, resources), navigate to their detail pages
  if (def.entityType === 'process') {
    navigateTo(`/app/processes/${item.id}`)
    return
  }
  if (def.entityType === 'role') {
    navigateTo(`/app/roles/${item.id}`)
    return
  }

  // For direct entities, open edit modal
  if (def.entityType === 'partner') {
    editingEntity.value = partners.value.find(p => p.id === item.id) || null
    entityType.value = 'partner'
  } else if (def.entityType === 'channel') {
    editingEntity.value = channels.value.find(c => c.id === item.id) || null
    entityType.value = 'channel'
  } else if (def.entityType === 'valueProposition') {
    editingEntity.value = valuePropositions.value.find(v => v.id === item.id) || null
    entityType.value = 'valueProposition'
  } else if (def.entityType === 'customerRelationship') {
    editingEntity.value = customerRelationships.value.find(c => c.id === item.id) || null
    entityType.value = 'customerRelationship'
  } else if (def.entityType === 'revenueStream') {
    editingEntity.value = revenueStreams.value.find(r => r.id === item.id) || null
    entityType.value = 'revenueStream'
  }

  if (editingEntity.value) {
    showEntityModal.value = true
  }
}

// Handle add new item - either open link dialog or create dialog
const handleAddItem = (block: BlockType) => {
  const def = blockDefinitions[block]

  // For blocks that use linking (activities, resources), open link dialog
  if (def.entityType === 'process' || def.entityType === 'role') {
    linkDialogBlockType.value = block
    showLinkDialog.value = true
    return
  }

  // For other blocks, open create modal
  editingEntity.value = null
  if (def.entityType === 'partner') entityType.value = 'partner'
  else if (def.entityType === 'channel') entityType.value = 'channel'
  else if (def.entityType === 'valueProposition') entityType.value = 'valueProposition'
  else if (def.entityType === 'customerRelationship') entityType.value = 'customerRelationship'
  else if (def.entityType === 'revenueStream') entityType.value = 'revenueStream'
  else return // Not a supported entity type for add
  showEntityModal.value = true
}

// Link an entity to a block
const handleLinkEntity = async (entityId: string) => {
  if (!currentCanvas.value || !linkDialogBlockType.value) return

  const def = blockDefinitions[linkDialogBlockType.value]
  if (!def.referenceEntityType) return

  try {
    const blockKey = linkDialogBlockType.value
    const newRef = await addBlockReference(
      currentCanvas.value.id,
      def.apiBlockType as CanvasBlockType,
      {
        entityType: def.referenceEntityType,
        entityId,
        role: 'Primary',
      }
    )

    // Add to local state
    if (!blockReferences.value[blockKey]) {
      blockReferences.value[blockKey] = []
    }
    blockReferences.value[blockKey]!.push(newRef)

    showLinkDialog.value = false
    linkDialogBlockType.value = null
  } catch (e) {
    console.error('Failed to link entity:', e)
  }
}

// Unlink an entity from a block
const handleUnlinkEntity = async (block: BlockType, item: BlockItem) => {
  if (!currentCanvas.value || !item.referenceId) return

  const def = blockDefinitions[block]

  try {
    await removeBlockReference(currentCanvas.value.id, def.apiBlockType as CanvasBlockType, item.referenceId)

    // Remove from local state
    blockReferences.value[block] = (blockReferences.value[block] || []).filter(r => r.id !== item.referenceId)
  } catch (e) {
    console.error('Failed to unlink entity:', e)
  }
}

// Toggle role expansion to show assigned people
const toggleRoleExpansion = (roleId: string) => {
  expandedRoleId.value = expandedRoleId.value === roleId ? null : roleId
}

// Stats computed
const stats = computed(() => ({
  partners: partners.value.length,
  channels: channels.value.length,
  propositions: valuePropositions.value.length,
  relationships: customerRelationships.value.length,
  revenue: revenueStreams.value.length,
  activities: blockItems.value.activities.length,
  resources: blockItems.value.resources.length,
  total: partners.value.length + channels.value.length + valuePropositions.value.length + customerRelationships.value.length + revenueStreams.value.length,
}))

// Kanban columns (by status)
const kanbanStatuses = ['Active', 'Planned', 'Optimizing', 'Sunset'] as const
type KanbanStatus = typeof kanbanStatuses[number]

const kanbanItems = computed(() => {
  const items: Record<KanbanStatus, Array<{ id: string; name: string; type: string; block: BlockType; status: string }>> = {
    Active: [],
    Planned: [],
    Optimizing: [],
    Sunset: [],
  }

  // Map status strings to Kanban columns
  const mapStatus = (status: string | undefined): KanbanStatus => {
    if (!status) return 'Planned'
    const s = status.toLowerCase()
    if (s === 'active' || s === 'validated' || s === 'covered') return 'Active'
    if (s === 'planned' || s === 'draft' || s === 'prospective' || s === 'testing' || s === 'uncovered') return 'Planned'
    if (s === 'optimizing' || s === 'growing' || s === 'mature' || s === 'atrisk') return 'Optimizing'
    if (s === 'sunset' || s === 'declining' || s === 'retired' || s === 'terminated' || s === 'inactive') return 'Sunset'
    return 'Active'
  }

  partners.value.forEach(p => items[mapStatus(p.status)].push({ id: p.id, name: p.name, type: 'Partner', block: 'partners', status: p.status }))
  channels.value.forEach(c => items[mapStatus(c.status)].push({ id: c.id, name: c.name, type: 'Channel', block: 'channels', status: c.status }))
  valuePropositions.value.forEach(v => items[mapStatus(v.status)].push({ id: v.id, name: v.name, type: 'Value Prop', block: 'propositions', status: v.status }))
  customerRelationships.value.forEach(c => items[mapStatus(c.status)].push({ id: c.id, name: c.name, type: 'Relationship', block: 'relationships', status: c.status }))
  revenueStreams.value.forEach(r => items[mapStatus(r.status)].push({ id: r.id, name: r.name, type: 'Revenue', block: 'revenue', status: r.status }))
  // Add linked activities and resources
  blockItems.value.activities.forEach(a => items[mapStatus(a.status)].push({ id: a.id, name: a.name, type: 'Process', block: 'activities', status: a.status || 'Active' }))
  blockItems.value.resources.forEach(r => items[mapStatus(r.coverageStatus)].push({ id: r.id, name: r.name, type: 'Role', block: 'resources', status: r.coverageStatus || 'Active' }))

  return items
})

// List view - all items flattened
const listItems = computed(() => {
  const items: Array<{ id: string; name: string; type: string; block: BlockType; status: string; description?: string }> = []

  partners.value.forEach(p => items.push({ id: p.id, name: p.name, type: 'Partner', block: 'partners', status: p.status, description: p.description }))
  channels.value.forEach(c => items.push({ id: c.id, name: c.name, type: 'Channel', block: 'channels', status: c.status, description: c.description }))
  valuePropositions.value.forEach(v => items.push({ id: v.id, name: v.name, type: 'Value Proposition', block: 'propositions', status: v.status, description: v.headline }))
  customerRelationships.value.forEach(c => items.push({ id: c.id, name: c.name, type: 'Customer Relationship', block: 'relationships', status: c.status, description: c.description }))
  revenueStreams.value.forEach(r => items.push({ id: r.id, name: r.name, type: 'Revenue Stream', block: 'revenue', status: r.status, description: r.description }))
  // Add linked items
  blockItems.value.activities.forEach(a => items.push({ id: a.id, name: a.name, type: 'Process', block: 'activities', status: a.status || 'Active', description: a.description }))
  blockItems.value.resources.forEach(r => items.push({ id: r.id, name: r.name, type: 'Role', block: 'resources', status: r.coverageStatus || 'Active', description: r.description }))

  return items
})

// List view sorting
const sortField = ref<'name' | 'type' | 'status'>('name')
const sortDirection = ref<'asc' | 'desc'>('asc')

const sortedListItems = computed(() => {
  return [...listItems.value].sort((a, b) => {
    const aVal = a[sortField.value] || ''
    const bVal = b[sortField.value] || ''
    const cmp = aVal.localeCompare(bVal)
    return sortDirection.value === 'asc' ? cmp : -cmp
  })
})

const toggleSort = (field: 'name' | 'type' | 'status') => {
  if (sortField.value === field) {
    sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortField.value = field
    sortDirection.value = 'asc'
  }
}

// Search/filter
const searchQuery = ref('')
const filteredListItems = computed(() => {
  if (!searchQuery.value) return sortedListItems.value
  const q = searchQuery.value.toLowerCase()
  return sortedListItems.value.filter(item =>
    item.name.toLowerCase().includes(q) ||
    item.type.toLowerCase().includes(q) ||
    (item.description && item.description.toLowerCase().includes(q)),
  )
})

// Status color helper
const getStatusColor = (status: string | undefined) => {
  if (!status) return 'bg-slate-500/20 text-slate-300'
  const s = status.toLowerCase()
  if (s === 'active' || s === 'validated' || s === 'covered') return 'bg-emerald-500/20 text-emerald-300'
  if (s === 'planned' || s === 'draft' || s === 'prospective' || s === 'testing' || s === 'uncovered') return 'bg-blue-500/20 text-blue-300'
  if (s === 'optimizing' || s === 'growing' || s === 'mature' || s === 'atrisk') return 'bg-purple-500/20 text-purple-300'
  if (s === 'sunset' || s === 'declining' || s === 'retired' || s === 'terminated' || s === 'inactive') return 'bg-amber-500/20 text-amber-300'
  return 'bg-slate-500/20 text-slate-300'
}

// Coverage status color helper
const getCoverageColor = (status: string | undefined) => {
  if (!status) return 'bg-slate-500/20 text-slate-300'
  const s = status.toLowerCase()
  if (s === 'covered') return 'bg-emerald-500/20 text-emerald-300'
  if (s === 'atrisk') return 'bg-amber-500/20 text-amber-300'
  if (s === 'uncovered') return 'bg-red-500/20 text-red-300'
  return 'bg-slate-500/20 text-slate-300'
}
</script>

<template>
  <div class="flex h-[calc(100vh-8rem)] flex-col">
    <!-- Header -->
    <div class="mb-4 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="orbitos-heading-lg">Business Model Canvas</h1>
        <p class="orbitos-text">
          {{ currentCanvas?.name || 'Map your business model with the 9 building blocks' }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <!-- View Mode Toggle -->
        <div class="flex rounded-xl bg-white/5 border border-white/10 p-1">
          <button
            v-for="mode in [{ id: 'canvas', label: 'Canvas', icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z' }, { id: 'kanban', label: 'Kanban', icon: 'M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2' }, { id: 'list', label: 'List', icon: 'M4 6h16M4 10h16M4 14h16M4 18h16' }] as const"
            :key="mode.id"
            :class="[
              'flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-all duration-200',
              viewMode === mode.id
                ? 'bg-purple-500/20 border border-purple-500/30 text-purple-300'
                : 'text-white/60 hover:text-white hover:bg-white/5'
            ]"
            @click="viewMode = mode.id"
          >
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="mode.icon" />
            </svg>
            <span class="hidden sm:inline">{{ mode.label }}</span>
          </button>
        </div>

        <!-- Export Button -->
        <button class="orbitos-btn-secondary px-4 py-2 text-sm">
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
          </svg>
          Export
        </button>
      </div>
    </div>

    <!-- Stats Bar -->
    <div class="mb-4 flex flex-wrap gap-3">
      <div class="flex items-center gap-2 rounded-lg bg-white/5 border border-white/10 px-3 py-2">
        <span class="text-xs text-white/40">Partners</span>
        <span class="text-sm font-semibold text-blue-300">{{ stats.partners }}</span>
      </div>
      <div class="flex items-center gap-2 rounded-lg bg-white/5 border border-white/10 px-3 py-2">
        <span class="text-xs text-white/40">Activities</span>
        <span class="text-sm font-semibold text-purple-300">{{ stats.activities }}</span>
      </div>
      <div class="flex items-center gap-2 rounded-lg bg-white/5 border border-white/10 px-3 py-2">
        <span class="text-xs text-white/40">Resources</span>
        <span class="text-sm font-semibold text-emerald-300">{{ stats.resources }}</span>
      </div>
      <div class="flex items-center gap-2 rounded-lg bg-white/5 border border-white/10 px-3 py-2">
        <span class="text-xs text-white/40">Channels</span>
        <span class="text-sm font-semibold text-pink-300">{{ stats.channels }}</span>
      </div>
      <div class="flex items-center gap-2 rounded-lg bg-white/5 border border-white/10 px-3 py-2">
        <span class="text-xs text-white/40">Value Props</span>
        <span class="text-sm font-semibold text-amber-300">{{ stats.propositions }}</span>
      </div>
      <div class="flex items-center gap-2 rounded-lg bg-white/5 border border-white/10 px-3 py-2">
        <span class="text-xs text-white/40">Revenue Streams</span>
        <span class="text-sm font-semibold text-teal-300">{{ stats.revenue }}</span>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading || canvasLoading" class="flex flex-1 items-center justify-center">
      <div class="orbitos-spinner orbitos-spinner-lg"></div>
    </div>

    <!-- Canvas View -->
    <div v-else-if="viewMode === 'canvas'" class="flex flex-1 gap-4 overflow-hidden">
      <!-- Main Canvas Grid -->
      <div
        :class="[
          'flex-1 overflow-auto rounded-2xl border border-white/10 bg-white/5 p-4 transition-all duration-300',
          activeBlock ? 'lg:w-2/3' : ''
        ]"
      >
        <div class="grid h-full min-h-[600px] gap-3" style="grid-template-columns: 1fr 1fr 2fr 1fr 1fr; grid-template-rows: 1fr 1fr 1fr;">
          <!-- Key Partners -->
          <div
            class="row-span-2 rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.partners.borderColor, activeBlock === 'partners' ? 'ring-2 ring-blue-500/50' : '']"
            @click="handleBlockClick('partners')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.partners.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('blue').badge]">{{ blockItems.partners.length }}</span>
            </div>
            <div class="space-y-2 max-h-[calc(100%-2rem)] overflow-y-auto">
              <div
                v-for="item in blockItems.partners.slice(0, 5)"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs cursor-pointer transition-all hover:scale-[1.02]', getColorClasses('blue').bg, getColorClasses('blue').border, getColorClasses('blue').text]"
                @click.stop="handleItemClick('partners', item)"
              >
                {{ item.name }}
              </div>
              <div v-if="blockItems.partners.length > 5" class="text-xs text-white/40 text-center">
                +{{ blockItems.partners.length - 5 }} more
              </div>
              <button
                class="w-full rounded-lg border border-dashed border-white/20 p-2 text-xs text-white/40 hover:border-white/40 hover:text-white/60 transition-colors"
                @click.stop="handleAddItem('partners')"
              >
                + Add Partner
              </button>
            </div>
          </div>

          <!-- Key Activities (Processes) -->
          <div
            class="rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.activities.borderColor, activeBlock === 'activities' ? 'ring-2 ring-purple-500/50' : '']"
            @click="handleBlockClick('activities')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.activities.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('purple').badge]">{{ blockItems.activities.length }}</span>
            </div>
            <div class="space-y-2">
              <div
                v-for="item in blockItems.activities.slice(0, 3)"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs cursor-pointer transition-all hover:scale-[1.02]', getColorClasses('purple').bg, getColorClasses('purple').border, getColorClasses('purple').text]"
                @click.stop="handleItemClick('activities', item)"
              >
                <div class="flex items-center justify-between">
                  <span>{{ item.name }}</span>
                  <span v-if="item.activities" class="text-[10px] opacity-60">{{ item.activities.length }} steps</span>
                </div>
              </div>
              <div v-if="blockItems.activities.length === 0" class="text-xs text-white/30 text-center italic py-2">
                Link processes here
              </div>
              <button
                class="w-full rounded-lg border border-dashed border-white/20 p-2 text-xs text-white/40 hover:border-white/40 hover:text-white/60 transition-colors"
                @click.stop="handleAddItem('activities')"
              >
                + Link Process
              </button>
            </div>
          </div>

          <!-- Value Propositions -->
          <div
            class="row-span-2 rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.propositions.borderColor, activeBlock === 'propositions' ? 'ring-2 ring-amber-500/50' : '']"
            @click="handleBlockClick('propositions')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.propositions.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('amber').badge]">{{ blockItems.propositions.length }}</span>
            </div>
            <div class="space-y-2 max-h-[calc(100%-2rem)] overflow-y-auto">
              <div
                v-for="item in blockItems.propositions.slice(0, 5)"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs cursor-pointer transition-all hover:scale-[1.02]', getColorClasses('amber').bg, getColorClasses('amber').border, getColorClasses('amber').text]"
                @click.stop="handleItemClick('propositions', item)"
              >
                <div class="font-medium">{{ item.name }}</div>
                <div v-if="item.description" class="text-[10px] opacity-70 mt-1 line-clamp-2">{{ item.description }}</div>
              </div>
              <button
                class="w-full rounded-lg border border-dashed border-white/20 p-2 text-xs text-white/40 hover:border-white/40 hover:text-white/60 transition-colors"
                @click.stop="handleAddItem('propositions')"
              >
                + Add Value Proposition
              </button>
            </div>
          </div>

          <!-- Customer Relationships -->
          <div
            class="rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.relationships.borderColor, activeBlock === 'relationships' ? 'ring-2 ring-cyan-500/50' : '']"
            @click="handleBlockClick('relationships')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.relationships.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('cyan').badge]">{{ blockItems.relationships.length }}</span>
            </div>
            <div class="space-y-2">
              <div
                v-for="item in blockItems.relationships.slice(0, 3)"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs cursor-pointer transition-all hover:scale-[1.02]', getColorClasses('cyan').bg, getColorClasses('cyan').border, getColorClasses('cyan').text]"
                @click.stop="handleItemClick('relationships', item)"
              >
                {{ item.name }}
              </div>
              <button
                class="w-full rounded-lg border border-dashed border-white/20 p-2 text-xs text-white/40 hover:border-white/40 hover:text-white/60 transition-colors"
                @click.stop="handleAddItem('relationships')"
              >
                + Add
              </button>
            </div>
          </div>

          <!-- Customer Segments -->
          <div
            class="row-span-2 rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.segments.borderColor, activeBlock === 'segments' ? 'ring-2 ring-orange-500/50' : '']"
            @click="handleBlockClick('segments')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.segments.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('orange').badge]">{{ blockItems.segments.length }}</span>
            </div>
            <div class="space-y-2">
              <div class="text-xs text-white/30 text-center italic">
                Coming soon
              </div>
            </div>
          </div>

          <!-- Key Resources (Roles) -->
          <div
            class="rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.resources.borderColor, activeBlock === 'resources' ? 'ring-2 ring-emerald-500/50' : '']"
            @click="handleBlockClick('resources')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.resources.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('emerald').badge]">{{ blockItems.resources.length }}</span>
            </div>
            <div class="space-y-2">
              <div
                v-for="item in blockItems.resources.slice(0, 3)"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs cursor-pointer transition-all hover:scale-[1.02]', getColorClasses('emerald').bg, getColorClasses('emerald').border, getColorClasses('emerald').text]"
                @click.stop="handleItemClick('resources', item)"
              >
                <div class="flex items-center justify-between">
                  <span>{{ item.name }}</span>
                  <span v-if="item.coverageStatus" :class="['rounded px-1 py-0.5 text-[10px]', getCoverageColor(item.coverageStatus)]">
                    {{ item.coverageStatus }}
                  </span>
                </div>
                <div v-if="item.assignedPeople && item.assignedPeople.length > 0" class="text-[10px] opacity-60 mt-1">
                  {{ item.assignedPeople.length }} people assigned
                </div>
              </div>
              <div v-if="blockItems.resources.length === 0" class="text-xs text-white/30 text-center italic py-2">
                Link roles here
              </div>
              <button
                class="w-full rounded-lg border border-dashed border-white/20 p-2 text-xs text-white/40 hover:border-white/40 hover:text-white/60 transition-colors"
                @click.stop="handleAddItem('resources')"
              >
                + Link Role
              </button>
            </div>
          </div>

          <!-- Channels -->
          <div
            class="rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.channels.borderColor, activeBlock === 'channels' ? 'ring-2 ring-pink-500/50' : '']"
            @click="handleBlockClick('channels')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.channels.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('pink').badge]">{{ blockItems.channels.length }}</span>
            </div>
            <div class="space-y-2">
              <div
                v-for="item in blockItems.channels.slice(0, 3)"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs cursor-pointer transition-all hover:scale-[1.02]', getColorClasses('pink').bg, getColorClasses('pink').border, getColorClasses('pink').text]"
                @click.stop="handleItemClick('channels', item)"
              >
                {{ item.name }}
              </div>
              <button
                class="w-full rounded-lg border border-dashed border-white/20 p-2 text-xs text-white/40 hover:border-white/40 hover:text-white/60 transition-colors"
                @click.stop="handleAddItem('channels')"
              >
                + Add
              </button>
            </div>
          </div>

          <!-- Cost Structure -->
          <div
            class="col-span-2 rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.costs.borderColor, activeBlock === 'costs' ? 'ring-2 ring-red-500/50' : '']"
            @click="handleBlockClick('costs')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.costs.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('red').badge]">{{ blockItems.costs.length }}</span>
            </div>
            <div class="flex flex-wrap gap-2">
              <div class="text-xs text-white/30 italic">
                Coming soon
              </div>
            </div>
          </div>

          <!-- Revenue Streams -->
          <div
            class="col-span-3 rounded-xl border bg-slate-900/40 p-3 cursor-pointer transition-all duration-200 hover:bg-slate-900/60"
            :class="[blockDefinitions.revenue.borderColor, activeBlock === 'revenue' ? 'ring-2 ring-teal-500/50' : '']"
            @click="handleBlockClick('revenue')"
          >
            <div class="flex items-center justify-between mb-2">
              <h3 class="text-xs font-semibold uppercase text-white/60">{{ blockDefinitions.revenue.title }}</h3>
              <span :class="['rounded-full px-2 py-0.5 text-xs', getColorClasses('teal').badge]">{{ blockItems.revenue.length }}</span>
            </div>
            <div class="flex flex-wrap gap-2">
              <div
                v-for="item in blockItems.revenue"
                :key="item.id"
                :class="['rounded-lg border px-3 py-2 text-xs cursor-pointer transition-all hover:scale-[1.02]', getColorClasses('teal').bg, getColorClasses('teal').border, getColorClasses('teal').text]"
                @click.stop="handleItemClick('revenue', item)"
              >
                {{ item.name }}
              </div>
              <button
                class="rounded-lg border border-dashed border-white/20 px-3 py-2 text-xs text-white/40 hover:border-white/40 hover:text-white/60 transition-colors"
                @click.stop="handleAddItem('revenue')"
              >
                + Add Revenue Stream
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Side Panel (expanded block details) -->
      <Transition
        enter-active-class="transition-all duration-300 ease-out"
        enter-from-class="translate-x-full opacity-0"
        enter-to-class="translate-x-0 opacity-100"
        leave-active-class="transition-all duration-200 ease-in"
        leave-from-class="translate-x-0 opacity-100"
        leave-to-class="translate-x-full opacity-0"
      >
        <div
          v-if="activeBlock"
          class="w-96 flex-shrink-0 rounded-2xl border bg-slate-900/80 backdrop-blur-xl flex flex-col"
          :class="[blockDefinitions[activeBlock].borderColor]"
        >
          <div class="flex items-center justify-between border-b border-white/10 p-4">
            <div class="flex items-center gap-3">
              <div :class="['w-3 h-3 rounded-full', `bg-${blockDefinitions[activeBlock].color}-500`]"></div>
              <h3 class="font-semibold text-white">{{ blockDefinitions[activeBlock].title }}</h3>
            </div>
            <button
              class="rounded-lg p-1 text-white/40 hover:bg-white/10 hover:text-white transition-colors"
              @click="activeBlock = null"
            >
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="flex-1 overflow-y-auto p-4 space-y-3">
            <div
              v-for="item in blockItems[activeBlock]"
              :key="item.id"
              :class="['rounded-xl border p-4 cursor-pointer transition-all hover:scale-[1.01]', blockDefinitions[activeBlock].bgColor, blockDefinitions[activeBlock].borderColor]"
            >
              <div class="flex items-start justify-between" @click="handleItemClick(activeBlock, item)">
                <div class="flex-1">
                  <h4 class="font-medium text-white">{{ item.name }}</h4>
                  <p v-if="item.description" class="text-sm text-white/60 mt-1 line-clamp-2">{{ item.description }}</p>
                </div>
                <div class="flex items-center gap-2">
                  <span v-if="item.coverageStatus" :class="['rounded-full px-2 py-0.5 text-xs', getCoverageColor(item.coverageStatus)]">
                    {{ item.coverageStatus }}
                  </span>
                  <span v-else-if="item.status" :class="['rounded-full px-2 py-0.5 text-xs', getStatusColor(item.status)]">
                    {{ item.status }}
                  </span>
                </div>
              </div>

              <!-- Expandable people list for roles -->
              <div v-if="item.assignedPeople && item.assignedPeople.length > 0" class="mt-3">
                <button
                  class="flex items-center gap-2 text-xs text-white/50 hover:text-white/80 transition-colors"
                  @click.stop="toggleRoleExpansion(item.id)"
                >
                  <svg
                    class="h-3 w-3 transition-transform"
                    :class="{ 'rotate-90': expandedRoleId === item.id }"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                  {{ item.assignedPeople.length }} people assigned
                </button>

                <Transition
                  enter-active-class="transition-all duration-200 ease-out"
                  enter-from-class="opacity-0 max-h-0"
                  enter-to-class="opacity-100 max-h-40"
                  leave-active-class="transition-all duration-150 ease-in"
                  leave-from-class="opacity-100 max-h-40"
                  leave-to-class="opacity-0 max-h-0"
                >
                  <div v-if="expandedRoleId === item.id" class="mt-2 ml-5 space-y-1 overflow-hidden">
                    <div
                      v-for="person in item.assignedPeople"
                      :key="person.resourceId"
                      class="flex items-center justify-between text-xs text-white/70 py-1"
                    >
                      <span>{{ person.resourceName }}</span>
                      <span v-if="person.allocationPercentage" class="text-white/40">
                        {{ person.allocationPercentage }}%
                      </span>
                    </div>
                  </div>
                </Transition>
              </div>

              <!-- Activities list for processes -->
              <div v-if="item.activities && item.activities.length > 0" class="mt-3">
                <div class="text-xs text-white/50 mb-2">{{ item.activities.length }} activities</div>
                <div class="flex flex-wrap gap-1">
                  <span
                    v-for="activity in item.activities.slice(0, 4)"
                    :key="activity.id"
                    class="rounded bg-white/10 px-2 py-0.5 text-[10px] text-white/60"
                  >
                    {{ activity.name }}
                  </span>
                  <span v-if="item.activities.length > 4" class="text-[10px] text-white/40">
                    +{{ item.activities.length - 4 }} more
                  </span>
                </div>
              </div>

              <!-- Unlink button for referenced items -->
              <div v-if="item.referenceId" class="mt-3 pt-3 border-t border-white/10">
                <button
                  class="text-xs text-red-400/70 hover:text-red-400 transition-colors"
                  @click.stop="handleUnlinkEntity(activeBlock, item)"
                >
                  Unlink from canvas
                </button>
              </div>

              <div v-if="item.type" class="mt-2 flex items-center gap-2">
                <span class="rounded bg-white/10 px-2 py-0.5 text-xs text-white/50">{{ item.type }}</span>
              </div>
            </div>

            <div v-if="blockItems[activeBlock].length === 0" class="text-center py-8">
              <div class="text-white/40 text-sm mb-4">No items yet</div>
              <button
                class="orbitos-btn-secondary px-4 py-2 text-sm"
                @click="handleAddItem(activeBlock)"
              >
                {{ blockDefinitions[activeBlock].entityType === 'process' || blockDefinitions[activeBlock].entityType === 'role' ? 'Link First Item' : 'Add First Item' }}
              </button>
            </div>
          </div>

          <div class="border-t border-white/10 p-4">
            <button
              class="w-full orbitos-btn-primary py-2 text-sm"
              @click="handleAddItem(activeBlock)"
            >
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              {{ blockDefinitions[activeBlock].entityType === 'process' || blockDefinitions[activeBlock].entityType === 'role' ? 'Link' : 'Add' }} {{ blockDefinitions[activeBlock].title.replace('Key ', '').replace('Customer ', '') }}
            </button>
          </div>
        </div>
      </Transition>
    </div>

    <!-- Kanban View -->
    <div v-else-if="viewMode === 'kanban'" class="flex-1 overflow-x-auto">
      <div class="flex gap-4 h-full min-w-max pb-4">
        <div
          v-for="status in kanbanStatuses"
          :key="status"
          class="w-80 flex-shrink-0 rounded-2xl border border-white/10 bg-white/5 flex flex-col"
        >
          <div class="flex items-center justify-between border-b border-white/10 p-4">
            <div class="flex items-center gap-2">
              <span
                :class="[
                  'w-2 h-2 rounded-full',
                  status === 'Active' ? 'bg-emerald-500' : '',
                  status === 'Planned' ? 'bg-blue-500' : '',
                  status === 'Optimizing' ? 'bg-purple-500' : '',
                  status === 'Sunset' ? 'bg-amber-500' : '',
                ]"
              ></span>
              <h3 class="font-semibold text-white">{{ status }}</h3>
            </div>
            <span class="rounded-full bg-white/10 px-2 py-0.5 text-xs text-white/60">{{ kanbanItems[status].length }}</span>
          </div>

          <div class="flex-1 overflow-y-auto p-3 space-y-2">
            <div
              v-for="item in kanbanItems[status]"
              :key="item.id"
              class="rounded-xl border border-white/10 bg-slate-900/60 p-3 cursor-pointer transition-all hover:bg-slate-900/80 hover:border-white/20"
              @click="handleItemClick(item.block, item)"
            >
              <div class="flex items-start justify-between gap-2">
                <h4 class="font-medium text-white text-sm">{{ item.name }}</h4>
              </div>
              <div class="mt-2 flex items-center gap-2">
                <span :class="['rounded px-2 py-0.5 text-xs', getColorClasses(blockDefinitions[item.block].color).badge]">
                  {{ item.type }}
                </span>
              </div>
            </div>

            <div v-if="kanbanItems[status].length === 0" class="text-center py-8 text-white/30 text-sm">
              No items
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- List View -->
    <div v-else-if="viewMode === 'list'" class="flex-1 overflow-hidden flex flex-col">
      <!-- Search Bar -->
      <div class="mb-4">
        <div class="relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-white/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search items..."
            class="w-full rounded-xl bg-white/5 border border-white/10 pl-10 pr-4 py-3 text-white placeholder-white/30 focus:border-purple-500 focus:outline-none focus:ring-1 focus:ring-purple-500"
          />
        </div>
      </div>

      <!-- Table -->
      <div class="flex-1 orbitos-glass-subtle overflow-hidden rounded-xl">
        <div class="overflow-x-auto h-full">
          <table class="w-full text-left text-sm">
            <thead class="bg-black/20 text-xs uppercase text-white/40 sticky top-0">
              <tr>
                <th class="px-6 py-4 cursor-pointer hover:text-white/60 transition-colors" @click="toggleSort('name')">
                  <div class="flex items-center gap-2">
                    Name
                    <svg v-if="sortField === 'name'" class="h-3 w-3" :class="{ 'rotate-180': sortDirection === 'desc' }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                    </svg>
                  </div>
                </th>
                <th class="px-6 py-4 cursor-pointer hover:text-white/60 transition-colors" @click="toggleSort('type')">
                  <div class="flex items-center gap-2">
                    Type
                    <svg v-if="sortField === 'type'" class="h-3 w-3" :class="{ 'rotate-180': sortDirection === 'desc' }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                    </svg>
                  </div>
                </th>
                <th class="px-6 py-4">Description</th>
                <th class="px-6 py-4 cursor-pointer hover:text-white/60 transition-colors" @click="toggleSort('status')">
                  <div class="flex items-center gap-2">
                    Status
                    <svg v-if="sortField === 'status'" class="h-3 w-3" :class="{ 'rotate-180': sortDirection === 'desc' }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                    </svg>
                  </div>
                </th>
                <th class="px-6 py-4">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-white/5">
              <tr
                v-for="item in filteredListItems"
                :key="item.id"
                class="hover:bg-white/5 transition-colors cursor-pointer"
                @click="handleItemClick(item.block, item)"
              >
                <td class="px-6 py-4 font-medium text-white">{{ item.name }}</td>
                <td class="px-6 py-4">
                  <span :class="['rounded px-2 py-1 text-xs', getColorClasses(blockDefinitions[item.block].color).badge]">
                    {{ item.type }}
                  </span>
                </td>
                <td class="px-6 py-4 text-white/60 max-w-xs truncate">{{ item.description || '-' }}</td>
                <td class="px-6 py-4">
                  <span :class="['rounded-full px-3 py-1 text-xs', getStatusColor(item.status)]">
                    {{ item.status }}
                  </span>
                </td>
                <td class="px-6 py-4">
                  <button
                    class="text-white/40 hover:text-white transition-colors"
                    @click.stop="handleItemClick(item.block, item)"
                  >
                    <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                </td>
              </tr>
              <tr v-if="filteredListItems.length === 0">
                <td colspan="5" class="px-6 py-12 text-center text-white/40">
                  {{ searchQuery ? 'No items match your search' : 'No items yet' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Entity Edit Modal -->
    <Teleport to="body">
      <BusinessCanvasEntityModal
        v-if="showEntityModal"
        :entity="editingEntity"
        :entity-type="entityType"
        @close="showEntityModal = false"
        @save="showEntityModal = false"
      />
    </Teleport>

    <!-- Link Entity Dialog -->
    <BaseDialog
      v-model="showLinkDialog"
      size="md"
      :title="`Link ${linkDialogBlockType === 'activities' ? 'Process' : 'Role'} to Canvas`"
      subtitle="Select an existing item to link to this canvas block"
    >
      <div class="space-y-2 max-h-80 overflow-y-auto">
        <div
          v-for="item in availableToLink"
          :key="item.id"
          class="rounded-lg border border-white/10 bg-white/5 p-3 cursor-pointer transition-all hover:bg-white/10 hover:border-white/20"
          @click="handleLinkEntity(item.id)"
        >
          <h4 class="font-medium text-white">{{ item.name }}</h4>
          <p v-if="item.description" class="text-sm text-white/60 mt-1">{{ item.description }}</p>
        </div>

        <div v-if="availableToLink.length === 0" class="text-center py-8 text-white/40">
          <p class="mb-4">No items available to link</p>
          <NuxtLink
            :to="linkDialogBlockType === 'activities' ? '/app/processes' : '/app/roles'"
            class="text-purple-400 hover:text-purple-300"
          >
            Create a new {{ linkDialogBlockType === 'activities' ? 'process' : 'role' }} first
          </NuxtLink>
        </div>
      </div>

      <template #footer="{ close }">
        <button class="orbitos-btn-secondary" @click="close">Cancel</button>
      </template>
    </BaseDialog>
  </div>
</template>

<style scoped>
/* Custom scrollbar for side panel */
.overflow-y-auto::-webkit-scrollbar {
  width: 6px;
}

.overflow-y-auto::-webkit-scrollbar-track {
  background: transparent;
}

.overflow-y-auto::-webkit-scrollbar-thumb {
  background-color: rgba(255, 255, 255, 0.1);
  border-radius: 3px;
}

.overflow-y-auto::-webkit-scrollbar-thumb:hover {
  background-color: rgba(255, 255, 255, 0.2);
}

/* Line clamp utility */
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
