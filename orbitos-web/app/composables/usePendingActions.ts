/**
 * Composable for managing AI-proposed pending actions
 * Provides reactive state and methods for the approval workflow
 */

import { useOrganizations } from '~/composables/useOrganizations'

export interface PendingAction {
  id: string
  actionType: 'Create' | 'Update' | 'Delete'
  entityType: string
  entityId?: string
  entityName?: string
  proposedData: string // JSON string
  previousData?: string // JSON string (for updates)
  reason: string
  status: 'Pending' | 'Approved' | 'Rejected' | 'Modified' | 'Executed' | 'Failed' | 'Expired'
  agentId?: string
  agentName?: string
  conversationId?: string
  reviewedByUserName?: string
  reviewedAt?: string
  executedAt?: string
  createdAt: string
  expiresAt?: string
}

export interface PendingActionDetail extends PendingAction {
  messageId?: string
  userModifications?: string
  finalData?: string
  executionResult?: string
  resultEntityId?: string
  reviewedByUserId?: string
  rejectionReason?: string
}

export interface PendingActionCounts {
  pending: number
  approved: number
  rejected: number
  executed: number
  failed: number
  expired: number
}

export interface PendingActionsListResponse {
  items: PendingAction[]
  totalCount: number
  page: number
  pageSize: number
}

export interface ApproveActionRequest {
  modifications?: string
}

export interface RejectActionRequest {
  reason?: string
}

export interface PendingActionResult {
  success: boolean
  actionId: string
  status: string
  resultEntityId?: string
  message?: string
}

export function usePendingActions() {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase || 'https://localhost:5001'

  // State
  const pendingActions = ref<PendingAction[]>([])
  const currentAction = ref<PendingActionDetail | null>(null)
  const counts = ref<PendingActionCounts>({ pending: 0, approved: 0, rejected: 0, executed: 0, failed: 0, expired: 0 })
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const totalCount = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(20)

  // Get organization ID reactively from useOrganizations, falling back to localStorage
  const getOrganizationId = (): string => {
    try {
      const { currentOrganizationId } = useOrganizations()
      if (currentOrganizationId.value) {
        return currentOrganizationId.value
      }
    } catch {
      // Composable not available
    }
    if (typeof window !== 'undefined') {
      return localStorage.getItem('currentOrganizationId') || '11111111-1111-1111-1111-111111111111'
    }
    return '11111111-1111-1111-1111-111111111111'
  }

  /**
   * Fetch pending actions for the organization
   */
  async function fetchPendingActions(options?: {
    conversationId?: string
    status?: string
    entityType?: string
    page?: number
    pageSize?: number
  }) {
    isLoading.value = true
    error.value = null

    try {
      const orgId = getOrganizationId()
      const params = new URLSearchParams()
      if (options?.conversationId) params.append('conversationId', options.conversationId)
      if (options?.status) params.append('status', options.status)
      if (options?.entityType) params.append('entityType', options.entityType)
      params.append('page', String(options?.page || currentPage.value))
      params.append('pageSize', String(options?.pageSize || pageSize.value))

      const response = await fetch(`${apiBase}/api/organizations/${orgId}/pending-actions?${params}`)
      if (!response.ok) throw new Error('Failed to fetch pending actions')

      const data: PendingActionsListResponse = await response.json()
      pendingActions.value = data.items
      totalCount.value = data.totalCount
      currentPage.value = data.page
      pageSize.value = data.pageSize
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Error fetching pending actions:', e)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Fetch a single pending action with full details
   */
  async function fetchPendingAction(actionId: string) {
    isLoading.value = true
    error.value = null

    try {
      const orgId = getOrganizationId()
      const response = await fetch(`${apiBase}/api/organizations/${orgId}/pending-actions/${actionId}`)
      if (!response.ok) throw new Error('Failed to fetch pending action')

      currentAction.value = await response.json()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Error fetching pending action:', e)
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Approve a pending action, optionally with modifications
   */
  async function approveAction(actionId: string, modifications?: Record<string, unknown>): Promise<PendingActionResult | null> {
    isLoading.value = true
    error.value = null

    try {
      const orgId = getOrganizationId()
      const body: ApproveActionRequest = modifications
        ? { modifications: JSON.stringify(modifications) }
        : {}

      const response = await fetch(`${apiBase}/api/organizations/${orgId}/pending-actions/${actionId}/approve`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || 'Failed to approve action')
      }

      const result: PendingActionResult = await response.json()

      // Update the local state
      const index = pendingActions.value.findIndex(a => a.id === actionId)
      if (index !== -1) {
        pendingActions.value[index].status = result.status as PendingAction['status']
      }

      // Refresh counts
      await fetchCounts()

      return result
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Error approving action:', e)
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Reject a pending action
   */
  async function rejectAction(actionId: string, reason?: string): Promise<PendingActionResult | null> {
    isLoading.value = true
    error.value = null

    try {
      const orgId = getOrganizationId()
      const body: RejectActionRequest = reason ? { reason } : {}

      const response = await fetch(`${apiBase}/api/organizations/${orgId}/pending-actions/${actionId}/reject`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || 'Failed to reject action')
      }

      const result: PendingActionResult = await response.json()

      // Update the local state
      const index = pendingActions.value.findIndex(a => a.id === actionId)
      if (index !== -1) {
        pendingActions.value[index].status = 'Rejected'
      }

      // Refresh counts
      await fetchCounts()

      return result
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Error rejecting action:', e)
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Retry a failed action
   */
  async function retryAction(actionId: string): Promise<PendingActionResult | null> {
    isLoading.value = true
    error.value = null

    try {
      const orgId = getOrganizationId()
      const response = await fetch(`${apiBase}/api/organizations/${orgId}/pending-actions/${actionId}/retry`, {
        method: 'POST'
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || 'Failed to retry action')
      }

      const result: PendingActionResult = await response.json()

      // Update the local state
      const index = pendingActions.value.findIndex(a => a.id === actionId)
      if (index !== -1) {
        pendingActions.value[index].status = result.status as PendingAction['status']
      }

      // Refresh counts
      await fetchCounts()

      return result
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Error retrying action:', e)
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Fetch counts of pending actions by status
   */
  async function fetchCounts() {
    try {
      const orgId = getOrganizationId()
      const response = await fetch(`${apiBase}/api/organizations/${orgId}/pending-actions/counts`)
      if (!response.ok) throw new Error('Failed to fetch counts')

      counts.value = await response.json()
    } catch (e) {
      console.error('Error fetching counts:', e)
    }
  }

  /**
   * Parse proposed data JSON to object
   */
  function parseProposedData(action: PendingAction): Record<string, unknown> {
    try {
      return JSON.parse(action.proposedData)
    } catch {
      return {}
    }
  }

  /**
   * Parse previous data JSON to object
   */
  function parsePreviousData(action: PendingAction): Record<string, unknown> | null {
    if (!action.previousData) return null
    try {
      return JSON.parse(action.previousData)
    } catch {
      return null
    }
  }

  /**
   * Get a human-readable description of the action
   */
  function getActionDescription(action: PendingAction): string {
    const entityName = action.entityName || action.entityType
    switch (action.actionType) {
      case 'Create':
        return `Create new ${action.entityType}: ${entityName}`
      case 'Update':
        return `Update ${action.entityType}: ${entityName}`
      case 'Delete':
        return `Delete ${action.entityType}: ${entityName}`
      default:
        return `${action.actionType} ${action.entityType}`
    }
  }

  /**
   * Get status color for UI display
   */
  function getStatusColor(status: PendingAction['status']): string {
    switch (status) {
      case 'Pending':
        return 'warning'
      case 'Approved':
      case 'Executed':
        return 'success'
      case 'Rejected':
      case 'Failed':
        return 'error'
      case 'Modified':
        return 'info'
      case 'Expired':
        return 'secondary'
      default:
        return 'default'
    }
  }

  /**
   * Get action type color for UI display
   */
  function getActionTypeColor(actionType: PendingAction['actionType']): string {
    switch (actionType) {
      case 'Create':
        return 'success'
      case 'Update':
        return 'info'
      case 'Delete':
        return 'error'
      default:
        return 'default'
    }
  }

  /**
   * Computed: pending actions that need attention
   */
  const pendingCount = computed(() => counts.value.pending)

  /**
   * Computed: has any pending actions
   */
  const hasPendingActions = computed(() => counts.value.pending > 0)

  return {
    // State
    pendingActions,
    currentAction,
    counts,
    isLoading,
    error,
    totalCount,
    currentPage,
    pageSize,

    // Methods
    fetchPendingActions,
    fetchPendingAction,
    approveAction,
    rejectAction,
    retryAction,
    fetchCounts,

    // Helpers
    parseProposedData,
    parsePreviousData,
    getActionDescription,
    getStatusColor,
    getActionTypeColor,

    // Computed
    pendingCount,
    hasPendingActions
  }
}
