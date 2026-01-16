/**
 * Help System Composable
 *
 * Provides context-aware help functionality:
 * - Spotlight search (Cmd+K)
 * - Field-level tooltips
 * - Feature guides
 * - Interactive walkthroughs
 * - Concept map navigation
 */

import type {
  HelpIndex,
  HelpSearchResult,
  FieldHelp,
  Walkthrough,
  WalkthroughStep
} from '~/types/help'

// Global help state
const isSpotlightOpen = ref(false)
const isHelpPanelOpen = ref(false)
const currentFeatureId = ref<string | null>(null)
const currentWalkthrough = ref<Walkthrough | null>(null)
const currentWalkthroughStep = ref(0)
const helpIndex = ref<HelpIndex | null>(null)
const fieldHelpData = ref<Record<string, FieldHelp>>({})
const isLoading = ref(false)

export function useHelp() {
  const route = useRoute()

  // Load help index on first use
  const loadHelpIndex = async () => {
    if (helpIndex.value) return

    isLoading.value = true
    try {
      // In production, this would be an API call
      // For now, we'll load from static JSON
      const indexResponse = await fetch('/docs/user-manual/index.json')
      if (indexResponse.ok) {
        helpIndex.value = await indexResponse.json()
      }

      const fieldHelpResponse = await fetch('/docs/user-manual/field-help.json')
      if (fieldHelpResponse.ok) {
        fieldHelpData.value = await fieldHelpResponse.json()
      }
    } catch (error) {
      console.warn('Help system: Could not load help index', error)
    } finally {
      isLoading.value = false
    }
  }

  // Toggle spotlight search
  const toggleSpotlight = () => {
    isSpotlightOpen.value = !isSpotlightOpen.value
  }

  const openSpotlight = () => {
    isSpotlightOpen.value = true
  }

  const closeSpotlight = () => {
    isSpotlightOpen.value = false
  }

  // Toggle help panel
  const toggleHelpPanel = () => {
    isHelpPanelOpen.value = !isHelpPanelOpen.value
  }

  const openHelpPanel = (featureId?: string) => {
    if (featureId) {
      currentFeatureId.value = featureId
    }
    isHelpPanelOpen.value = true
  }

  const closeHelpPanel = () => {
    isHelpPanelOpen.value = false
  }

  // Search help content
  const searchHelp = (query: string): HelpSearchResult[] => {
    if (!helpIndex.value || !query.trim()) return []

    const normalizedQuery = query.toLowerCase()
    const results: HelpSearchResult[] = []

    // Search features
    for (const feature of helpIndex.value.features) {
      const nameMatch = feature.name.toLowerCase().includes(normalizedQuery)
      const idMatch = feature.id.toLowerCase().includes(normalizedQuery)

      if (nameMatch || idMatch) {
        results.push({
          type: 'feature',
          id: feature.id,
          title: feature.name,
          description: `Feature guide for ${feature.name}`,
          path: `/help/features/${feature.id}`,
          relevance: nameMatch ? 1 : 0.8
        })
      }
    }

    // Search concepts
    for (const concept of helpIndex.value.concepts) {
      const nameMatch = concept.name.toLowerCase().includes(normalizedQuery)
      const idMatch = concept.id.toLowerCase().includes(normalizedQuery)

      if (nameMatch || idMatch) {
        results.push({
          type: 'concept',
          id: concept.id,
          title: concept.name,
          description: `Learn about ${concept.name}`,
          path: `/help/concepts/${concept.id}`,
          relevance: nameMatch ? 0.9 : 0.7
        })
      }
    }

    // Search field help
    for (const [key, fieldHelp] of Object.entries(fieldHelpData.value)) {
      const fieldMatch = fieldHelp.field.toLowerCase().includes(normalizedQuery)
      const descMatch = fieldHelp.description.toLowerCase().includes(normalizedQuery)

      if (fieldMatch || descMatch) {
        results.push({
          type: 'field',
          id: key,
          title: `${fieldHelp.entityName}: ${fieldHelp.field}`,
          description: fieldHelp.description,
          relevance: fieldMatch ? 0.7 : 0.5
        })
      }
    }

    // Sort by relevance
    return results.sort((a, b) => b.relevance - a.relevance).slice(0, 10)
  }

  // Get field help for a specific field
  const getFieldHelp = (entityId: string, fieldName: string): FieldHelp | null => {
    const key = `${entityId}.${fieldName}`
    return fieldHelpData.value[key] || null
  }

  // Get all field help for an entity
  const getEntityFieldHelp = (entityId: string): FieldHelp[] => {
    return Object.values(fieldHelpData.value).filter(
      (field) => field.entity === entityId
    )
  }

  // Start a walkthrough
  const startWalkthrough = async (featureId: string) => {
    try {
      const response = await fetch(`/docs/user-manual/walkthroughs/${featureId}.json`)
      if (response.ok) {
        currentWalkthrough.value = await response.json()
        currentWalkthroughStep.value = 0
      }
    } catch (error) {
      console.warn('Help system: Could not load walkthrough', error)
    }
  }

  // Navigate walkthrough
  const nextWalkthroughStep = () => {
    if (!currentWalkthrough.value) return

    if (currentWalkthroughStep.value < currentWalkthrough.value.steps.length - 1) {
      currentWalkthroughStep.value++
    } else {
      endWalkthrough()
    }
  }

  const previousWalkthroughStep = () => {
    if (currentWalkthroughStep.value > 0) {
      currentWalkthroughStep.value--
    }
  }

  const endWalkthrough = () => {
    currentWalkthrough.value = null
    currentWalkthroughStep.value = 0
  }

  const getCurrentWalkthroughStep = (): WalkthroughStep | null => {
    if (!currentWalkthrough.value) return null
    return currentWalkthrough.value.steps[currentWalkthroughStep.value] || null
  }

  // Detect current context from route
  const detectCurrentFeature = (): string | null => {
    const path = route.path

    // Map routes to feature IDs
    const routeToFeature: Record<string, string> = {
      '/': 'F001',           // Authentication
      '/app/people': 'F002', // Super Admin / People
      '/app/roles': 'F002',
      '/app/functions': 'F002',
      '/app/processes': 'F003',
      '/app/goals': 'F004',
      '/app/canvas': 'F005'
    }

    for (const [routePattern, featureId] of Object.entries(routeToFeature)) {
      if (path.startsWith(routePattern) || path === routePattern) {
        return featureId
      }
    }

    return null
  }

  // Get contextual help for current page
  const getContextualHelp = () => {
    const featureId = detectCurrentFeature()
    if (!featureId || !helpIndex.value) return null

    return helpIndex.value.features.find((f) => f.id === featureId) || null
  }

  // Keyboard shortcut handler
  const setupKeyboardShortcuts = () => {
    const handleKeydown = (e: KeyboardEvent) => {
      // Cmd+K or Ctrl+K to open spotlight
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        toggleSpotlight()
      }

      // Escape to close
      if (e.key === 'Escape') {
        if (isSpotlightOpen.value) {
          closeSpotlight()
        }
        if (isHelpPanelOpen.value) {
          closeHelpPanel()
        }
        if (currentWalkthrough.value) {
          endWalkthrough()
        }
      }

      // Arrow keys for walkthrough navigation
      if (currentWalkthrough.value) {
        if (e.key === 'ArrowRight') {
          nextWalkthroughStep()
        }
        if (e.key === 'ArrowLeft') {
          previousWalkthroughStep()
        }
      }
    }

    onMounted(() => {
      window.addEventListener('keydown', handleKeydown)
      loadHelpIndex()
    })

    onUnmounted(() => {
      window.removeEventListener('keydown', handleKeydown)
    })
  }

  return {
    // State
    isSpotlightOpen: readonly(isSpotlightOpen),
    isHelpPanelOpen: readonly(isHelpPanelOpen),
    currentFeatureId: readonly(currentFeatureId),
    currentWalkthrough: readonly(currentWalkthrough),
    currentWalkthroughStep: readonly(currentWalkthroughStep),
    helpIndex: readonly(helpIndex),
    isLoading: readonly(isLoading),

    // Spotlight
    toggleSpotlight,
    openSpotlight,
    closeSpotlight,

    // Help Panel
    toggleHelpPanel,
    openHelpPanel,
    closeHelpPanel,

    // Search
    searchHelp,

    // Field Help
    getFieldHelp,
    getEntityFieldHelp,

    // Walkthroughs
    startWalkthrough,
    nextWalkthroughStep,
    previousWalkthroughStep,
    endWalkthrough,
    getCurrentWalkthroughStep,

    // Context
    detectCurrentFeature,
    getContextualHelp,

    // Setup
    setupKeyboardShortcuts,
    loadHelpIndex
  }
}
