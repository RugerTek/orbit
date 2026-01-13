export interface User {
  id: string
  email: string
  displayName: string
  firstName?: string
  lastName?: string
  avatarUrl?: string
  hasPassword: boolean
  hasGoogleId: boolean
  hasAzureAdId: boolean
  lastLoginAt?: string
  createdAt: string
  updatedAt: string
  organizationCount: number
}

export interface Organization {
  id: string
  name: string
  slug: string
  description?: string
  logoUrl?: string
  azureAdTenantId?: string
  createdAt: string
  updatedAt: string
  memberCount: number
  roleCount: number
  functionCount: number
}

export interface Role {
  id: string
  name: string
  description?: string
  purpose?: string
  department?: string
  organizationId: string
  organizationName: string
  createdAt: string
  updatedAt: string
  functionCount: number
  userCount: number
}

export interface FunctionEntity {
  id: string
  name: string
  description?: string
  purpose?: string
  category?: string
  organizationId: string
  organizationName: string
  createdAt: string
  updatedAt: string
  roleCount: number
}

export interface OrganizationMembership {
  id: string
  userId: string
  userEmail: string
  userDisplayName: string
  organizationId: string
  organizationName: string
  role: number
  createdAt: string
}

export interface DashboardStats {
  totalUsers: number
  totalOrganizations: number
  totalRoles: number
  totalFunctions: number
  usersLast30Days: number
  orgsLast30Days: number
  recentActivity: Array<{
    type: string
    description: string
    timestamp: string
  }>
}

export const useSuperAdmin = () => {
  const config = useRuntimeConfig()
  const baseUrl = `${config.public.apiBaseUrl}/api/SuperAdmin`

  // Dashboard
  const getDashboardStats = async (): Promise<DashboardStats> => {
    return await $fetch<DashboardStats>(`${baseUrl}/dashboard`)
  }

  // Users
  const getUsers = async (search?: string, page = 1, pageSize = 20): Promise<User[]> => {
    const params = new URLSearchParams()
    if (search) params.append('search', search)
    params.append('page', page.toString())
    params.append('pageSize', pageSize.toString())
    return await $fetch<User[]>(`${baseUrl}/users?${params}`)
  }

  const getUsersCount = async (search?: string): Promise<number> => {
    const params = new URLSearchParams()
    if (search) params.append('search', search)
    return await $fetch<number>(`${baseUrl}/users/count?${params}`)
  }

  const getUser = async (id: string): Promise<User> => {
    return await $fetch<User>(`${baseUrl}/users/${id}`)
  }

  const createUser = async (data: { email: string; displayName: string; firstName?: string; lastName?: string; password?: string }): Promise<User> => {
    return await $fetch<User>(`${baseUrl}/users`, {
      method: 'POST',
      body: data
    })
  }

  const updateUser = async (id: string, data: { email: string; displayName: string; firstName?: string; lastName?: string }): Promise<User> => {
    return await $fetch<User>(`${baseUrl}/users/${id}`, {
      method: 'PUT',
      body: data
    })
  }

  const deleteUser = async (id: string): Promise<void> => {
    await $fetch(`${baseUrl}/users/${id}`, { method: 'DELETE' })
  }

  const resetUserPassword = async (id: string, newPassword: string): Promise<void> => {
    await $fetch(`${baseUrl}/users/${id}/reset-password`, {
      method: 'POST',
      body: { newPassword }
    })
  }

  // Organizations
  const getOrganizations = async (search?: string, page = 1, pageSize = 20): Promise<Organization[]> => {
    const params = new URLSearchParams()
    if (search) params.append('search', search)
    params.append('page', page.toString())
    params.append('pageSize', pageSize.toString())
    return await $fetch<Organization[]>(`${baseUrl}/organizations?${params}`)
  }

  const getOrganizationsCount = async (search?: string): Promise<number> => {
    const params = new URLSearchParams()
    if (search) params.append('search', search)
    return await $fetch<number>(`${baseUrl}/organizations/count?${params}`)
  }

  const getOrganization = async (id: string): Promise<Organization> => {
    return await $fetch<Organization>(`${baseUrl}/organizations/${id}`)
  }

  const createOrganization = async (data: { name: string; slug: string; description?: string; logoUrl?: string }): Promise<Organization> => {
    return await $fetch<Organization>(`${baseUrl}/organizations`, {
      method: 'POST',
      body: data
    })
  }

  const updateOrganization = async (id: string, data: { name: string; slug: string; description?: string; logoUrl?: string }): Promise<Organization> => {
    return await $fetch<Organization>(`${baseUrl}/organizations/${id}`, {
      method: 'PUT',
      body: data
    })
  }

  const deleteOrganization = async (id: string): Promise<void> => {
    await $fetch(`${baseUrl}/organizations/${id}`, { method: 'DELETE' })
  }

  // Organization Members
  const getOrganizationMembers = async (orgId: string): Promise<OrganizationMembership[]> => {
    return await $fetch<OrganizationMembership[]>(`${baseUrl}/organizations/${orgId}/members`)
  }

  const addMember = async (data: { userId: string; organizationId: string; role?: number }): Promise<OrganizationMembership> => {
    return await $fetch<OrganizationMembership>(`${baseUrl}/memberships`, {
      method: 'POST',
      body: data
    })
  }

  const removeMember = async (membershipId: string): Promise<void> => {
    await $fetch(`${baseUrl}/memberships/${membershipId}`, { method: 'DELETE' })
  }

  // Roles
  const getRoles = async (organizationId?: string, search?: string, page = 1, pageSize = 20): Promise<Role[]> => {
    const params = new URLSearchParams()
    if (organizationId) params.append('organizationId', organizationId)
    if (search) params.append('search', search)
    params.append('page', page.toString())
    params.append('pageSize', pageSize.toString())
    return await $fetch<Role[]>(`${baseUrl}/roles?${params}`)
  }

  const getRolesCount = async (organizationId?: string, search?: string): Promise<number> => {
    const params = new URLSearchParams()
    if (organizationId) params.append('organizationId', organizationId)
    if (search) params.append('search', search)
    return await $fetch<number>(`${baseUrl}/roles/count?${params}`)
  }

  const getRole = async (id: string): Promise<Role> => {
    return await $fetch<Role>(`${baseUrl}/roles/${id}`)
  }

  const createRole = async (data: { name: string; description?: string; purpose?: string; department?: string; organizationId: string }): Promise<Role> => {
    return await $fetch<Role>(`${baseUrl}/roles`, {
      method: 'POST',
      body: data
    })
  }

  const updateRole = async (id: string, data: { name: string; description?: string; purpose?: string; department?: string }): Promise<Role> => {
    return await $fetch<Role>(`${baseUrl}/roles/${id}`, {
      method: 'PUT',
      body: data
    })
  }

  const deleteRole = async (id: string): Promise<void> => {
    await $fetch(`${baseUrl}/roles/${id}`, { method: 'DELETE' })
  }

  // Functions
  const getFunctions = async (organizationId?: string, search?: string, page = 1, pageSize = 20): Promise<FunctionEntity[]> => {
    const params = new URLSearchParams()
    if (organizationId) params.append('organizationId', organizationId)
    if (search) params.append('search', search)
    params.append('page', page.toString())
    params.append('pageSize', pageSize.toString())
    return await $fetch<FunctionEntity[]>(`${baseUrl}/functions?${params}`)
  }

  const getFunctionsCount = async (organizationId?: string, search?: string): Promise<number> => {
    const params = new URLSearchParams()
    if (organizationId) params.append('organizationId', organizationId)
    if (search) params.append('search', search)
    return await $fetch<number>(`${baseUrl}/functions/count?${params}`)
  }

  const getFunction = async (id: string): Promise<FunctionEntity> => {
    return await $fetch<FunctionEntity>(`${baseUrl}/functions/${id}`)
  }

  const createFunction = async (data: { name: string; description?: string; purpose?: string; category?: string; organizationId: string }): Promise<FunctionEntity> => {
    return await $fetch<FunctionEntity>(`${baseUrl}/functions`, {
      method: 'POST',
      body: data
    })
  }

  const updateFunction = async (id: string, data: { name: string; description?: string; purpose?: string; category?: string }): Promise<FunctionEntity> => {
    return await $fetch<FunctionEntity>(`${baseUrl}/functions/${id}`, {
      method: 'PUT',
      body: data
    })
  }

  const deleteFunction = async (id: string): Promise<void> => {
    await $fetch(`${baseUrl}/functions/${id}`, { method: 'DELETE' })
  }

  return {
    // Dashboard
    getDashboardStats,
    // Users
    getUsers,
    getUsersCount,
    getUser,
    createUser,
    updateUser,
    deleteUser,
    resetUserPassword,
    // Organizations
    getOrganizations,
    getOrganizationsCount,
    getOrganization,
    createOrganization,
    updateOrganization,
    deleteOrganization,
    getOrganizationMembers,
    addMember,
    removeMember,
    // Roles
    getRoles,
    getRolesCount,
    getRole,
    createRole,
    updateRole,
    deleteRole,
    // Functions
    getFunctions,
    getFunctionsCount,
    getFunction,
    createFunction,
    updateFunction,
    deleteFunction
  }
}
