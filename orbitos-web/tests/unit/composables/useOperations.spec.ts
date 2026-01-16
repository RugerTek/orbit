/**
 * =============================================================================
 * OrbitOS useOperations Composable Unit Tests
 * =============================================================================
 * Unit tests for the Operations composable covering People/Resources management.
 *
 * Spec: Operations module - useOperations composable
 * =============================================================================
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mock the useApi composable
const mockGet = vi.fn()
const mockPost = vi.fn()
const mockPut = vi.fn()
const mockDelete = vi.fn()

vi.mock('~/composables/useApi', () => ({
  useApi: () => ({
    get: mockGet,
    post: mockPost,
    put: mockPut,
    delete: mockDelete,
  }),
}))

// Import after mocking
import { useOperations } from '~/composables/useOperations'

describe('useOperations', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('initialization', () => {
    it('should initialize with default values', () => {
      const ops = useOperations()

      expect(ops.isLoading.value).toBe(false)
      expect(ops.error.value).toBeNull()
      expect(ops.processes.value).toEqual([])
      expect(ops.functions.value).toEqual([])
      expect(ops.roles.value).toEqual([])
      expect(ops.people.value).toEqual([])
      expect(ops.resources.value).toEqual([])
      expect(ops.goals.value).toEqual([])
      expect(ops.canvases.value).toEqual([])
    })

    it('should have default organization ID', () => {
      const ops = useOperations()
      expect(ops.organizationId.value).toBe('11111111-1111-1111-1111-111111111111')
    })
  })

  describe('fetchResources', () => {
    it('should fetch and transform resources correctly', async () => {
      const mockResources = [
        {
          id: 'resource-1',
          name: 'John Doe',
          description: 'Senior Developer',
          status: 1, // Active
          resourceType: 0, // Person
          resourceSubtypeId: '77777777-7777-7777-7777-777777777701',
          resourceSubtypeName: 'Employee',
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
          roleAssignmentCount: 2,
          functionCapabilityCount: 3,
        },
      ]

      mockGet.mockResolvedValueOnce(mockResources)

      const ops = useOperations()
      await ops.fetchResources()

      expect(mockGet).toHaveBeenCalledWith(
        '/api/organizations/11111111-1111-1111-1111-111111111111/operations/resources'
      )
      expect(ops.resources.value).toHaveLength(1)
      expect(ops.resources.value[0].name).toBe('John Doe')
      expect(ops.resources.value[0].resourceType).toBe('person')
      expect(ops.resources.value[0].status).toBe('active')
    })

    it('should filter people from resources', async () => {
      const mockResources = [
        {
          id: 'person-1',
          name: 'John Doe',
          resourceType: 0, // Person
          status: 1,
          resourceSubtypeId: '77777777-7777-7777-7777-777777777701',
          resourceSubtypeName: 'Employee',
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
          roleAssignmentCount: 0,
          functionCapabilityCount: 0,
        },
        {
          id: 'tool-1',
          name: 'Slack',
          resourceType: 2, // Tool
          status: 1,
          resourceSubtypeId: '77777777-7777-7777-7777-777777777702',
          resourceSubtypeName: 'Software',
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
          roleAssignmentCount: 0,
          functionCapabilityCount: 0,
        },
      ]

      mockGet.mockResolvedValueOnce(mockResources)

      const ops = useOperations()
      await ops.fetchResources()

      expect(ops.resources.value).toHaveLength(2)
      expect(ops.people.value).toHaveLength(1)
      expect(ops.people.value[0].name).toBe('John Doe')
    })

    it('should handle fetch error', async () => {
      mockGet.mockRejectedValueOnce(new Error('Network error'))

      const ops = useOperations()
      await ops.fetchResources()

      expect(ops.error.value).toBe('Failed to fetch resources')
      expect(ops.resources.value).toEqual([])
    })

    it('should set loading state during fetch', async () => {
      let resolvePromise: (value: unknown[]) => void
      const pendingPromise = new Promise<unknown[]>((resolve) => {
        resolvePromise = resolve
      })

      mockGet.mockReturnValueOnce(pendingPromise)

      const ops = useOperations()
      const fetchPromise = ops.fetchResources()

      expect(ops.isLoading.value).toBe(true)

      resolvePromise!([])
      await fetchPromise

      expect(ops.isLoading.value).toBe(false)
    })
  })

  describe('fetchProcesses', () => {
    it('should fetch and transform processes correctly', async () => {
      const mockProcesses = [
        {
          id: 'process-1',
          name: 'Onboarding',
          purpose: 'New employee onboarding',
          status: 1, // Active
          stateType: 0, // Current
          frequency: 3, // On-demand
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
          activityCount: 5,
          activities: [],
        },
      ]

      mockGet.mockResolvedValueOnce(mockProcesses)

      const ops = useOperations()
      await ops.fetchProcesses()

      expect(ops.processes.value).toHaveLength(1)
      expect(ops.processes.value[0].name).toBe('Onboarding')
      expect(ops.processes.value[0].status).toBe('active')
      expect(ops.processes.value[0].frequency).toBe('on_demand')
    })
  })

  describe('fetchRoles', () => {
    it('should fetch and transform roles correctly', async () => {
      const mockRoles = [
        {
          id: 'role-1',
          name: 'Senior Developer',
          description: 'Technical lead role',
          department: 'Engineering',
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
          assignmentCount: 2,
          functionCount: 5,
        },
      ]

      mockGet.mockResolvedValueOnce(mockRoles)

      const ops = useOperations()
      await ops.fetchRoles()

      expect(ops.roles.value).toHaveLength(1)
      expect(ops.roles.value[0].name).toBe('Senior Developer')
      expect(ops.roles.value[0].coverageStatus).toBe('covered')
    })

    it('should set coverage status based on assignment count', async () => {
      const mockRoles = [
        {
          id: 'role-uncovered',
          name: 'Uncovered Role',
          assignmentCount: 0,
          functionCount: 0,
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 'role-at-risk',
          name: 'At Risk Role',
          assignmentCount: 1,
          functionCount: 0,
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 'role-covered',
          name: 'Covered Role',
          assignmentCount: 3,
          functionCount: 0,
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
      ]

      mockGet.mockResolvedValueOnce(mockRoles)

      const ops = useOperations()
      await ops.fetchRoles()

      expect(ops.roles.value[0].coverageStatus).toBe('uncovered')
      expect(ops.roles.value[1].coverageStatus).toBe('at_risk')
      expect(ops.roles.value[2].coverageStatus).toBe('covered')
    })
  })

  describe('fetchFunctions', () => {
    it('should fetch and transform functions correctly', async () => {
      const mockFunctions = [
        {
          id: 'func-1',
          name: 'Code Review',
          description: 'Review pull requests',
          category: 'Engineering',
          status: 1,
          organizationId: '11111111-1111-1111-1111-111111111111',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
          capabilityCount: 3,
          roleCount: 2,
        },
      ]

      mockGet.mockResolvedValueOnce(mockFunctions)

      const ops = useOperations()
      await ops.fetchFunctions()

      expect(ops.functions.value).toHaveLength(1)
      expect(ops.functions.value[0].name).toBe('Code Review')
      expect(ops.functions.value[0].coverageStatus).toBe('covered')
    })
  })

  describe('createRole', () => {
    it('should create a new role', async () => {
      const newRole = {
        name: 'New Role',
        description: 'A new role',
        department: 'Engineering',
      }

      const mockResponse = {
        id: 'new-role-id',
        ...newRole,
        organizationId: '11111111-1111-1111-1111-111111111111',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
        assignmentCount: 0,
        functionCount: 0,
      }

      mockPost.mockResolvedValueOnce(mockResponse)

      const ops = useOperations()
      const result = await ops.createRole(newRole)

      expect(mockPost).toHaveBeenCalledWith(
        '/api/organizations/11111111-1111-1111-1111-111111111111/operations/roles',
        newRole
      )
      expect(result.name).toBe('New Role')
      expect(ops.roles.value).toContainEqual(expect.objectContaining({ name: 'New Role' }))
    })
  })

  describe('createFunction', () => {
    it('should create a new function', async () => {
      const newFunction = {
        name: 'New Function',
        description: 'A new function',
        category: 'Operations',
      }

      const mockResponse = {
        id: 'new-func-id',
        ...newFunction,
        status: 1,
        organizationId: '11111111-1111-1111-1111-111111111111',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
        capabilityCount: 0,
        roleCount: 0,
      }

      mockPost.mockResolvedValueOnce(mockResponse)

      const ops = useOperations()
      const result = await ops.createFunction(newFunction)

      expect(mockPost).toHaveBeenCalledWith(
        '/api/organizations/11111111-1111-1111-1111-111111111111/operations/functions',
        newFunction
      )
      expect(result.name).toBe('New Function')
    })
  })

  describe('fetchRoleAssignments', () => {
    it('should fetch all role assignments', async () => {
      const mockAssignments = [
        {
          id: 'assignment-1',
          resourceId: 'person-1',
          resourceName: 'John Doe',
          roleId: 'role-1',
          roleName: 'Developer',
          allocationPercentage: 50,
          isPrimary: true,
          createdAt: '2024-01-01T00:00:00Z',
        },
      ]

      mockGet.mockResolvedValueOnce(mockAssignments)

      const ops = useOperations()
      const result = await ops.fetchRoleAssignments()

      expect(mockGet).toHaveBeenCalledWith(
        '/api/organizations/11111111-1111-1111-1111-111111111111/operations/role-assignments'
      )
      expect(result).toHaveLength(1)
      expect(ops.roleAssignments.value).toEqual(mockAssignments)
    })

    it('should fetch role assignments filtered by resource', async () => {
      mockGet.mockResolvedValueOnce([])

      const ops = useOperations()
      await ops.fetchRoleAssignments('person-1')

      expect(mockGet).toHaveBeenCalledWith(
        '/api/organizations/11111111-1111-1111-1111-111111111111/operations/role-assignments?resourceId=person-1'
      )
    })
  })

  describe('createRoleAssignment', () => {
    it('should create a new role assignment', async () => {
      const newAssignment = {
        resourceId: 'person-1',
        roleId: 'role-1',
        allocationPercentage: 50,
        isPrimary: true,
      }

      const mockResponse = {
        id: 'new-assignment-id',
        ...newAssignment,
        resourceName: 'John Doe',
        roleName: 'Developer',
        createdAt: '2024-01-01T00:00:00Z',
      }

      mockPost.mockResolvedValueOnce(mockResponse)

      const ops = useOperations()
      const result = await ops.createRoleAssignment(newAssignment)

      expect(mockPost).toHaveBeenCalledWith(
        '/api/organizations/11111111-1111-1111-1111-111111111111/operations/role-assignments',
        newAssignment
      )
      expect(result.id).toBe('new-assignment-id')
    })
  })

  describe('deleteRoleAssignment', () => {
    it('should delete a role assignment', async () => {
      mockDelete.mockResolvedValueOnce(undefined)

      const ops = useOperations()
      ops.roleAssignments.value = [
        {
          id: 'assignment-1',
          resourceId: 'person-1',
          resourceName: 'John',
          roleId: 'role-1',
          roleName: 'Dev',
          isPrimary: true,
          createdAt: '2024-01-01T00:00:00Z',
        },
      ]

      await ops.deleteRoleAssignment('assignment-1')

      expect(mockDelete).toHaveBeenCalledWith(
        '/api/organizations/11111111-1111-1111-1111-111111111111/operations/role-assignments/assignment-1'
      )
      expect(ops.roleAssignments.value).toHaveLength(0)
    })
  })

  describe('stats computed', () => {
    it('should compute stats correctly', async () => {
      // Mock data
      mockGet
        .mockResolvedValueOnce([
          { id: 'p1', name: 'Process 1', status: 1, stateType: 0, activityCount: 0, organizationId: 'org', createdAt: '', updatedAt: '' },
        ])
        .mockResolvedValueOnce([
          { id: 'f1', name: 'Func 1', status: 1, capabilityCount: 1, roleCount: 1, organizationId: 'org', createdAt: '', updatedAt: '' },
        ])
        .mockResolvedValueOnce([
          { id: 'r1', name: 'Role 1', assignmentCount: 2, functionCount: 1, organizationId: 'org', createdAt: '', updatedAt: '' },
        ])
        .mockResolvedValueOnce([
          { id: 'res1', name: 'Person 1', resourceType: 0, status: 1, resourceSubtypeId: 'sub1', resourceSubtypeName: 'Employee', organizationId: 'org', createdAt: '', updatedAt: '', roleAssignmentCount: 0, functionCapabilityCount: 0 },
        ])

      const ops = useOperations()

      await ops.fetchProcesses()
      await ops.fetchFunctions()
      await ops.fetchRoles()
      await ops.fetchResources()

      expect(ops.stats.value.processCount).toBe(1)
      expect(ops.stats.value.functionCount).toBe(1)
      expect(ops.stats.value.roleCount).toBe(1)
      expect(ops.stats.value.peopleCount).toBe(1)
    })
  })
})
