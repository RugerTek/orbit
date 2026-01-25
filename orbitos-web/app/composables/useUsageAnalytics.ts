// Usage analytics composable for token/cost tracking
export interface ModelUsage {
  modelId: string
  provider: string
  tokensUsed: number
  costCents: number
  responseCount: number
}

export interface AgentUsage {
  agentName: string
  tokensUsed: number
  costCents: number
  responseCount: number
}

export interface DailyUsage {
  date: string
  tokensUsed: number
  costCents: number
  responseCount: number
}

export interface OrganizationUsage {
  organizationId: string
  startDate: string
  endDate: string
  totalTokens: number
  totalCostCents: number
  totalCostDollars: number
  totalResponses: number
  conversationCount: number
  byModel: ModelUsage[]
  byAgent: AgentUsage[]
  dailyUsage: DailyUsage[]
}

export interface ProviderUsage {
  provider: string
  tokensUsed: number
  costCents: number
  costDollars: number
  responseCount: number
}

export interface OrganizationUsageSummary {
  organizationId: string
  organizationName: string
  tokensUsed: number
  costCents: number
  costDollars: number
  responseCount: number
}

export interface GlobalUsage {
  startDate: string
  endDate: string
  totalTokens: number
  totalCostCents: number
  totalCostDollars: number
  totalResponses: number
  organizationCount: number
  byOrganization: OrganizationUsageSummary[]
  byModel: ModelUsage[]
  byProvider: ProviderUsage[]
  dailyUsage: DailyUsage[]
}

export function useUsageAnalytics() {
  const { get } = useApi()
  const { currentOrganization } = useOrganizations()

  const organizationUsage = ref<OrganizationUsage | null>(null)
  const globalUsage = ref<GlobalUsage | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Fetch organization-level usage
  async function fetchOrganizationUsage(
    organizationId?: string,
    startDate?: Date,
    endDate?: Date
  ) {
    const orgId = organizationId || currentOrganization.value?.id
    if (!orgId) {
      error.value = 'No organization selected'
      return
    }

    isLoading.value = true
    error.value = null

    try {
      const params = new URLSearchParams()
      if (startDate) params.append('startDate', startDate.toISOString())
      if (endDate) params.append('endDate', endDate.toISOString())

      const queryString = params.toString() ? `?${params.toString()}` : ''
      const result = await get<OrganizationUsage>(
        `/api/organizations/${orgId}/usage${queryString}`
      )

      if (result) {
        organizationUsage.value = result
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch usage data'
      console.error('Failed to fetch organization usage:', e)
    } finally {
      isLoading.value = false
    }
  }

  // Fetch global usage (super admin only)
  async function fetchGlobalUsage(startDate?: Date, endDate?: Date) {
    isLoading.value = true
    error.value = null

    try {
      const params = new URLSearchParams()
      if (startDate) params.append('startDate', startDate.toISOString())
      if (endDate) params.append('endDate', endDate.toISOString())

      const queryString = params.toString() ? `?${params.toString()}` : ''
      const result = await get<GlobalUsage>(`/api/admin/usage${queryString}`)

      if (result) {
        globalUsage.value = result
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch global usage data'
      console.error('Failed to fetch global usage:', e)
    } finally {
      isLoading.value = false
    }
  }

  // Format tokens for display
  function formatTokens(tokens: number): string {
    if (tokens >= 1000000) {
      return `${(tokens / 1000000).toFixed(2)}M`
    }
    if (tokens >= 1000) {
      return `${(tokens / 1000).toFixed(1)}K`
    }
    return tokens.toLocaleString()
  }

  // Format cost for display
  function formatCost(costCents: number): string {
    const dollars = costCents / 100
    return `$${dollars.toFixed(2)}`
  }

  // Get provider color class
  function getProviderColor(provider: string): string {
    const colors: Record<string, string> = {
      Anthropic: 'bg-orange-500',
      OpenAI: 'bg-emerald-500',
      Google: 'bg-blue-500'
    }
    return colors[provider] || 'bg-gray-500'
  }

  // Get provider text color class
  function getProviderTextColor(provider: string): string {
    const colors: Record<string, string> = {
      Anthropic: 'text-orange-400',
      OpenAI: 'text-emerald-400',
      Google: 'text-blue-400'
    }
    return colors[provider] || 'text-gray-400'
  }

  return {
    organizationUsage,
    globalUsage,
    isLoading,
    error,
    fetchOrganizationUsage,
    fetchGlobalUsage,
    formatTokens,
    formatCost,
    getProviderColor,
    getProviderTextColor
  }
}
