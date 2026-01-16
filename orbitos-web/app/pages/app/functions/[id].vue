<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const route = useRoute()
const functionId = route.params.id as string

// Mock data - would come from API
const functionsData: Record<string, {
  id: string
  name: string
  purpose: string
  description: string
  category: string
  ownerRole: string
  complexity: 'simple' | 'moderate' | 'complex'
  estimatedDuration: string
  requiresApproval: boolean
  status: string
  coverage: string
  instructions: string
  toolsRequired: Array<{ id: string; name: string; type: string }>
  capablePeople: Array<{ id: string; name: string; role: string; isPrimary: boolean }>
  usedInProcesses: Array<{ id: string; name: string; activityName: string }>
}> = {
  'fn-1': {
    id: 'fn-1',
    name: 'Handle Inbound Request',
    purpose: 'Ensure customer inquiries are captured accurately for fast response',
    description: 'Receive and log incoming customer requests from various channels (email, phone, portal) and route to appropriate team member.',
    category: 'Sales',
    ownerRole: 'Sales Rep',
    complexity: 'simple',
    estimatedDuration: '15 min',
    requiresApproval: false,
    status: 'Covered',
    coverage: '3 people',
    instructions: '1. Check incoming request queue\n2. Log request in CRM with customer details\n3. Categorize by urgency and type\n4. Route to appropriate team member or self-assign',
    toolsRequired: [
      { id: 'tool-1', name: 'HubSpot CRM', type: 'Tool' },
      { id: 'tool-2', name: 'Email System', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-1', name: 'Maya Torres', role: 'Sales Lead', isPrimary: true },
      { id: 'person-2', name: 'Jake Wilson', role: 'Sales Rep', isPrimary: false },
      { id: 'person-3', name: 'Sofia Martinez', role: 'Sales Rep', isPrimary: false }
    ],
    usedInProcesses: [
      { id: 'quote-to-cash', name: 'Quote to Cash', activityName: 'Receive Quote Request' }
    ]
  },
  'fn-2': {
    id: 'fn-2',
    name: 'Draft Proposal',
    purpose: 'Create compelling proposals that accurately reflect customer needs',
    description: 'Prepare a detailed proposal document including pricing, scope, timeline, and terms based on customer requirements.',
    category: 'Sales',
    ownerRole: 'Sales Rep',
    complexity: 'moderate',
    estimatedDuration: '30 min',
    requiresApproval: false,
    status: 'Covered',
    coverage: '2 people',
    instructions: '1. Review customer requirements\n2. Select appropriate pricing template\n3. Customize scope and deliverables\n4. Add terms and conditions\n5. Generate PDF for review',
    toolsRequired: [
      { id: 'tool-1', name: 'HubSpot CRM', type: 'Tool' },
      { id: 'tool-3', name: 'PandaDoc', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-1', name: 'Maya Torres', role: 'Sales Lead', isPrimary: true },
      { id: 'person-2', name: 'Jake Wilson', role: 'Sales Rep', isPrimary: false }
    ],
    usedInProcesses: [
      { id: 'quote-to-cash', name: 'Quote to Cash', activityName: 'Draft Proposal' }
    ]
  },
  'fn-3': {
    id: 'fn-3',
    name: 'Approve Discount',
    purpose: 'Protect margins while empowering sales to close strategic deals',
    description: 'Review and approve discount requests exceeding standard thresholds, ensuring alignment with pricing strategy and margin targets.',
    category: 'Finance',
    ownerRole: 'Finance Partner',
    complexity: 'moderate',
    estimatedDuration: '10 min',
    requiresApproval: true,
    status: 'SPOF',
    coverage: '1 person',
    instructions: '1. Review discount request details\n2. Check customer account history\n3. Verify margin impact\n4. Approve, reject, or suggest alternative\n5. Document decision rationale',
    toolsRequired: [
      { id: 'tool-1', name: 'HubSpot CRM', type: 'Tool' },
      { id: 'tool-4', name: 'NetSuite', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-4', name: 'Jordan Lee', role: 'Finance Partner', isPrimary: true }
    ],
    usedInProcesses: [
      { id: 'quote-to-cash', name: 'Quote to Cash', activityName: 'Validate Pricing' },
      { id: 'renewals', name: 'Renewals', activityName: 'Negotiate Terms' }
    ]
  },
  'fn-4': {
    id: 'fn-4',
    name: 'Generate Invoice',
    purpose: 'Ensure accurate billing to maintain cash flow and customer trust',
    description: 'Create and send invoice to customer based on agreed terms and pricing.',
    category: 'Finance',
    ownerRole: 'Finance Partner',
    complexity: 'simple',
    estimatedDuration: '15 min',
    requiresApproval: false,
    status: 'Covered',
    coverage: '2 people',
    instructions: '1. Verify order details and pricing\n2. Generate invoice in billing system\n3. Send to customer via email\n4. Log in CRM',
    toolsRequired: [
      { id: 'tool-4', name: 'NetSuite', type: 'Tool' },
      { id: 'tool-5', name: 'Stripe', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-4', name: 'Jordan Lee', role: 'Finance Partner', isPrimary: true },
      { id: 'person-5', name: 'Alex Chen', role: 'Finance Analyst', isPrimary: false }
    ],
    usedInProcesses: [
      { id: 'quote-to-cash', name: 'Quote to Cash', activityName: 'Generate Invoice' }
    ]
  },
  'fn-5': {
    id: 'fn-5',
    name: 'Conduct Kickoff',
    purpose: 'Set clear expectations and build trust from day one',
    description: 'Lead initial customer meeting to establish goals, timeline, and working relationship.',
    category: 'Customer Success',
    ownerRole: 'CS Manager',
    complexity: 'moderate',
    estimatedDuration: '45 min',
    requiresApproval: false,
    status: 'Covered',
    coverage: '2 people',
    instructions: '1. Prepare kickoff deck with customer info\n2. Schedule meeting with key stakeholders\n3. Run meeting covering goals and timeline\n4. Document action items and next steps',
    toolsRequired: [
      { id: 'tool-6', name: 'Zoom', type: 'Tool' },
      { id: 'tool-7', name: 'Notion', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-6', name: 'Felix Nguyen', role: 'Ops Manager', isPrimary: true },
      { id: 'person-7', name: 'Alina Chen', role: 'CS Manager', isPrimary: false }
    ],
    usedInProcesses: [
      { id: 'onboarding', name: 'Customer Onboarding', activityName: 'Kickoff Call' }
    ]
  },
  'fn-6': {
    id: 'fn-6',
    name: 'Deliver Training',
    purpose: 'Enable customers to get value from the platform quickly',
    description: 'Conduct training sessions for customer users on platform features and best practices.',
    category: 'Enablement',
    ownerRole: 'Enablement',
    complexity: 'complex',
    estimatedDuration: '2 hrs',
    requiresApproval: false,
    status: 'At Risk',
    coverage: '1 person',
    instructions: '1. Customize training content for customer use case\n2. Schedule and send calendar invites\n3. Deliver live training session\n4. Share recording and resources\n5. Follow up with Q&A',
    toolsRequired: [
      { id: 'tool-6', name: 'Zoom', type: 'Tool' },
      { id: 'tool-8', name: 'Loom', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-8', name: 'Alina Chen', role: 'Enablement', isPrimary: true }
    ],
    usedInProcesses: [
      { id: 'onboarding', name: 'Customer Onboarding', activityName: 'Training Session' }
    ]
  },
  'fn-7': {
    id: 'fn-7',
    name: 'Plan Account Strategy',
    purpose: 'Proactively retain and grow customer relationships',
    description: 'Develop engagement plan for customer accounts based on health score and renewal timeline.',
    category: 'Customer Success',
    ownerRole: 'CSM',
    complexity: 'moderate',
    estimatedDuration: '30 min',
    requiresApproval: false,
    status: 'Covered',
    coverage: '2 people',
    instructions: '1. Review account health score and usage\n2. Identify risks and opportunities\n3. Plan touchpoint cadence\n4. Document strategy in CRM',
    toolsRequired: [
      { id: 'tool-1', name: 'HubSpot CRM', type: 'Tool' },
      { id: 'tool-9', name: 'Gainsight', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-9', name: 'Sarah Kim', role: 'CSM', isPrimary: true },
      { id: 'person-10', name: 'Mike Brown', role: 'CSM', isPrimary: false }
    ],
    usedInProcesses: [
      { id: 'renewals', name: 'Renewals', activityName: 'Plan Touchpoints' }
    ]
  },
  'fn-8': {
    id: 'fn-8',
    name: 'Close Renewal',
    purpose: 'Secure continued partnership and revenue predictability',
    description: 'Finalize renewal agreement with customer, handling any negotiation and contract execution.',
    category: 'Customer Success',
    ownerRole: 'CSM',
    complexity: 'moderate',
    estimatedDuration: '30 min',
    requiresApproval: false,
    status: 'Covered',
    coverage: '2 people',
    instructions: '1. Present renewal proposal\n2. Address customer concerns\n3. Finalize terms and pricing\n4. Execute contract\n5. Update CRM with renewal details',
    toolsRequired: [
      { id: 'tool-1', name: 'HubSpot CRM', type: 'Tool' },
      { id: 'tool-3', name: 'PandaDoc', type: 'Tool' }
    ],
    capablePeople: [
      { id: 'person-9', name: 'Sarah Kim', role: 'CSM', isPrimary: true },
      { id: 'person-10', name: 'Mike Brown', role: 'CSM', isPrimary: false }
    ],
    usedInProcesses: [
      { id: 'renewals', name: 'Renewals', activityName: 'Close Renewal' }
    ]
  }
}

const func = computed(() => functionsData[functionId] || functionsData['fn-1'])

const getComplexityColor = (complexity: string) => {
  switch (complexity) {
    case 'simple': return 'bg-emerald-500/20 text-emerald-300'
    case 'moderate': return 'bg-blue-500/20 text-blue-300'
    case 'complex': return 'bg-purple-500/20 text-purple-300'
    default: return 'bg-slate-500/20 text-slate-300'
  }
}

const getStatusColor = (status: string) => {
  switch (status) {
    case 'Covered': return 'bg-emerald-500/20 text-emerald-300'
    case 'SPOF': return 'bg-red-500/20 text-red-300'
    case 'At Risk': return 'bg-amber-500/20 text-amber-300'
    default: return 'bg-slate-500/20 text-slate-300'
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
      <div class="flex items-start gap-4">
        <NuxtLink
          to="/app/functions"
          class="mt-1 flex items-center justify-center h-10 w-10 rounded-lg border border-slate-700 bg-slate-800/60 text-slate-400 hover:text-white hover:border-slate-600 transition-colors"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </NuxtLink>
        <div>
          <div class="flex items-center gap-3">
            <h1 class="text-2xl font-bold text-white">{{ func.name }}</h1>
            <span :class="['rounded-full px-3 py-1 text-xs font-medium', getStatusColor(func.status)]">
              {{ func.status }}
            </span>
          </div>
          <p class="mt-1 text-slate-400">{{ func.purpose }}</p>
        </div>
      </div>
      <button class="rounded-xl bg-gradient-to-r from-purple-500 to-blue-600 px-4 py-2 text-sm font-semibold text-white">
        Edit Function
      </button>
    </div>

    <div class="grid gap-6 lg:grid-cols-3">
      <!-- Main Content -->
      <div class="lg:col-span-2 space-y-6">
        <!-- Overview Card -->
        <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
          <h2 class="text-lg font-semibold text-white mb-4">Overview</h2>
          <p class="text-slate-300 mb-4">{{ func.description }}</p>

          <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <div>
              <div class="text-xs uppercase text-slate-500">Category</div>
              <div class="mt-1 text-white">{{ func.category }}</div>
            </div>
            <div>
              <div class="text-xs uppercase text-slate-500">Owner Role</div>
              <div class="mt-1 text-white">{{ func.ownerRole }}</div>
            </div>
            <div>
              <div class="text-xs uppercase text-slate-500">Complexity</div>
              <span :class="['mt-1 inline-block rounded-full px-2 py-0.5 text-xs', getComplexityColor(func.complexity)]">
                {{ func.complexity }}
              </span>
            </div>
            <div>
              <div class="text-xs uppercase text-slate-500">Est. Duration</div>
              <div class="mt-1 text-white">{{ func.estimatedDuration }}</div>
            </div>
          </div>

          <div v-if="func.requiresApproval" class="mt-4 flex items-center gap-2 rounded-lg bg-amber-500/10 border border-amber-500/20 px-3 py-2">
            <svg class="h-4 w-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span class="text-sm text-amber-300">This function requires approval</span>
          </div>
        </div>

        <!-- Instructions -->
        <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
          <h2 class="text-lg font-semibold text-white mb-4">Instructions (SOP)</h2>
          <div class="prose prose-invert prose-sm max-w-none">
            <pre class="whitespace-pre-wrap text-slate-300 text-sm font-sans bg-slate-900/50 rounded-lg p-4">{{ func.instructions }}</pre>
          </div>
        </div>

        <!-- Used in Processes -->
        <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
          <h2 class="text-lg font-semibold text-white mb-4">Used in Processes</h2>
          <div class="space-y-3">
            <NuxtLink
              v-for="process in func.usedInProcesses"
              :key="process.id"
              :to="`/app/processes/${process.id}`"
              class="flex items-center justify-between rounded-xl border border-slate-700 bg-slate-900/40 p-4 transition hover:border-purple-500/40"
            >
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-purple-500/20">
                  <svg class="h-5 w-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                </div>
                <div>
                  <div class="font-medium text-white">{{ process.name }}</div>
                  <div class="text-sm text-slate-400">as "{{ process.activityName }}"</div>
                </div>
              </div>
              <svg class="h-5 w-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </NuxtLink>
          </div>
        </div>
      </div>

      <!-- Sidebar -->
      <div class="space-y-6">
        <!-- Capable People -->
        <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-lg font-semibold text-white">Capable People</h2>
            <span class="text-sm text-slate-400">{{ func.capablePeople.length }}</span>
          </div>

          <div v-if="func.status === 'SPOF'" class="mb-4 rounded-lg bg-red-500/10 border border-red-500/20 p-3">
            <div class="flex items-center gap-2 text-red-300 text-sm">
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <span>Single point of failure - add backup</span>
            </div>
          </div>

          <div class="space-y-3">
            <div
              v-for="person in func.capablePeople"
              :key="person.id"
              class="flex items-center gap-3"
            >
              <div class="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white">
                {{ person.name.split(' ').map(n => n[0]).join('') }}
              </div>
              <div class="flex-1">
                <div class="flex items-center gap-2">
                  <span class="text-white">{{ person.name }}</span>
                  <span v-if="person.isPrimary" class="rounded bg-purple-500/20 px-1.5 py-0.5 text-xs text-purple-300">Primary</span>
                </div>
                <div class="text-sm text-slate-400">{{ person.role }}</div>
              </div>
            </div>
          </div>

          <button class="mt-4 w-full rounded-lg border border-dashed border-slate-600 py-2 text-sm text-slate-400 hover:border-slate-500 hover:text-slate-300 transition-colors">
            + Add capable person
          </button>
        </div>

        <!-- Tools Required -->
        <div class="rounded-2xl border border-slate-700 bg-slate-800/60 p-6">
          <h2 class="text-lg font-semibold text-white mb-4">Tools Required</h2>
          <div class="space-y-2">
            <div
              v-for="tool in func.toolsRequired"
              :key="tool.id"
              class="flex items-center gap-3 rounded-lg bg-slate-900/40 p-3"
            >
              <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-500/20">
                <svg class="h-4 w-4 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z" />
                </svg>
              </div>
              <div>
                <div class="text-sm text-white">{{ tool.name }}</div>
                <div class="text-xs text-slate-400">{{ tool.type }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- AI Insights -->
        <div class="rounded-2xl border border-purple-500/30 bg-purple-500/10 p-6">
          <div class="flex items-center gap-2 mb-3">
            <svg class="h-5 w-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
            </svg>
            <span class="font-medium text-purple-300">AI Insights</span>
          </div>
          <p class="text-sm text-slate-300 mb-3">
            {{ func.status === 'SPOF'
              ? 'This function is a single point of failure. Training a backup person could reduce risk.'
              : func.status === 'At Risk'
                ? 'Low coverage detected. Consider cross-training team members.'
                : 'Coverage looks healthy. Consider documenting edge cases for knowledge sharing.'
            }}
          </p>
          <button class="text-sm text-purple-400 hover:text-purple-300 flex items-center gap-1">
            Get recommendations
            <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
