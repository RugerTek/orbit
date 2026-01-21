<script setup lang="ts">
/**
 * HelpSidebar - Persistent Right Sidebar with Contextual Help
 *
 * Shows help content relevant to the current page
 * Always accessible, can be collapsed
 */

const route = useRoute()
const { loadHelpIndex } = useHelp()

// Sidebar state - persisted in localStorage
const isCollapsed = ref(true)
const activeSection = ref<'overview' | 'fields' | 'actions' | 'tips'>('overview')

// Load collapsed state from localStorage
onMounted(() => {
  loadHelpIndex()
  const stored = localStorage.getItem('helpSidebarCollapsed')
  if (stored !== null) {
    isCollapsed.value = stored === 'true'
  }
})

// Save collapsed state
watch(isCollapsed, (val) => {
  localStorage.setItem('helpSidebarCollapsed', String(val))
})

// Help content type definition
interface HelpContent {
  title: string
  description: string
  icon: string
  sections: {
    overview: string
    fields?: { name: string; description: string; tip?: string }[]
    actions?: { name: string; description: string; shortcut?: string }[]
    tips?: string[]
    relatedPages?: { name: string; path: string }[]
  }
}

// Default help content
const defaultHelp: HelpContent = {
  title: 'Help',
  description: 'Contextual documentation',
  icon: 'M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
  sections: {
    overview: 'Select a page from the navigation to see contextual help.',
    tips: ['Use the sidebar navigation to explore features', 'Press Cmd+K to search']
  }
}

// Contextual help content based on current route
const contextualHelp = computed((): HelpContent => {
  const path = route.path

  // Define help content for each page
  const helpContent: Record<string, HelpContent> = {
    '/app': {
      title: 'Dashboard Overview',
      description: 'Your organization at a glance',
      icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6',
      sections: {
        overview: 'The dashboard shows key metrics about your organization: people count, roles, functions, and processes. Use it to quickly assess organizational health and identify areas needing attention.',
        tips: [
          'Check the "Focus Areas" section for AI-recommended priorities',
          'The health indicators show coverage gaps and risks',
          'Click any stat card to navigate to that section'
        ],
        relatedPages: [
          { name: 'People', path: '/app/people' },
          { name: 'Functions', path: '/app/functions' },
          { name: 'Processes', path: '/app/processes' }
        ]
      }
    },
    '/app/people': {
      title: 'People Management',
      description: 'Manage your team members and their capacity',
      icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z',
      sections: {
        overview: 'People are the resources in your organization of type "Person". Each person can be assigned to Roles, which determines what Functions they can perform. Track capacity allocation to prevent overload.',
        fields: [
          { name: 'Name', description: 'Full name of the team member', tip: 'Use consistent naming conventions' },
          { name: 'Email', description: 'Contact email for the person', tip: 'Used for notifications and integration' },
          { name: 'Role', description: 'Organizational role (linked to Roles entity)', tip: 'Select from existing roles or create new ones' },
          { name: 'Allocation', description: 'Percentage of capacity assigned across roles', tip: 'Over 100% indicates overload' },
          { name: 'Status', description: 'Available, Stable, Near Capacity, or Overloaded' }
        ],
        actions: [
          { name: 'Add Person', description: 'Create a new team member', shortcut: 'Click "+ Add Person"' },
          { name: 'Edit Person', description: 'Update name, email, or role', shortcut: 'Click pencil icon' },
          { name: 'View Details', description: 'See full profile with capabilities', shortcut: 'Click on row' }
        ],
        tips: [
          'Assign roles to people to track their responsibilities',
          'Monitor allocation to prevent burnout',
          'Link people to Functions to track skill coverage',
          'Use the Org Chart view for hierarchy visualization'
        ],
        relatedPages: [
          { name: 'Org Chart', path: '/app/people/org-chart' },
          { name: 'Roles', path: '/app/roles' },
          { name: 'Functions', path: '/app/functions' }
        ]
      }
    },
    '/app/people/org-chart': {
      title: 'Organization Chart',
      description: 'Visualize reporting relationships',
      icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4',
      sections: {
        overview: 'The Org Chart shows the hierarchical structure of your organization. View in Tree, List, or Cards mode. Manage reporting relationships and identify vacancies.',
        actions: [
          { name: 'Set Reports To', description: 'Assign who a person reports to' },
          { name: 'Create Vacancy', description: 'Add an open position to fill' },
          { name: 'Fill Vacancy', description: 'Assign a person to an open position' },
          { name: 'Switch View', description: 'Toggle between Tree/List/Cards' }
        ],
        tips: [
          'Drag and drop in Tree view to reorder',
          'Vacancies show as dashed boxes',
          'Metrics show org depth and span of control',
          'Click a node to select and edit'
        ]
      }
    },
    '/app/roles': {
      title: 'Roles Management',
      description: 'Define organizational roles and their functions',
      icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
      sections: {
        overview: 'Roles are named positions in your organization (e.g., "COO", "Senior Developer"). Each role can have multiple Functions assigned to it. People are assigned to Roles to define their responsibilities.',
        fields: [
          { name: 'Name', description: 'Title of the role', tip: 'Use standard job titles when possible' },
          { name: 'Department', description: 'Which department this role belongs to' },
          { name: 'Purpose', description: 'What this role is responsible for' },
          { name: 'Coverage', description: 'Covered, At Risk, or Uncovered status' }
        ],
        actions: [
          { name: 'Add Role', description: 'Create a new organizational role' },
          { name: 'Assign Functions', description: 'Link functions to this role' },
          { name: 'View Details', description: 'See assigned functions and people' }
        ],
        tips: [
          'A role with no people assigned is "Uncovered"',
          'Assign Functions to roles to define responsibilities',
          'Coverage status helps identify hiring needs'
        ],
        relatedPages: [
          { name: 'Functions', path: '/app/functions' },
          { name: 'People', path: '/app/people' }
        ]
      }
    },
    '/app/functions': {
      title: 'Functions Catalog',
      description: 'Atomic work units your organization performs',
      icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4',
      sections: {
        overview: 'Functions are the smallest units of work in your organization. Examples: "Process Payroll", "Review Code", "Handle Customer Inquiry". Functions are assigned to Roles and linked to Process activities.',
        fields: [
          { name: 'Name', description: 'Action-oriented name', tip: 'Start with a verb: "Process...", "Review...", "Create..."' },
          { name: 'Purpose', description: 'Why this function exists' },
          { name: 'Category', description: 'Grouping (e.g., Finance, Engineering, Sales)' },
          { name: 'Coverage Status', description: 'SPOF, At Risk, Covered, or Uncovered' }
        ],
        actions: [
          { name: 'Add Function', description: 'Create a new function' },
          { name: 'Add Capable Person', description: 'Assign someone who can perform this' },
          { name: 'View Details', description: 'See people, roles, and processes' }
        ],
        tips: [
          'SPOF (Single Point of Failure) = only 1 person can do it',
          'At Risk = only 2 people, consider cross-training',
          'Link functions to roles for proper assignment',
          'Functions appear in Process activities'
        ],
        relatedPages: [
          { name: 'Roles', path: '/app/roles' },
          { name: 'Processes', path: '/app/processes' },
          { name: 'People', path: '/app/people' }
        ]
      }
    },
    '/app/processes': {
      title: 'Process Management',
      description: 'Define how work gets done',
      icon: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15',
      sections: {
        overview: 'Processes are sequences of Activities that describe how work flows through your organization. Each process has a trigger (what starts it) and an output (what it produces).',
        fields: [
          { name: 'Name', description: 'Descriptive process name' },
          { name: 'Purpose', description: 'Why this process exists' },
          { name: 'Trigger', description: 'What event starts this process' },
          { name: 'Output', description: 'What the process produces' },
          { name: 'Status', description: 'Draft, Active, or Deprecated' }
        ],
        actions: [
          { name: 'Add Process', description: 'Create a new process' },
          { name: 'Open Editor', description: 'Design the process flow visually' },
          { name: 'Add Activities', description: 'Define steps in the process' }
        ],
        tips: [
          'Start with high-level processes, then detail activities',
          'Link activities to Functions for coverage tracking',
          'Use the visual editor for complex flows',
          'Decision nodes create branching paths'
        ],
        relatedPages: [
          { name: 'Functions', path: '/app/functions' },
          { name: 'Resources', path: '/app/resources' }
        ]
      }
    },
    '/app/resources': {
      title: 'Resources Registry',
      description: 'All organizational resources',
      icon: 'M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z',
      sections: {
        overview: 'Resources include People, Teams, Tools, Automations, Partners, and Assets. This is the master registry of everything your organization uses to operate.',
        fields: [
          { name: 'Type', description: 'Person, Team, Tool, Automation, Partner, Asset' },
          { name: 'Name', description: 'Resource identifier' },
          { name: 'Status', description: 'Active, Inactive, Planned, or Archived' },
          { name: 'Subtype', description: 'Specific categorization within type' }
        ],
        tips: [
          'People are managed in the People section',
          'Tools include software and systems',
          'Partners are external vendors or collaborators',
          'Assets are physical equipment or property'
        ]
      }
    },
    '/app/goals': {
      title: 'Goals & OKRs',
      description: 'Track objectives and key results',
      icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
      sections: {
        overview: 'Goals are Objectives with measurable Key Results. Set targets, track progress, and align your team around what matters most.',
        fields: [
          { name: 'Name', description: 'Clear, inspiring objective' },
          { name: 'Type', description: 'Objective or Key Result' },
          { name: 'Target', description: 'Numeric target value' },
          { name: 'Current', description: 'Current progress value' },
          { name: 'Owner', description: 'Person responsible' }
        ],
        tips: [
          'Objectives should be qualitative and inspiring',
          'Key Results should be measurable',
          'Link goals to processes they depend on',
          'Review and update progress regularly'
        ]
      }
    },
    '/app/ai-agents': {
      title: 'AI Agents',
      description: 'Configure AI assistants for your organization',
      icon: 'M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
      sections: {
        overview: 'AI Agents are specialized assistants you can configure for different purposes: strategy advice, financial analysis, operations optimization, and more. Create conversations with multiple agents.',
        fields: [
          { name: 'Name', description: 'Agent identifier' },
          { name: 'Model', description: 'AI model (Claude Sonnet, Opus, Haiku)' },
          { name: 'System Prompt', description: 'Instructions that define agent behavior' },
          { name: 'Temperature', description: 'Creativity level (0=focused, 1=creative)' }
        ],
        actions: [
          { name: 'Create Agent', description: 'Define a new AI assistant' },
          { name: 'Start Conversation', description: 'Create a group chat with agents' },
          { name: 'Use Templates', description: 'Pre-built prompts for common roles' }
        ],
        tips: [
          'Use templates for quick setup (CFO, Strategy, HR)',
          'Agents have access to your organization data',
          '@mention agents in conversations to invoke them',
          'Review AI suggestions before applying changes'
        ]
      }
    },
    '/app/canvases': {
      title: 'Business Model Canvas',
      description: 'Strategic planning and visualization',
      icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z',
      sections: {
        overview: 'The Business Model Canvas is a strategic tool with 9 blocks: Key Partners, Key Activities, Key Resources, Value Propositions, Customer Relationships, Channels, Customer Segments, Cost Structure, and Revenue Streams.',
        tips: [
          'Create canvases for different products or initiatives',
          'Link canvas items to actual entities (partners, processes)',
          'Use three views: Canvas, Kanban, or List',
          'Canvas scope can be Organization, Product, or Segment'
        ]
      }
    },
    '/app/assignments': {
      title: 'Pending Actions',
      description: 'Review AI-proposed changes',
      icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4',
      sections: {
        overview: 'When AI agents suggest changes to your organization (creating entities, updating data), they appear here for your review. Approve or reject each action to maintain control.',
        actions: [
          { name: 'Approve', description: 'Apply the suggested change' },
          { name: 'Reject', description: 'Dismiss the suggestion' },
          { name: 'View Details', description: 'See what will change' }
        ],
        tips: [
          'Review AI suggestions carefully before approving',
          'Rejected actions are logged for audit',
          'Batch approve similar actions to save time'
        ]
      }
    }
  }

  // Find the most specific match
  const exactMatch = helpContent[path]
  if (exactMatch) return exactMatch

  // Try prefix matches for dynamic routes
  for (const [pattern, content] of Object.entries(helpContent)) {
    if (path.startsWith(pattern) && pattern !== '/app') {
      return content
    }
  }

  // Default to dashboard or fallback
  return helpContent['/app'] || defaultHelp
})

// Toggle sidebar
const toggleSidebar = () => {
  isCollapsed.value = !isCollapsed.value
}
</script>

<template>
  <div>
    <!-- Always-visible toggle button on right edge - prominent purple gradient -->
    <Teleport to="body">
      <button
        @click="toggleSidebar"
        class="fixed right-0 top-1/3 z-[9999] rounded-l-xl border-2 border-r-0 border-purple-400 bg-gradient-to-r from-purple-600 to-blue-600 px-4 py-5 shadow-2xl shadow-purple-500/50 text-white hover:from-purple-500 hover:to-blue-500 hover:shadow-purple-500/70 transition-all duration-200"
        title="Help & Documentation (?)"
      >
        <span class="text-lg font-bold">?</span>
      </button>
    </Teleport>

    <!-- Sidebar panel with slide animation -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition-transform duration-300 ease-out"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition-transform duration-200 ease-in"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div
          v-if="!isCollapsed"
          class="fixed right-0 top-16 bottom-0 z-[9998] w-80 border-l border-white/10 bg-slate-800/95 backdrop-blur-xl overflow-hidden flex flex-col"
        >
        <!-- Header -->
        <div class="border-b border-white/10 bg-gradient-to-r from-purple-500/10 to-blue-500/10 px-4 py-3">
          <div class="flex items-center gap-3">
            <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-purple-500 to-blue-600">
              <svg class="h-4 w-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path :d="contextualHelp.icon" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="text-sm font-semibold text-white truncate">{{ contextualHelp.title }}</h3>
              <p class="text-xs text-slate-400 truncate">{{ contextualHelp.description }}</p>
            </div>
            <button
              @click="toggleSidebar"
              class="rounded-lg p-1.5 text-slate-400 hover:bg-slate-700 hover:text-white transition-colors"
              title="Close help sidebar"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <!-- Section tabs -->
        <div class="flex border-b border-white/10 bg-slate-800/50">
          <button
            v-for="section in ['overview', 'fields', 'actions', 'tips'] as const"
            :key="section"
            :class="[
              'flex-1 px-2 py-2 text-xs font-medium transition-colors',
              activeSection === section
                ? 'border-b-2 border-purple-500 text-purple-300'
                : 'text-slate-400 hover:text-white'
            ]"
            @click="activeSection = section"
          >
            {{ section.charAt(0).toUpperCase() + section.slice(1) }}
          </button>
        </div>

        <!-- Content -->
        <div class="flex-1 overflow-y-auto p-4">
          <!-- Overview -->
          <div v-if="activeSection === 'overview'">
            <p class="text-sm text-slate-300 leading-relaxed">
              {{ contextualHelp.sections.overview }}
            </p>

            <!-- Related Pages -->
            <div v-if="contextualHelp.sections.relatedPages?.length" class="mt-6">
              <h4 class="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">Related Pages</h4>
              <div class="space-y-1">
                <NuxtLink
                  v-for="page in contextualHelp.sections.relatedPages"
                  :key="page.path"
                  :to="page.path"
                  class="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-300 hover:bg-white/5 hover:text-white transition-colors"
                >
                  <svg class="w-4 h-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                  </svg>
                  {{ page.name }}
                </NuxtLink>
              </div>
            </div>
          </div>

          <!-- Fields -->
          <div v-else-if="activeSection === 'fields'">
            <div v-if="contextualHelp.sections.fields?.length" class="space-y-4">
              <div
                v-for="field in contextualHelp.sections.fields"
                :key="field.name"
                class="rounded-lg bg-white/5 p-3"
              >
                <h4 class="text-sm font-medium text-white">{{ field.name }}</h4>
                <p class="mt-1 text-xs text-slate-400">{{ field.description }}</p>
                <p v-if="field.tip" class="mt-2 text-xs text-purple-300 flex items-start gap-1">
                  <svg class="w-3 h-3 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  {{ field.tip }}
                </p>
              </div>
            </div>
            <p v-else class="text-sm text-slate-400">No field documentation for this page.</p>
          </div>

          <!-- Actions -->
          <div v-else-if="activeSection === 'actions'">
            <div v-if="contextualHelp.sections.actions?.length" class="space-y-3">
              <div
                v-for="action in contextualHelp.sections.actions"
                :key="action.name"
                class="flex items-start gap-3 rounded-lg bg-white/5 p-3"
              >
                <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-500/20 text-emerald-400 flex-shrink-0">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
                  </svg>
                </div>
                <div class="flex-1 min-w-0">
                  <h4 class="text-sm font-medium text-white">{{ action.name }}</h4>
                  <p class="mt-0.5 text-xs text-slate-400">{{ action.description }}</p>
                  <p v-if="action.shortcut" class="mt-1 text-xs text-slate-500">{{ action.shortcut }}</p>
                </div>
              </div>
            </div>
            <p v-else class="text-sm text-slate-400">No actions documented for this page.</p>
          </div>

          <!-- Tips -->
          <div v-else-if="activeSection === 'tips'">
            <div v-if="contextualHelp.sections.tips?.length" class="space-y-3">
              <div
                v-for="(tip, index) in contextualHelp.sections.tips"
                :key="index"
                class="flex items-start gap-3"
              >
                <div class="flex h-6 w-6 items-center justify-center rounded-full bg-amber-500/20 text-amber-400 flex-shrink-0 text-xs font-bold">
                  {{ index + 1 }}
                </div>
                <p class="text-sm text-slate-300 pt-0.5">{{ tip }}</p>
              </div>
            </div>
            <p v-else class="text-sm text-slate-400">No tips for this page yet.</p>
          </div>
        </div>

        <!-- Footer -->
        <div class="border-t border-white/10 px-4 py-3 bg-slate-800/80">
          <div class="flex items-center justify-between text-xs text-slate-500">
            <span>Press <kbd class="rounded border border-slate-600 bg-slate-700/50 px-1.5 py-0.5">Cmd+K</kbd> to search</span>
            <span class="text-slate-600">Context-aware help</span>
          </div>
        </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
