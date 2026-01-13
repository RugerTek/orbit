import { test, expect } from '@playwright/test'

const API_BASE = 'http://localhost:5027/api/SuperAdmin'

test.describe('Super Admin Dashboard', () => {
  test('should load dashboard stats', async ({ request }) => {
    const response = await request.get(`${API_BASE}/dashboard`)
    expect(response.ok()).toBeTruthy()

    const stats = await response.json()
    expect(stats).toHaveProperty('totalUsers')
    expect(stats).toHaveProperty('totalOrganizations')
    expect(stats).toHaveProperty('totalRoles')
    expect(stats).toHaveProperty('totalFunctions')
    expect(stats).toHaveProperty('recentActivity')
  })
})

test.describe('Users CRUD API', () => {
  let createdUserId: string

  test('should list all users', async ({ request }) => {
    const response = await request.get(`${API_BASE}/users`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    expect(data.length).toBeGreaterThanOrEqual(0)
  })

  test('should create a new user', async ({ request }) => {
    const newUser = {
      email: `test-create-${Date.now()}@example.com`,
      displayName: 'Test Create User',
      password: 'TestPass123!'
    }

    const response = await request.post(`${API_BASE}/users`, {
      data: newUser
    })
    expect(response.ok()).toBeTruthy()

    const user = await response.json()
    expect(user.email).toBe(newUser.email)
    expect(user.displayName).toBe(newUser.displayName)
    expect(user).toHaveProperty('id')
    createdUserId = user.id
  })

  test('should search users by name', async ({ request }) => {
    const response = await request.get(`${API_BASE}/users?search=Rodrigo`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    // Should find at least the main user
    const foundUser = data.find((u: any) => u.displayName.includes('Rodrigo'))
    expect(foundUser).toBeTruthy()
  })

  test('should get user by id', async ({ request }) => {
    // First get list to get a valid ID
    const listResponse = await request.get(`${API_BASE}/users`)
    const users = await listResponse.json()

    if (users.length > 0) {
      const userId = users[0].id
      const response = await request.get(`${API_BASE}/users/${userId}`)
      expect(response.ok()).toBeTruthy()

      const user = await response.json()
      expect(user.id).toBe(userId)
    }
  })

  test('should update user', async ({ request }) => {
    // First get list to find a test user
    const listResponse = await request.get(`${API_BASE}/users`)
    const users = await listResponse.json()

    // Find a test user to update
    const testUser = users.find((u: any) => u.email.includes('test-create'))
    if (testUser) {
      const response = await request.put(`${API_BASE}/users/${testUser.id}`, {
        data: {
          email: testUser.email,
          displayName: 'Updated Test User'
        }
      })
      expect(response.ok()).toBeTruthy()

      const updatedUser = await response.json()
      expect(updatedUser.displayName).toBe('Updated Test User')
    }
  })

  test('should reset user password', async ({ request }) => {
    // First create a user to reset password for
    const testEmail = `test-reset-${Date.now()}@example.com`
    const createResponse = await request.post(`${API_BASE}/users`, {
      data: {
        email: testEmail,
        displayName: 'Test Reset User',
        password: 'InitialPass123'
      }
    })
    expect(createResponse.ok()).toBeTruthy()
    const createdUser = await createResponse.json()

    // Now reset the password
    const response = await request.post(`${API_BASE}/users/${createdUser.id}/reset-password`, {
      data: { NewPassword: 'NewTestPass456' }
    })
    expect(response.ok()).toBeTruthy()

    // Clean up
    await request.delete(`${API_BASE}/users/${createdUser.id}`)
  })

  test('should delete user', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/users`)
    const users = await listResponse.json()

    const testUser = users.find((u: any) => u.email.includes('test-create'))
    if (testUser) {
      const response = await request.delete(`${API_BASE}/users/${testUser.id}`)
      expect(response.ok()).toBeTruthy()
    }
  })
})

test.describe('Organizations CRUD API', () => {
  let createdOrgId: string

  test('should list all organizations', async ({ request }) => {
    const response = await request.get(`${API_BASE}/organizations`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    expect(data.length).toBeGreaterThanOrEqual(0)
  })

  test('should create a new organization', async ({ request }) => {
    const newOrg = {
      name: `Test Org Create ${Date.now()}`,
      slug: `test-org-create-${Date.now()}`
    }

    const response = await request.post(`${API_BASE}/organizations`, {
      data: newOrg
    })
    expect(response.ok()).toBeTruthy()

    const org = await response.json()
    expect(org.name).toBe(newOrg.name)
    expect(org.slug).toBe(newOrg.slug)
    expect(org).toHaveProperty('id')
    createdOrgId = org.id
  })

  test('should search organizations', async ({ request }) => {
    const response = await request.get(`${API_BASE}/organizations?search=Rugertek`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    const foundOrg = data.find((o: any) => o.name.includes('Rugertek'))
    expect(foundOrg).toBeTruthy()
  })

  test('should get organization by id', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/organizations`)
    const orgs = await listResponse.json()

    if (orgs.length > 0) {
      const orgId = orgs[0].id
      const response = await request.get(`${API_BASE}/organizations/${orgId}`)
      expect(response.ok()).toBeTruthy()

      const org = await response.json()
      expect(org.id).toBe(orgId)
    }
  })

  test('should update organization', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/organizations`)
    const orgs = await listResponse.json()

    const testOrg = orgs.find((o: any) => o.name.includes('Test Org Create'))
    if (testOrg) {
      const response = await request.put(`${API_BASE}/organizations/${testOrg.id}`, {
        data: {
          Name: 'Updated Test Org',
          Slug: testOrg.slug
        }
      })
      expect(response.ok()).toBeTruthy()

      const updatedOrg = await response.json()
      expect(updatedOrg.name).toBe('Updated Test Org')
    }
  })

  test('should delete organization', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/organizations`)
    const orgs = await listResponse.json()

    const testOrg = orgs.find((o: any) => o.name.includes('Updated Test Org') || o.name.includes('Test Org Create'))
    if (testOrg) {
      const response = await request.delete(`${API_BASE}/organizations/${testOrg.id}`)
      expect(response.ok()).toBeTruthy()
    }
  })
})

test.describe('Roles CRUD API', () => {
  let createdRoleId: string
  let testOrgId: string

  test.beforeAll(async ({ request }) => {
    // Get an organization ID for role tests
    const orgsResponse = await request.get(`${API_BASE}/organizations`)
    const orgs = await orgsResponse.json()
    if (orgs.length > 0) {
      // Use Rugertek org
      const rugertek = orgs.find((o: any) => o.name === 'Rugertek')
      testOrgId = rugertek?.id || orgs[0].id
    }
  })

  test('should list all roles', async ({ request }) => {
    const response = await request.get(`${API_BASE}/roles`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    expect(data.length).toBeGreaterThanOrEqual(0)
  })

  test('should filter roles by organization', async ({ request }) => {
    if (!testOrgId) {
      test.skip()
      return
    }

    const response = await request.get(`${API_BASE}/roles?organizationId=${testOrgId}`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    // All items should belong to the specified organization
    for (const role of data) {
      expect(role.organizationId).toBe(testOrgId)
    }
  })

  test('should create a new role', async ({ request }) => {
    if (!testOrgId) {
      test.skip()
      return
    }

    const newRole = {
      name: `Test Role Create ${Date.now()}`,
      description: 'A test role for testing',
      organizationId: testOrgId
    }

    const response = await request.post(`${API_BASE}/roles`, {
      data: newRole
    })
    expect(response.ok()).toBeTruthy()

    const role = await response.json()
    expect(role.name).toBe(newRole.name)
    expect(role.description).toBe(newRole.description)
    expect(role.organizationId).toBe(testOrgId)
    expect(role).toHaveProperty('id')
    createdRoleId = role.id
  })

  test('should search roles', async ({ request }) => {
    const response = await request.get(`${API_BASE}/roles?search=Administrator`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    const foundRole = data.find((r: any) => r.name.includes('Administrator'))
    expect(foundRole).toBeTruthy()
  })

  test('should get role by id', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/roles`)
    const roles = await listResponse.json()

    if (roles.length > 0) {
      const roleId = roles[0].id
      const response = await request.get(`${API_BASE}/roles/${roleId}`)
      expect(response.ok()).toBeTruthy()

      const role = await response.json()
      expect(role.id).toBe(roleId)
    }
  })

  test('should update role', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/roles`)
    const roles = await listResponse.json()

    const testRole = roles.find((r: any) => r.name.includes('Test Role Create'))
    if (testRole) {
      const response = await request.put(`${API_BASE}/roles/${testRole.id}`, {
        data: {
          name: 'Updated Test Role',
          description: 'Updated description',
          organizationId: testRole.organizationId
        }
      })
      expect(response.ok()).toBeTruthy()

      const updatedRole = await response.json()
      expect(updatedRole.name).toBe('Updated Test Role')
    }
  })

  test('should delete role', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/roles`)
    const roles = await listResponse.json()

    const testRole = roles.find((r: any) => r.name.includes('Updated Test Role') || r.name.includes('Test Role Create'))
    if (testRole) {
      const response = await request.delete(`${API_BASE}/roles/${testRole.id}`)
      expect(response.ok()).toBeTruthy()
    }
  })
})

test.describe('Functions CRUD API', () => {
  let createdFunctionId: string
  let testOrgId: string

  test.beforeAll(async ({ request }) => {
    // Get an organization ID for function tests
    const orgsResponse = await request.get(`${API_BASE}/organizations`)
    const orgs = await orgsResponse.json()
    if (orgs.length > 0) {
      // Use Rugertek org
      const rugertek = orgs.find((o: any) => o.name === 'Rugertek')
      testOrgId = rugertek?.id || orgs[0].id
    }
  })

  test('should list all functions', async ({ request }) => {
    const response = await request.get(`${API_BASE}/functions`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    expect(data.length).toBeGreaterThanOrEqual(0)
  })

  test('should filter functions by organization', async ({ request }) => {
    if (!testOrgId) {
      test.skip()
      return
    }

    const response = await request.get(`${API_BASE}/functions?organizationId=${testOrgId}`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    // All items should belong to the specified organization
    for (const func of data) {
      expect(func.organizationId).toBe(testOrgId)
    }
  })

  test('should create a new function', async ({ request }) => {
    if (!testOrgId) {
      test.skip()
      return
    }

    const newFunction = {
      name: `Test Function Create ${Date.now()}`,
      description: 'A test function for testing',
      category: 'Testing',
      organizationId: testOrgId
    }

    const response = await request.post(`${API_BASE}/functions`, {
      data: newFunction
    })
    expect(response.ok()).toBeTruthy()

    const func = await response.json()
    expect(func.name).toBe(newFunction.name)
    expect(func.description).toBe(newFunction.description)
    expect(func.category).toBe(newFunction.category)
    expect(func.organizationId).toBe(testOrgId)
    expect(func).toHaveProperty('id')
    createdFunctionId = func.id
  })

  test('should search functions', async ({ request }) => {
    const response = await request.get(`${API_BASE}/functions?search=users`)
    expect(response.ok()).toBeTruthy()

    const data = await response.json()
    expect(Array.isArray(data)).toBeTruthy()
    const foundFunc = data.find((f: any) => f.name.includes('users'))
    expect(foundFunc).toBeTruthy()
  })

  test('should get function by id', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/functions`)
    const functions = await listResponse.json()

    if (functions.length > 0) {
      const funcId = functions[0].id
      const response = await request.get(`${API_BASE}/functions/${funcId}`)
      expect(response.ok()).toBeTruthy()

      const func = await response.json()
      expect(func.id).toBe(funcId)
    }
  })

  test('should update function', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/functions`)
    const functions = await listResponse.json()

    const testFunc = functions.find((f: any) => f.name.includes('Test Function Create'))
    if (testFunc) {
      const response = await request.put(`${API_BASE}/functions/${testFunc.id}`, {
        data: {
          name: 'Updated Test Function',
          description: 'Updated description',
          category: 'Updated Category',
          organizationId: testFunc.organizationId
        }
      })
      expect(response.ok()).toBeTruthy()

      const updatedFunc = await response.json()
      expect(updatedFunc.name).toBe('Updated Test Function')
    }
  })

  test('should delete function', async ({ request }) => {
    const listResponse = await request.get(`${API_BASE}/functions`)
    const functions = await listResponse.json()

    const testFunc = functions.find((f: any) => f.name.includes('Updated Test Function') || f.name.includes('Test Function Create'))
    if (testFunc) {
      const response = await request.delete(`${API_BASE}/functions/${testFunc.id}`)
      expect(response.ok()).toBeTruthy()
    }
  })
})

test.describe('Super Admin UI Tests', () => {
  test('should navigate to admin panel and show dashboard', async ({ page }) => {
    await page.goto('/admin')

    // Check dashboard heading
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
  })

  test('should display sidebar navigation', async ({ page }) => {
    await page.goto('/admin')

    // Check sidebar links - using the sidebar specifically
    await expect(page.locator('nav a:has-text("Dashboard")')).toBeVisible()
    await expect(page.locator('nav a:has-text("Users")')).toBeVisible()
    await expect(page.locator('nav a:has-text("Organizations")')).toBeVisible()
    await expect(page.locator('nav a:has-text("Roles")')).toBeVisible()
    await expect(page.locator('nav a:has-text("Functions")')).toBeVisible()
  })

  test('should navigate to users page', async ({ page }) => {
    await page.goto('/admin/users')

    await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()
    await expect(page.getByRole('button', { name: /add user/i })).toBeVisible()
  })

  test('should open create user modal', async ({ page }) => {
    await page.goto('/admin/users')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add user/i }).click()

    // Wait for modal - check for the modal container with form
    await page.waitForTimeout(1000)
    // Look for any visible modal overlay or email input
    const modalVisible = await page.locator('[class*="fixed"][class*="z-50"]').or(page.locator('input[type="email"]')).first().isVisible().catch(() => false)
    expect(modalVisible).toBeTruthy()
  })

  test('should navigate to organizations page', async ({ page }) => {
    await page.goto('/admin/organizations')

    await expect(page.getByRole('heading', { name: 'Organizations' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()
    await expect(page.getByRole('button', { name: /add organization/i })).toBeVisible()
  })

  test('should open create organization modal', async ({ page }) => {
    await page.goto('/admin/organizations')
    await page.waitForLoadState('networkidle')

    await page.getByRole('button', { name: /add organization/i }).click()

    // Wait for modal - check for the modal container
    await page.waitForTimeout(1000)
    // Look for any visible modal overlay or text input
    const modalVisible = await page.locator('[class*="fixed"][class*="z-50"]').or(page.locator('input[placeholder*="name" i]')).first().isVisible().catch(() => false)
    expect(modalVisible).toBeTruthy()
  })

  test('should navigate to roles page', async ({ page }) => {
    await page.goto('/admin/roles')

    await expect(page.getByRole('heading', { name: 'Roles' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()
  })

  test('should have organization filter on roles page', async ({ page }) => {
    await page.goto('/admin/roles')

    // Check for organization filter dropdown
    const filterSelect = page.locator('select')
    await expect(filterSelect).toBeVisible()

    // Should have "All Organizations" option in the select
    const options = await filterSelect.locator('option').allTextContents()
    expect(options.some(opt => opt.includes('All Organizations'))).toBeTruthy()
  })

  test('should navigate to functions page', async ({ page }) => {
    await page.goto('/admin/functions')

    await expect(page.getByRole('heading', { name: 'Functions' })).toBeVisible()
    await expect(page.locator('input[placeholder*="Search"]')).toBeVisible()
  })

  test('should have organization filter on functions page', async ({ page }) => {
    await page.goto('/admin/functions')

    // Check for organization filter dropdown
    const filterSelect = page.locator('select')
    await expect(filterSelect).toBeVisible()

    // Should have "All Organizations" option in the select
    const options = await filterSelect.locator('option').allTextContents()
    expect(options.some(opt => opt.includes('All Organizations'))).toBeTruthy()
  })

  test('should display stats on dashboard', async ({ page }) => {
    await page.goto('/admin')

    // Wait for stats to load
    await page.waitForSelector('text=Total Users', { timeout: 10000 })

    // Check for stat cards - use more specific selectors due to multiple matches
    await expect(page.getByText('Total Users')).toBeVisible()
    // Find the stat card label specifically
    await expect(page.locator('.text-slate-400:has-text("Organizations")').first()).toBeVisible()
    await expect(page.locator('.text-slate-400:has-text("Roles")').first()).toBeVisible()
    await expect(page.locator('.text-slate-400:has-text("Functions")').first()).toBeVisible()
  })

  test('should display quick action links on dashboard', async ({ page }) => {
    await page.goto('/admin')

    // Wait for page to load
    await page.waitForSelector('text=Manage Users')

    await expect(page.getByText('Manage Users')).toBeVisible()
    await expect(page.getByText('Manage Organizations')).toBeVisible()
    await expect(page.getByText('Manage Roles')).toBeVisible()
  })
})
