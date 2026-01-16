<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const activeCanvas = ref<'bmc' | 'lean' | 'value_prop'>('bmc')
const aiInterviewMode = ref(false)
const aiMessages = ref<Array<{ role: 'ai' | 'user'; content: string }>>([
  { role: 'ai', content: "Let's build your Business Model Canvas together. I'll ask you questions to help fill in each section. Ready to start with your Value Propositions?" }
])
const userInput = ref('')

const bmcSections = ref({
  keyPartners: {
    title: 'Key Partners',
    items: [
      { id: '1', content: 'Cloud infrastructure providers (AWS, GCP)', color: 'blue' },
      { id: '2', content: 'Implementation partners', color: 'blue' },
    ]
  },
  keyActivities: {
    title: 'Key Activities',
    items: [
      { id: '3', content: 'Platform development & maintenance', color: 'purple' },
      { id: '4', content: 'Customer onboarding & training', color: 'purple' },
      { id: '5', content: 'Sales & marketing', color: 'purple' },
    ]
  },
  keyResources: {
    title: 'Key Resources',
    items: [
      { id: '6', content: 'Engineering team', color: 'green' },
      { id: '7', content: 'Customer success team', color: 'green' },
      { id: '8', content: 'Proprietary AI models', color: 'green' },
    ]
  },
  valuePropositions: {
    title: 'Value Propositions',
    items: [
      { id: '9', content: 'AI-native operations management', color: 'amber' },
      { id: '10', content: 'Full organizational graph visibility', color: 'amber' },
      { id: '11', content: 'Automated insights & recommendations', color: 'amber' },
    ]
  },
  customerRelationships: {
    title: 'Customer Relationships',
    items: [
      { id: '12', content: 'Dedicated customer success manager', color: 'cyan' },
      { id: '13', content: 'Self-service knowledge base', color: 'cyan' },
    ]
  },
  channels: {
    title: 'Channels',
    items: [
      { id: '14', content: 'Direct sales', color: 'pink' },
      { id: '15', content: 'Partner referrals', color: 'pink' },
      { id: '16', content: 'Content marketing', color: 'pink' },
    ]
  },
  customerSegments: {
    title: 'Customer Segments',
    items: [
      { id: '17', content: 'Mid-market SaaS companies (50-500 employees)', color: 'orange' },
      { id: '18', content: 'Operations-heavy service businesses', color: 'orange' },
    ]
  },
  costStructure: {
    title: 'Cost Structure',
    items: [
      { id: '19', content: 'Engineering salaries (60%)', color: 'red' },
      { id: '20', content: 'Cloud infrastructure (15%)', color: 'red' },
      { id: '21', content: 'Sales & marketing (20%)', color: 'red' },
    ]
  },
  revenueStreams: {
    title: 'Revenue Streams',
    items: [
      { id: '22', content: 'Annual SaaS subscriptions', color: 'emerald' },
      { id: '23', content: 'Implementation services', color: 'emerald' },
    ]
  },
})

const getColorClasses = (color: string) => {
  const colors: Record<string, string> = {
    blue: 'bg-blue-500/20 border-blue-500/30 text-blue-200',
    purple: 'bg-purple-500/20 border-purple-500/30 text-purple-200',
    green: 'bg-emerald-500/20 border-emerald-500/30 text-emerald-200',
    amber: 'bg-amber-500/20 border-amber-500/30 text-amber-200',
    cyan: 'bg-cyan-500/20 border-cyan-500/30 text-cyan-200',
    pink: 'bg-pink-500/20 border-pink-500/30 text-pink-200',
    orange: 'bg-orange-500/20 border-orange-500/30 text-orange-200',
    red: 'bg-red-500/20 border-red-500/30 text-red-200',
    emerald: 'bg-emerald-500/20 border-emerald-500/30 text-emerald-200',
  }
  return colors[color] || colors.blue
}

const sendAiMessage = () => {
  if (!userInput.value.trim()) return
  aiMessages.value.push({ role: 'user', content: userInput.value })

  // Simulate AI response
  setTimeout(() => {
    aiMessages.value.push({
      role: 'ai',
      content: "Great insight! I've added that to your Value Propositions. Now, let's think about your Customer Segments. Who are the primary users or buyers of your product?"
    })
  }, 1000)

  userInput.value = ''
}
</script>

<template>
  <div class="flex h-[calc(100vh-8rem)] flex-col">
    <!-- Header -->
    <div class="mb-4 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="text-2xl font-bold text-white">Strategic Canvas</h1>
        <p class="text-slate-400">Visualize and refine your business model</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- Canvas Type Toggle -->
        <div class="flex rounded-lg bg-slate-800 p-1">
          <button
            v-for="type in [{ id: 'bmc', label: 'Business Model' }, { id: 'lean', label: 'Lean' }, { id: 'value_prop', label: 'Value Prop' }]"
            :key="type.id"
            :class="[
              'rounded-md px-3 py-1.5 text-sm transition-colors',
              activeCanvas === type.id ? 'bg-purple-500/30 text-purple-300' : 'text-slate-400 hover:text-white'
            ]"
            @click="activeCanvas = type.id as 'bmc' | 'lean' | 'value_prop'"
          >
            {{ type.label }}
          </button>
        </div>

        <!-- AI Interview Toggle -->
        <button
          :class="[
            'flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-medium transition-colors',
            aiInterviewMode
              ? 'bg-purple-500 text-white'
              : 'border border-slate-700 bg-slate-800/70 text-slate-200 hover:bg-slate-800'
          ]"
          @click="aiInterviewMode = !aiInterviewMode"
        >
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
          </svg>
          AI Interview
        </button>

        <button class="rounded-xl bg-gradient-to-r from-purple-500 to-blue-600 px-4 py-2 text-sm font-semibold text-white">
          Export
        </button>
      </div>
    </div>

    <!-- Main Content -->
    <div class="flex flex-1 gap-4 overflow-hidden">
      <!-- Canvas Grid -->
      <div :class="['flex-1 overflow-auto rounded-2xl border border-slate-700 bg-slate-800/40 p-4', aiInterviewMode ? 'lg:w-2/3' : '']">
        <!-- BMC Layout -->
        <div v-if="activeCanvas === 'bmc'" class="grid h-full min-h-[600px] gap-3" style="grid-template-columns: 1fr 1fr 2fr 1fr 1fr; grid-template-rows: 1fr 1fr 1fr;">
          <!-- Key Partners -->
          <div class="row-span-2 rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.keyPartners.title }}</h3>
            <div class="space-y-2">
              <div
                v-for="item in bmcSections.keyPartners.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
              <button class="w-full rounded-lg border border-dashed border-slate-600 p-2 text-xs text-slate-500 hover:border-slate-500 hover:text-slate-400">
                + Add
              </button>
            </div>
          </div>

          <!-- Key Activities -->
          <div class="rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.keyActivities.title }}</h3>
            <div class="space-y-2">
              <div
                v-for="item in bmcSections.keyActivities.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>

          <!-- Value Propositions -->
          <div class="row-span-2 rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.valuePropositions.title }}</h3>
            <div class="space-y-2">
              <div
                v-for="item in bmcSections.valuePropositions.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>

          <!-- Customer Relationships -->
          <div class="rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.customerRelationships.title }}</h3>
            <div class="space-y-2">
              <div
                v-for="item in bmcSections.customerRelationships.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>

          <!-- Customer Segments -->
          <div class="row-span-2 rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.customerSegments.title }}</h3>
            <div class="space-y-2">
              <div
                v-for="item in bmcSections.customerSegments.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>

          <!-- Key Resources -->
          <div class="rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.keyResources.title }}</h3>
            <div class="space-y-2">
              <div
                v-for="item in bmcSections.keyResources.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>

          <!-- Channels -->
          <div class="rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.channels.title }}</h3>
            <div class="space-y-2">
              <div
                v-for="item in bmcSections.channels.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>

          <!-- Cost Structure -->
          <div class="col-span-2 rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.costStructure.title }}</h3>
            <div class="flex flex-wrap gap-2">
              <div
                v-for="item in bmcSections.costStructure.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>

          <!-- Revenue Streams -->
          <div class="col-span-3 rounded-xl border border-slate-700 bg-slate-900/40 p-3">
            <h3 class="mb-2 text-xs font-semibold uppercase text-slate-400">{{ bmcSections.revenueStreams.title }}</h3>
            <div class="flex flex-wrap gap-2">
              <div
                v-for="item in bmcSections.revenueStreams.items"
                :key="item.id"
                :class="['rounded-lg border p-2 text-xs', getColorClasses(item.color)]"
              >
                {{ item.content }}
              </div>
            </div>
          </div>
        </div>

        <!-- Lean Canvas Placeholder -->
        <div v-else-if="activeCanvas === 'lean'" class="flex h-full items-center justify-center text-slate-400">
          <div class="text-center">
            <svg class="mx-auto h-12 w-12 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" />
            </svg>
            <p>Lean Canvas - Coming soon</p>
          </div>
        </div>

        <!-- Value Prop Canvas Placeholder -->
        <div v-else class="flex h-full items-center justify-center text-slate-400">
          <div class="text-center">
            <svg class="mx-auto h-12 w-12 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
            <p>Value Proposition Canvas - Coming soon</p>
          </div>
        </div>
      </div>

      <!-- AI Interview Panel -->
      <div v-if="aiInterviewMode" class="w-96 flex-shrink-0 rounded-2xl border border-purple-500/30 bg-purple-500/5 flex flex-col">
        <div class="border-b border-purple-500/20 p-4">
          <div class="flex items-center gap-2">
            <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-purple-500/20">
              <svg class="h-4 w-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
              </svg>
            </div>
            <div>
              <h3 class="font-semibold text-white">AI Canvas Builder</h3>
              <p class="text-xs text-slate-400">Answer questions to build your canvas</p>
            </div>
          </div>
        </div>

        <!-- Messages -->
        <div class="flex-1 overflow-y-auto p-4 space-y-4">
          <div
            v-for="(msg, idx) in aiMessages"
            :key="idx"
            :class="[
              'rounded-xl p-3 text-sm',
              msg.role === 'ai'
                ? 'bg-purple-500/10 text-slate-200'
                : 'bg-slate-700 text-white ml-8'
            ]"
          >
            {{ msg.content }}
          </div>
        </div>

        <!-- Input -->
        <div class="border-t border-purple-500/20 p-4">
          <div class="flex gap-2">
            <input
              v-model="userInput"
              type="text"
              placeholder="Type your answer..."
              class="flex-1 rounded-lg bg-slate-800 border border-slate-700 px-3 py-2 text-sm text-white placeholder-slate-500 focus:border-purple-500 focus:outline-none"
              @keyup.enter="sendAiMessage"
            />
            <button
              class="rounded-lg bg-purple-500 px-4 py-2 text-sm font-medium text-white hover:bg-purple-600"
              @click="sendAiMessage"
            >
              Send
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
