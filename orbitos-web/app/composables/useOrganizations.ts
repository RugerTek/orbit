import { useApi } from '~/composables/useApi'

export interface UserOrganization {
  id: string
  name: string
  slug: string
  description?: string
  logoUrl?: string
  role: 'Member' | 'Admin' | 'Owner'
  createdAt: string
}

// Shared state (singleton pattern - state persists across component instances)
const organizations = ref<UserOrganization[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

// LocalStorage key for persisting current org
const CURRENT_ORG_KEY = 'currentOrganizationId'

// Initialize currentOrganizationId from localStorage immediately on client
// Use a function to get initial value to ensure localStorage is read synchronously
function getInitialOrgId(): string | null {
  if (import.meta.client) {
    return localStorage.getItem(CURRENT_ORG_KEY)
  }
  return null
}

const currentOrganizationId = ref<string | null>(getInitialOrgId())

export function useOrganizations() {
  const { get, post } = useApi()

  // Get current organization from list
  const currentOrganization = computed(() => {
    if (!currentOrganizationId.value) return null
    return organizations.value.find(o => o.id === currentOrganizationId.value) || null
  })

  // Initialize from localStorage (called as fallback, main init happens at module level)
  const initializeOrganization = () => {
    if (import.meta.client && !currentOrganizationId.value) {
      const stored = localStorage.getItem(CURRENT_ORG_KEY)
      if (stored) {
        currentOrganizationId.value = stored
      }
    }
  }

  // Fetch user's organizations
  const fetchOrganizations = async () => {
    isLoading.value = true
    error.value = null
    try {
      const data = await get<UserOrganization[]>('/api/Auth/my-organizations')
      organizations.value = data

      // If no current org selected, or current org not in list, select first one
      if (data.length > 0) {
        if (!currentOrganizationId.value || !data.find(o => o.id === currentOrganizationId.value)) {
          setCurrentOrganization(data[0].id)
        }
      }

      return data
    } catch (e) {
      console.error('Failed to fetch organizations:', e)
      error.value = 'Failed to fetch organizations'
      return []
    } finally {
      isLoading.value = false
    }
  }

  // Set current organization
  const setCurrentOrganization = (orgId: string) => {
    currentOrganizationId.value = orgId
    if (import.meta.client) {
      localStorage.setItem(CURRENT_ORG_KEY, orgId)
    }
  }

  // Create new organization
  const createOrganization = async (data: { name: string; description?: string; logoUrl?: string }) => {
    isLoading.value = true
    error.value = null
    try {
      const newOrg = await post<UserOrganization>('/api/Auth/organizations', data)
      organizations.value.push(newOrg)
      // Switch to newly created org
      setCurrentOrganization(newOrg.id)
      return newOrg
    } catch (e) {
      console.error('Failed to create organization:', e)
      error.value = 'Failed to create organization'
      throw e
    } finally {
      isLoading.value = false
    }
  }

  // Clear state on logout
  const clearOrganizations = () => {
    organizations.value = []
    currentOrganizationId.value = null
    if (import.meta.client) {
      localStorage.removeItem(CURRENT_ORG_KEY)
    }
  }

  return {
    // State
    organizations: readonly(organizations),
    currentOrganization,
    currentOrganizationId: readonly(currentOrganizationId),
    isLoading: readonly(isLoading),
    error: readonly(error),
    // Actions
    initializeOrganization,
    fetchOrganizations,
    setCurrentOrganization,
    createOrganization,
    clearOrganizations,
  }
}
