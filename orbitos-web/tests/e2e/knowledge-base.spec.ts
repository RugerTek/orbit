import { test, expect } from '@playwright/test'

test.describe('Knowledge Base', () => {
  test.beforeEach(async ({ page }) => {
    // Login first
    await page.goto('/')
    await page.fill('input[type="email"]', 'admin@orbitos.com')
    await page.fill('input[type="password"]', 'admin123')
    await page.click('button[type="submit"]')
    await page.waitForURL('/app')
  })

  test('should load knowledge base index page', async ({ page }) => {
    await page.goto('/app/knowledge-base')

    // Should show the Knowledge Base header
    await expect(page.locator('h1:has-text("Knowledge Base")')).toBeVisible()

    // Should show categories
    await expect(page.locator('text=Process Mapping')).toBeVisible({ timeout: 10000 })
    await expect(page.locator('text=Roles & Responsibilities')).toBeVisible()
    await expect(page.locator('text=Business Functions')).toBeVisible()
    await expect(page.locator('text=Business Model Canvas')).toBeVisible()
    await expect(page.locator('text=Goals & OKRs')).toBeVisible()
    await expect(page.locator('text=Organization Design')).toBeVisible()
  })

  test('should load article from index page', async ({ page }) => {
    await page.goto('/app/knowledge-base')

    // Wait for categories to load
    await expect(page.locator('text=Role Design Principles')).toBeVisible({ timeout: 10000 })

    // Click on an article
    await page.click('text=Role Design Principles')

    // Should navigate to article page
    await expect(page).toHaveURL(/\/app\/knowledge-base\/roles\/role-design/)

    // Should show article content
    await expect(page.locator('h1:has-text("Role Design Principles")')).toBeVisible({ timeout: 10000 })
    await expect(page.locator('text=What is a Role?')).toBeVisible()
  })

  test('should load roles/role-design article directly', async ({ page }) => {
    await page.goto('/app/knowledge-base/roles/role-design')

    // Should show article title
    await expect(page.locator('h1:has-text("Role Design Principles")')).toBeVisible({ timeout: 10000 })

    // Should show article content
    await expect(page.locator('text=What is a Role?')).toBeVisible()
    await expect(page.locator('text=Responsibility')).toBeVisible()
    await expect(page.locator('text=Authority')).toBeVisible()
    await expect(page.locator('text=Accountability')).toBeVisible()

    // Should show keywords
    await expect(page.locator('text=role design')).toBeVisible()

    // Should show back link
    await expect(page.locator('a:has-text("Knowledge Base")')).toBeVisible()
  })

  test('should load goals/okr-methodology article directly', async ({ page }) => {
    await page.goto('/app/knowledge-base/goals/okr-methodology')

    // Should show article title
    await expect(page.locator('h1:has-text("OKR Methodology")')).toBeVisible({ timeout: 10000 })

    // Should show article content
    await expect(page.locator('text=Objectives and Key Results')).toBeVisible()
  })

  test('should load functions/function-catalog article directly', async ({ page }) => {
    await page.goto('/app/knowledge-base/functions/function-catalog')

    // Should show article title
    await expect(page.locator('h1:has-text("Building a Function Catalog")')).toBeVisible({ timeout: 10000 })
  })

  test('should load canvas/business-model-canvas article directly', async ({ page }) => {
    await page.goto('/app/knowledge-base/canvas/business-model-canvas')

    // Should show article title
    await expect(page.locator('h1:has-text("Business Model Canvas")')).toBeVisible({ timeout: 10000 })
  })

  test('should load org-design/span-of-control article directly', async ({ page }) => {
    await page.goto('/app/knowledge-base/org-design/span-of-control')

    // Should show article title
    await expect(page.locator('h1:has-text("Span of Control")')).toBeVisible({ timeout: 10000 })
  })

  test('should navigate from Roles page guide button', async ({ page }) => {
    await page.goto('/app/roles')

    // Find and click the guide button
    await page.click('a:has-text("Role Design Guide")')

    // Should navigate to article
    await expect(page).toHaveURL(/\/app\/knowledge-base\/roles\/role-design/)
    await expect(page.locator('h1:has-text("Role Design Principles")')).toBeVisible({ timeout: 10000 })
  })

  test('should navigate from Functions page guide button', async ({ page }) => {
    await page.goto('/app/functions')

    // Find and click the guide button
    await page.click('a:has-text("Functions Guide")')

    // Should navigate to article
    await expect(page).toHaveURL(/\/app\/knowledge-base\/functions\/function-catalog/)
    await expect(page.locator('h1:has-text("Building a Function Catalog")')).toBeVisible({ timeout: 10000 })
  })

  test('should navigate from Goals page guide button', async ({ page }) => {
    await page.goto('/app/goals')

    // Find and click the guide button
    await page.click('a:has-text("OKR Guide")')

    // Should navigate to article
    await expect(page).toHaveURL(/\/app\/knowledge-base\/goals\/okr-methodology/)
    await expect(page.locator('h1:has-text("OKR Methodology")')).toBeVisible({ timeout: 10000 })
  })

  test('should navigate from Org Chart page guide button', async ({ page }) => {
    await page.goto('/app/people/org-chart')

    // Find and click the guide button
    await page.click('a:has-text("Org Design Guide")')

    // Should navigate to article
    await expect(page).toHaveURL(/\/app\/knowledge-base\/org-design\/span-of-control/)
    await expect(page.locator('h1:has-text("Span of Control")')).toBeVisible({ timeout: 10000 })
  })

  test('should navigate from Canvases page guide button', async ({ page }) => {
    await page.goto('/app/canvases')

    // Find and click the guide button
    await page.click('a:has-text("Canvas Guide")')

    // Should navigate to article
    await expect(page).toHaveURL(/\/app\/knowledge-base\/canvas\/business-model-canvas/)
    await expect(page.locator('h1:has-text("Business Model Canvas")')).toBeVisible({ timeout: 10000 })
  })

  test('should search knowledge base', async ({ page }) => {
    await page.goto('/app/knowledge-base')

    // Wait for page to load
    await expect(page.locator('h1:has-text("Knowledge Base")')).toBeVisible()

    // Type in search
    await page.fill('input[placeholder*="Search"]', 'OKR')

    // Should filter results
    await expect(page.locator('text=OKR Methodology')).toBeVisible()
  })

  test('should show related articles', async ({ page }) => {
    await page.goto('/app/knowledge-base/roles/role-design')

    // Wait for article to load
    await expect(page.locator('h1:has-text("Role Design Principles")')).toBeVisible({ timeout: 10000 })

    // Scroll to related articles section
    await page.locator('text=Related Articles').scrollIntoViewIfNeeded()

    // Should show related articles
    await expect(page.locator('text=Related Articles')).toBeVisible()
  })

  test('should navigate back to index from article', async ({ page }) => {
    await page.goto('/app/knowledge-base/roles/role-design')

    // Wait for article to load
    await expect(page.locator('h1:has-text("Role Design Principles")')).toBeVisible({ timeout: 10000 })

    // Click back to Knowledge Base
    await page.click('a:has-text("Knowledge Base")')

    // Should be back on index
    await expect(page).toHaveURL('/app/knowledge-base')
    await expect(page.locator('text=Process Mapping')).toBeVisible()
  })

  test('should show error state for non-existent article', async ({ page }) => {
    await page.goto('/app/knowledge-base/nonexistent/article')

    // Should show error state
    await expect(page.locator('text=Article not found')).toBeVisible({ timeout: 10000 })

    // Should show back link
    await expect(page.locator('a:has-text("Back to Knowledge Base")')).toBeVisible()
  })

  test('should access knowledge base from help sidebar', async ({ page }) => {
    await page.goto('/app')

    // Open help sidebar
    await page.click('button:has-text("?")')

    // Should show Knowledge Base link in sidebar
    await expect(page.locator('a:has-text("Knowledge Base")')).toBeVisible()

    // Click on Knowledge Base link
    await page.click('a:has-text("Knowledge Base")')

    // Should navigate to knowledge base
    await expect(page).toHaveURL('/app/knowledge-base')
  })
})
