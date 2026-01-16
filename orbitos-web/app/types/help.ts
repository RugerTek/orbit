/**
 * Help System Types
 * Auto-synced from specs/ directory
 */

export interface HelpArticle {
  id: string
  entityRef?: string       // e.g., "ENT-001" from specs
  featureRef?: string      // e.g., "F001" from specs
  title: string
  summary: string
  content: string          // Markdown content
  relatedConcepts: string[]
  keywords: string[]
  lastSyncedAt: string
}

export interface FeatureGuide {
  id: string
  name: string
  status: string
  guide: string            // Path to markdown file
  walkthrough: string      // Path to walkthrough JSON
}

export interface ConceptDoc {
  id: string
  name: string
  doc: string              // Path to markdown file
}

export interface FieldHelp {
  entity: string
  entityName: string
  field: string
  type: string
  description: string
  required: boolean
  validation: {
    minLength?: number
    maxLength?: number
    pattern?: string
  } | null
  examples: string[]
  helpText: string
}

export interface WalkthroughStep {
  step: number
  title: string
  type: 'highlight' | 'action' | 'completion'
  target?: string
  content: string
  position: 'top' | 'bottom' | 'left' | 'right' | 'center'
}

export interface Walkthrough {
  featureId: string
  featureName: string
  totalSteps: number
  estimatedMinutes: number
  steps: WalkthroughStep[]
}

export interface HelpIndex {
  generated_at: string
  features: FeatureGuide[]
  concepts: ConceptDoc[]
}

export interface HelpSearchResult {
  type: 'feature' | 'concept' | 'field'
  id: string
  title: string
  description: string
  path?: string
  relevance: number
}

export interface ConceptMapNode {
  id: string
  name: string
  type: 'entity' | 'feature'
  connections: string[]
}
