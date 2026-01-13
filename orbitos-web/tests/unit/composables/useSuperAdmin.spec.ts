/**
 * =============================================================================
 * OrbitOS Super Admin Composable Unit Tests
 * =============================================================================
 * Tests for the useSuperAdmin composable functions.
 * Uses mocked $fetch to test API interactions.
 *
 * Spec: F002-super-admin.json
 * =============================================================================
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mock useRuntimeConfig
vi.mock('#app', () => ({
  useRuntimeConfig: () => ({
    public: {
      apiBaseUrl: 'http://localhost:5027'
    }
  })
}))

// Mock $fetch
const mockFetch = vi.fn()
vi.stubGlobal('$fetch', mockFetch)

// Import after mocking
import { useSuperAdmin } from '~/composables/useSuperAdmin'

describe('useSuperAdmin', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  // ===========================================================================
  // DASHBOARD TESTS
  // ===========================================================================

  describe('getDashboardStats', () => {
    it('should fetch dashboard stats', async () => {
      const mockStats = {
        totalUsers: 10,
        totalOrganizations: 5,
        totalRoles: 8,
        totalFunctions: 15,
        recentActivity: []
      }
      mockFetch.mockResolvedValueOnce(mockStats)

      const { getDashboardStats } = useSuperAdmin()
      const result = await getDashboardStats()

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5027/api/SuperAdmin/dashboard'
      )
      expect(result).toEqual(mockStats)
    })
  })

  // ===========================================================================
  // USERS TESTS
  // ===========================================================================

  describe('Users API', () => {
    it('should fetch all users', async () => {
      const mockUsers = [
        { id: '1', email: 'test1@example.com', displayName: 'User 1' },
        { id: '2', email: 'test2@example.com', displayName: 'User 2' }
      ]
      mockFetch.mockResolvedValueOnce(mockUsers)

      const { getUsers } = useSuperAdmin()
      const result = await getUsers()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/SuperAdmin/users')
      )
      expect(result).toEqual(mockUsers)
    })

    it('should fetch users with search parameter', async () => {
      const mockUsers = [{ id: '1', email: 'test@example.com', displayName: 'Test' }]
      mockFetch.mockResolvedValueOnce(mockUsers)

      const { getUsers } = useSuperAdmin()
      await getUsers({ search: 'test' })

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('search=test')
      )
    })

    it('should fetch users with pagination', async () => {
      mockFetch.mockResolvedValueOnce([])

      const { getUsers } = useSuperAdmin()
      await getUsers({ skip: 10, take: 20 })

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('skip=10')
      )
      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('take=20')
      )
    })

    it('should fetch users count', async () => {
      mockFetch.mockResolvedValueOnce(42)

      const { getUsersCount } = useSuperAdmin()
      const result = await getUsersCount()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/users/count')
      )
      expect(result).toBe(42)
    })

    it('should fetch single user by id', async () => {
      const mockUser = { id: '123', email: 'test@example.com' }
      mockFetch.mockResolvedValueOnce(mockUser)

      const { getUser } = useSuperAdmin()
      const result = await getUser('123')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/users/123')
      )
      expect(result).toEqual(mockUser)
    })

    it('should create a user', async () => {
      const newUser = { email: 'new@example.com', displayName: 'New User', password: 'Pass123!' }
      const createdUser = { id: '999', ...newUser }
      mockFetch.mockResolvedValueOnce(createdUser)

      const { createUser } = useSuperAdmin()
      const result = await createUser(newUser)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/users'),
        expect.objectContaining({
          method: 'POST',
          body: newUser
        })
      )
      expect(result).toEqual(createdUser)
    })

    it('should update a user', async () => {
      const updateData = { email: 'updated@example.com', displayName: 'Updated User' }
      const updatedUser = { id: '123', ...updateData }
      mockFetch.mockResolvedValueOnce(updatedUser)

      const { updateUser } = useSuperAdmin()
      const result = await updateUser('123', updateData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/users/123'),
        expect.objectContaining({
          method: 'PUT',
          body: updateData
        })
      )
      expect(result).toEqual(updatedUser)
    })

    it('should delete a user', async () => {
      mockFetch.mockResolvedValueOnce(undefined)

      const { deleteUser } = useSuperAdmin()
      await deleteUser('123')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/users/123'),
        expect.objectContaining({ method: 'DELETE' })
      )
    })

    it('should reset user password', async () => {
      mockFetch.mockResolvedValueOnce({ success: true })

      const { resetUserPassword } = useSuperAdmin()
      await resetUserPassword('123', 'NewPass123!')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/users/123/reset-password'),
        expect.objectContaining({
          method: 'POST',
          body: { NewPassword: 'NewPass123!' }
        })
      )
    })
  })

  // ===========================================================================
  // ORGANIZATIONS TESTS
  // ===========================================================================

  describe('Organizations API', () => {
    it('should fetch all organizations', async () => {
      const mockOrgs = [
        { id: '1', name: 'Org 1', slug: 'org-1' },
        { id: '2', name: 'Org 2', slug: 'org-2' }
      ]
      mockFetch.mockResolvedValueOnce(mockOrgs)

      const { getOrganizations } = useSuperAdmin()
      const result = await getOrganizations()

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/organizations')
      )
      expect(result).toEqual(mockOrgs)
    })

    it('should fetch organizations count', async () => {
      mockFetch.mockResolvedValueOnce(25)

      const { getOrganizationsCount } = useSuperAdmin()
      const result = await getOrganizationsCount()

      expect(result).toBe(25)
    })

    it('should create an organization', async () => {
      const newOrg = { name: 'New Org', slug: 'new-org' }
      const createdOrg = { id: '999', ...newOrg }
      mockFetch.mockResolvedValueOnce(createdOrg)

      const { createOrganization } = useSuperAdmin()
      const result = await createOrganization(newOrg)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/organizations'),
        expect.objectContaining({
          method: 'POST',
          body: newOrg
        })
      )
      expect(result).toEqual(createdOrg)
    })

    it('should update an organization', async () => {
      const updateData = { Name: 'Updated Org', Slug: 'updated-org' }
      mockFetch.mockResolvedValueOnce({ id: '123', ...updateData })

      const { updateOrganization } = useSuperAdmin()
      await updateOrganization('123', updateData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/organizations/123'),
        expect.objectContaining({
          method: 'PUT',
          body: updateData
        })
      )
    })

    it('should delete an organization', async () => {
      mockFetch.mockResolvedValueOnce(undefined)

      const { deleteOrganization } = useSuperAdmin()
      await deleteOrganization('123')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/organizations/123'),
        expect.objectContaining({ method: 'DELETE' })
      )
    })
  })

  // ===========================================================================
  // ROLES TESTS
  // ===========================================================================

  describe('Roles API', () => {
    it('should fetch all roles', async () => {
      const mockRoles = [
        { id: '1', name: 'Admin', organizationId: 'org-1' },
        { id: '2', name: 'User', organizationId: 'org-1' }
      ]
      mockFetch.mockResolvedValueOnce(mockRoles)

      const { getRoles } = useSuperAdmin()
      const result = await getRoles()

      expect(result).toEqual(mockRoles)
    })

    it('should fetch roles filtered by organization', async () => {
      const mockRoles = [{ id: '1', name: 'Admin', organizationId: 'org-123' }]
      mockFetch.mockResolvedValueOnce(mockRoles)

      const { getRoles } = useSuperAdmin()
      await getRoles({ organizationId: 'org-123' })

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('organizationId=org-123')
      )
    })

    it('should create a role', async () => {
      const newRole = { name: 'New Role', description: 'Desc', organizationId: 'org-1' }
      mockFetch.mockResolvedValueOnce({ id: '999', ...newRole })

      const { createRole } = useSuperAdmin()
      await createRole(newRole)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/roles'),
        expect.objectContaining({
          method: 'POST',
          body: newRole
        })
      )
    })

    it('should update a role', async () => {
      const updateData = { name: 'Updated Role', description: 'Updated', organizationId: 'org-1' }
      mockFetch.mockResolvedValueOnce({ id: '123', ...updateData })

      const { updateRole } = useSuperAdmin()
      await updateRole('123', updateData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/roles/123'),
        expect.objectContaining({ method: 'PUT' })
      )
    })

    it('should delete a role', async () => {
      mockFetch.mockResolvedValueOnce(undefined)

      const { deleteRole } = useSuperAdmin()
      await deleteRole('123')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/roles/123'),
        expect.objectContaining({ method: 'DELETE' })
      )
    })
  })

  // ===========================================================================
  // FUNCTIONS TESTS
  // ===========================================================================

  describe('Functions API', () => {
    it('should fetch all functions', async () => {
      const mockFunctions = [
        { id: '1', name: 'users.read', category: 'Users' },
        { id: '2', name: 'users.write', category: 'Users' }
      ]
      mockFetch.mockResolvedValueOnce(mockFunctions)

      const { getFunctions } = useSuperAdmin()
      const result = await getFunctions()

      expect(result).toEqual(mockFunctions)
    })

    it('should fetch functions filtered by organization', async () => {
      mockFetch.mockResolvedValueOnce([])

      const { getFunctions } = useSuperAdmin()
      await getFunctions({ organizationId: 'org-123' })

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('organizationId=org-123')
      )
    })

    it('should create a function', async () => {
      const newFunction = {
        name: 'test.read',
        description: 'Test permission',
        category: 'Testing',
        organizationId: 'org-1'
      }
      mockFetch.mockResolvedValueOnce({ id: '999', ...newFunction })

      const { createFunction } = useSuperAdmin()
      await createFunction(newFunction)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/functions'),
        expect.objectContaining({
          method: 'POST',
          body: newFunction
        })
      )
    })

    it('should update a function', async () => {
      const updateData = {
        name: 'test.updated',
        description: 'Updated',
        category: 'Testing',
        organizationId: 'org-1'
      }
      mockFetch.mockResolvedValueOnce({ id: '123', ...updateData })

      const { updateFunction } = useSuperAdmin()
      await updateFunction('123', updateData)

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/functions/123'),
        expect.objectContaining({ method: 'PUT' })
      )
    })

    it('should delete a function', async () => {
      mockFetch.mockResolvedValueOnce(undefined)

      const { deleteFunction } = useSuperAdmin()
      await deleteFunction('123')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/functions/123'),
        expect.objectContaining({ method: 'DELETE' })
      )
    })
  })

  // ===========================================================================
  // ORGANIZATION MEMBERS TESTS
  // ===========================================================================

  describe('Organization Members API', () => {
    it('should fetch organization members', async () => {
      const mockMembers = [
        { id: '1', userId: 'user-1', organizationId: 'org-1' }
      ]
      mockFetch.mockResolvedValueOnce(mockMembers)

      const { getOrganizationMembers } = useSuperAdmin()
      const result = await getOrganizationMembers('org-1')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/organizations/org-1/members')
      )
      expect(result).toEqual(mockMembers)
    })

    it('should add a member to organization', async () => {
      mockFetch.mockResolvedValueOnce({ id: '999', userId: 'user-1', organizationId: 'org-1' })

      const { addMember } = useSuperAdmin()
      await addMember('org-1', 'user-1')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/memberships'),
        expect.objectContaining({
          method: 'POST',
          body: { organizationId: 'org-1', userId: 'user-1' }
        })
      )
    })

    it('should remove a member from organization', async () => {
      mockFetch.mockResolvedValueOnce(undefined)

      const { removeMember } = useSuperAdmin()
      await removeMember('membership-123')

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/memberships/membership-123'),
        expect.objectContaining({ method: 'DELETE' })
      )
    })
  })

  // ===========================================================================
  // ERROR HANDLING TESTS
  // ===========================================================================

  describe('Error Handling', () => {
    it('should propagate API errors', async () => {
      const apiError = new Error('Network error')
      mockFetch.mockRejectedValueOnce(apiError)

      const { getUsers } = useSuperAdmin()

      await expect(getUsers()).rejects.toThrow('Network error')
    })

    it('should handle 404 errors', async () => {
      mockFetch.mockRejectedValueOnce({ status: 404, message: 'Not found' })

      const { getUser } = useSuperAdmin()

      await expect(getUser('non-existent')).rejects.toMatchObject({ status: 404 })
    })
  })
})
