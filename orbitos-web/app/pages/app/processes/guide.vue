<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

// Track active section for table of contents highlighting
const activeSection = ref('overview')

// Scroll to section
const scrollToSection = (sectionId: string) => {
  activeSection.value = sectionId
  const element = document.getElementById(sectionId)
  if (element) {
    element.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

// Symbol data for the legend
const basicSymbols = [
  {
    id: 'manual',
    name: 'Manual Activity',
    shape: 'Rounded Rectangle',
    color: 'blue',
    colorClass: 'bg-blue-500',
    description: 'A task performed by a person without automation. The worker uses their skills, judgment, and physical effort to complete the activity.',
    examples: ['Data entry', 'Physical assembly', 'Quality inspection by eye', 'Customer phone call'],
    fields: ['Name', 'Description', 'Duration', 'Assigned Resource'],
    whenToUse: 'When a human performs the work manually, even if they use basic tools.',
  },
  {
    id: 'automated',
    name: 'Automated Activity',
    shape: 'Rounded Rectangle',
    color: 'emerald',
    colorClass: 'bg-emerald-500',
    description: 'A task performed entirely by a machine, system, or software without human intervention during execution.',
    examples: ['Automated email notification', 'CNC machining cycle', 'Database backup', 'Robotic welding'],
    fields: ['Name', 'Description', 'Duration', 'Linked System/Tool'],
    whenToUse: 'When the activity runs without human input once triggered.',
  },
  {
    id: 'hybrid',
    name: 'Hybrid Activity',
    shape: 'Rounded Rectangle',
    color: 'cyan',
    colorClass: 'bg-cyan-500',
    description: 'A task that combines human effort with automated assistance. The person and system work together.',
    examples: ['Assisted data entry with auto-complete', 'Semi-automated testing', 'Machine-assisted sorting'],
    fields: ['Name', 'Description', 'Duration', 'Assigned Resource', 'Linked System'],
    whenToUse: 'When humans and machines collaborate on the same task.',
  },
  {
    id: 'decision',
    name: 'Decision',
    shape: 'Diamond',
    color: 'amber',
    colorClass: 'bg-amber-500',
    description: 'A point where the process branches based on a condition, question, or evaluation. Creates multiple paths.',
    examples: ['Approved/Rejected', 'Pass/Fail inspection', 'Yes/No confirmation', 'Route selection'],
    fields: ['Name', 'Decision Question', 'Yes Criteria', 'No Criteria', 'Historical Yes %'],
    whenToUse: 'When the flow must choose between two or more paths based on a condition.',
  },
  {
    id: 'handoff',
    name: 'Handoff',
    shape: 'Rounded Rectangle',
    color: 'purple',
    colorClass: 'bg-purple-500',
    description: 'A transfer point where work moves from one person, team, or department to another.',
    examples: ['Pass to QA team', 'Send to shipping', 'Escalate to manager', 'Transfer to next shift'],
    fields: ['Name', 'Description', 'From Resource', 'To Resource'],
    whenToUse: 'When responsibility changes hands between people or teams.',
  },
]

const ieSymbols = [
  {
    id: 'operation',
    name: 'Operation',
    shape: 'Large Circle',
    color: 'emerald',
    colorClass: 'bg-emerald-500',
    description: 'The primary value-adding activity in ASME Y15.3 standard. Operations transform materials, add value, or directly contribute to the product/service the customer pays for.',
    examples: ['Machine part', 'Assemble component', 'Package product', 'Prepare meal', 'Write code'],
    fields: [
      { name: 'Operation Type', desc: 'Fabrication, assembly, processing, packaging, service' },
      { name: 'Workstation', desc: 'Machine or station where work is performed' },
      { name: 'Setup Time', desc: 'Changeover time in minutes (for SMED analysis)' },
      { name: 'Standard Time', desc: 'Standard time per unit from time study' },
      { name: 'Pieces/Hour', desc: 'Throughput rate for capacity planning' },
    ],
    whenToUse: 'For the core value-adding work that transforms the product. This is what customers pay for.',
    tip: 'Maximize the ratio of Operation time to total cycle time. Non-operation time is waste to minimize.',
    valueCategory: 'value-add',
  },
  {
    id: 'inspection',
    name: 'Inspection',
    shape: 'Square',
    color: 'rose',
    colorClass: 'bg-rose-500',
    description: 'A quality check or verification step where items are examined against standards or specifications.',
    examples: ['Visual quality check', 'Dimensional measurement', 'Functional testing', 'Document review'],
    fields: [
      { name: 'Inspection Criteria', desc: 'What standards or specs are being checked' },
      { name: 'Pass Rate %', desc: 'Historical percentage that pass inspection' },
      { name: 'Sampling Rate %', desc: '100 = inspect all, lower = statistical sampling' },
    ],
    whenToUse: 'Whenever quality is verified, measurements are taken, or compliance is checked.',
    tip: 'Track pass rates over time to identify quality trends and improvement opportunities.',
  },
  {
    id: 'transport',
    name: 'Transport',
    shape: 'Circle with Arrow',
    color: 'orange',
    colorClass: 'bg-orange-500',
    description: 'Movement of materials, products, or information from one location to another.',
    examples: ['Move parts to assembly', 'Deliver to shipping dock', 'Transfer files to server', 'Carry to next station'],
    fields: [
      { name: 'Distance', desc: 'How far the item travels' },
      { name: 'Unit', desc: 'Meters, feet, kilometers, miles' },
      { name: 'Transport Mode', desc: 'Manual, forklift, conveyor, cart, truck, AGV' },
      { name: 'Origin', desc: 'Starting location' },
      { name: 'Destination', desc: 'Ending location' },
    ],
    whenToUse: 'When items physically move between locations. Transport is non-value-adding waste in lean.',
    tip: 'Minimize transport distance and frequency. Consider co-locating related operations.',
  },
  {
    id: 'delay',
    name: 'Delay',
    shape: 'D-Shape (Half Circle)',
    color: 'gray',
    colorClass: 'bg-gray-500',
    description: 'A wait or queue time where work items are idle, waiting for the next step.',
    examples: ['Queue for approval', 'Waiting for batch', 'Curing/drying time', 'Cooling period'],
    fields: [
      { name: 'Delay Reason', desc: 'Queue, batching, approval, curing, drying, cooling, scheduling' },
      { name: 'Average Wait', desc: 'Typical wait time in minutes' },
      { name: 'Max Wait', desc: 'Maximum observed wait time' },
    ],
    whenToUse: 'When work sits idle. Delays are waste unless technically required (like curing).',
    tip: 'Distinguish between necessary delays (curing) and unnecessary ones (queue). Target unnecessary delays for elimination.',
  },
  {
    id: 'storage',
    name: 'Storage',
    shape: 'Inverted Triangle',
    color: 'yellow',
    colorClass: 'bg-yellow-500',
    description: 'Controlled storage of materials, work-in-progress, or finished goods in a designated location.',
    examples: ['Raw material warehouse', 'WIP staging area', 'Finished goods inventory', 'Tool crib'],
    fields: [
      { name: 'Storage Type', desc: 'Raw materials, WIP, finished goods, tools, consumables' },
      { name: 'Capacity', desc: 'How much can be stored' },
      { name: 'Location', desc: 'Physical storage location identifier' },
      { name: 'Inventory Method', desc: 'FIFO, LIFO, FEFO' },
    ],
    whenToUse: 'When items are held in inventory. Storage ties up capital and space.',
    tip: 'FIFO (First In, First Out) prevents aging. Track capacity utilization to optimize space.',
  },
  {
    id: 'document',
    name: 'Document',
    shape: 'Rectangle with Wavy Bottom',
    color: 'indigo',
    colorClass: 'bg-indigo-500',
    description: 'Creation, review, or use of a document - paper or digital forms, reports, or records.',
    examples: ['Fill out inspection form', 'Generate report', 'Review specification', 'Sign approval document'],
    fields: [
      { name: 'Document Type', desc: 'Form, report, checklist, certificate, drawing, specification' },
      { name: 'Document Code', desc: 'Document number for tracking (e.g., QC-FORM-001)' },
      { name: 'Controlled Document', desc: 'Whether this is a controlled/versioned document' },
    ],
    whenToUse: 'When paperwork or documentation is created, filled out, or referenced.',
    tip: 'Identify which documents are legally required vs. which might be eliminated.',
  },
  {
    id: 'database',
    name: 'Database',
    shape: 'Cylinder',
    color: 'teal',
    colorClass: 'bg-teal-500',
    description: 'Interaction with a data storage system - reading from or writing to a database or information system.',
    examples: ['Update ERP', 'Query inventory levels', 'Log transaction', 'Retrieve customer data'],
    fields: [
      { name: 'System Name', desc: 'Name of the system (SAP, Oracle, custom app)' },
      { name: 'Operation', desc: 'Read only, write only, or both' },
      { name: 'Integration Type', desc: 'Direct connection, API, file transfer, message queue' },
    ],
    whenToUse: 'When the process interacts with an information system or database.',
    tip: 'Document which systems are involved to understand integration dependencies.',
  },
  {
    id: 'manualInput',
    name: 'Manual Input',
    shape: 'Parallelogram (Slanted)',
    color: 'pink',
    colorClass: 'bg-pink-500',
    description: 'A point where a person enters data into a system via keyboard, scanner, touchscreen, or other input device.',
    examples: ['Enter order details', 'Scan barcode', 'Input measurements', 'Record lot number'],
    fields: [
      { name: 'Input Device', desc: 'Keyboard, barcode scanner, touchscreen, voice' },
      { name: 'Expected Time', desc: 'How long data entry takes (seconds)' },
      { name: 'Error Rate %', desc: 'Historical data entry error rate' },
    ],
    whenToUse: 'When humans type, scan, or otherwise enter data into a system.',
    tip: 'High error rates suggest need for validation, auto-fill, or barcode scanning.',
  },
  {
    id: 'display',
    name: 'Display',
    shape: 'Angled Rectangle (Monitor)',
    color: 'sky',
    colorClass: 'bg-sky-500',
    description: 'Information displayed to workers or stakeholders on a screen, monitor, or display board.',
    examples: ['Show work instructions', 'Display dashboard', 'Andon board alert', 'Status screen'],
    fields: [
      { name: 'Display Type', desc: 'Dashboard, report, alert, status, work instructions' },
      { name: 'Display Device', desc: 'Monitor, andon board, mobile, projector' },
      { name: 'Refresh Rate', desc: 'How often the display updates (seconds)' },
      { name: 'Display Location', desc: 'Where the display is located' },
    ],
    whenToUse: 'When information is shown to people as part of the process.',
    tip: 'Place displays where workers can see them without stopping work.',
  },
]

const guidelines = [
  {
    title: '1. Define Clear Boundaries',
    icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2',
    content: 'Every process needs a clear starting trigger and ending output. Ask: "What kicks this off?" and "What do we deliver when done?"',
    dos: ['Define the triggering event', 'Specify the deliverable/output', 'Name the process by its outcome'],
    donts: ['Start with vague "begin" nodes', 'Leave the ending unclear', 'Mix multiple processes together'],
  },
  {
    title: '2. Use the Right Level of Detail',
    icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z',
    content: 'Match detail level to your audience. Executive overview differs from operator work instructions. Use subprocesses to manage complexity.',
    dos: ['Use subprocesses for complex steps', 'Keep main flow to 5-15 activities', 'Link to detailed procedures when needed'],
    donts: ['Cram 50 steps in one diagram', 'Include irrelevant details', 'Make it too abstract to be useful'],
  },
  {
    title: '3. Follow the Flow',
    icon: 'M13 5l7 7-7 7M5 5l7 7-7 7',
    content: 'Processes should flow logically, typically top-to-bottom or left-to-right. Minimize crossing lines and backtracking.',
    dos: ['Maintain consistent direction', 'Place decisions clearly', 'Show all possible paths'],
    donts: ['Create spaghetti diagrams', 'Hide exception paths', 'Assume readers know the shortcuts'],
  },
  {
    title: '4. Name Activities with Verbs',
    icon: 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z',
    content: 'Activity names should be action-oriented. Use verb + noun format: "Inspect Assembly", not just "Inspection".',
    dos: ['Start with action verbs', 'Be specific about what\'s done', 'Use consistent naming patterns'],
    donts: ['Use vague names like "Process"', 'Write long paragraphs as names', 'Use jargon others won\'t understand'],
  },
  {
    title: '5. Assign Resources',
    icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z',
    content: 'Every activity should have a clear owner - who or what performs it. This enables workload analysis and accountability.',
    dos: ['Assign roles to all manual steps', 'Identify systems for automated steps', 'Note handoffs between roles'],
    donts: ['Leave activities unassigned', 'Assume "someone" will do it', 'Skip tool/system assignments'],
  },
  {
    title: '6. Capture Time Data',
    icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    content: 'Document estimated durations to enable cycle time analysis and identify bottlenecks.',
    dos: ['Estimate realistic durations', 'Include wait times (delays)', 'Differentiate value-add from non-value-add'],
    donts: ['Guess wildly', 'Ignore delay times', 'Use only "best case" estimates'],
  },
]

const tableOfContents = [
  { id: 'overview', label: 'Overview' },
  { id: 'basic-symbols', label: 'Basic Symbols' },
  { id: 'ie-symbols', label: 'IE Flowchart Symbols' },
  { id: 'ise-analysis', label: 'ISE Analysis Metrics' },
  { id: 'guidelines', label: 'Process Mapping Guidelines' },
  { id: 'best-practices', label: 'Best Practices' },
  { id: 'common-mistakes', label: 'Common Mistakes' },
]
</script>

<template>
  <div class="flex h-[calc(100vh-4rem)]">
    <!-- Sidebar Table of Contents -->
    <aside class="w-64 flex-shrink-0 border-r border-white/10 overflow-y-auto hidden lg:block">
      <div class="p-4 sticky top-0">
        <h2 class="text-sm font-semibold text-white/40 uppercase tracking-wider mb-4">Contents</h2>
        <nav class="space-y-1">
          <button
            v-for="item in tableOfContents"
            :key="item.id"
            @click="scrollToSection(item.id)"
            :class="[
              'w-full text-left px-3 py-2 rounded-lg text-sm transition-colors',
              activeSection === item.id
                ? 'bg-purple-500/20 text-purple-300'
                : 'text-white/60 hover:text-white hover:bg-white/5'
            ]"
          >
            {{ item.label }}
          </button>
        </nav>

        <div class="mt-8 pt-6 border-t border-white/10">
          <NuxtLink
            to="/app/processes"
            class="flex items-center gap-2 text-sm text-white/40 hover:text-white transition-colors"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            Back to Processes
          </NuxtLink>
        </div>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="flex-1 overflow-y-auto">
      <div class="max-w-4xl mx-auto px-6 py-8">
        <!-- Header -->
        <div class="mb-10">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-12 h-12 rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 flex items-center justify-center">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <div>
              <h1 class="text-2xl font-bold text-white">Process Mapping Guide</h1>
              <p class="text-white/60">Symbol reference and best practices for creating effective process flows</p>
            </div>
          </div>
        </div>

        <!-- Overview Section -->
        <section id="overview" class="mb-12 scroll-mt-6">
          <h2 class="text-xl font-semibold text-white mb-4 flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-purple-500/20 flex items-center justify-center">
              <svg class="w-4 h-4 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </span>
            Overview
          </h2>
          <div class="orbitos-glass-subtle rounded-xl p-6">
            <p class="text-white/80 leading-relaxed mb-4">
              Process mapping is a visual method for documenting how work flows through your organization.
              A well-designed process map helps teams understand their work, identify inefficiencies,
              train new employees, and drive continuous improvement.
            </p>
            <p class="text-white/80 leading-relaxed mb-4">
              This guide covers two types of symbols:
            </p>
            <ul class="space-y-2 text-white/70">
              <li class="flex items-start gap-2">
                <span class="w-5 h-5 rounded bg-blue-500/20 flex items-center justify-center flex-shrink-0 mt-0.5">
                  <span class="w-2 h-2 rounded-sm bg-blue-400"></span>
                </span>
                <span><strong class="text-white">Basic Activity Symbols</strong> - General-purpose shapes for manual, automated, and hybrid work</span>
              </li>
              <li class="flex items-start gap-2">
                <span class="w-5 h-5 rounded bg-rose-500/20 flex items-center justify-center flex-shrink-0 mt-0.5">
                  <span class="w-2 h-2 rounded bg-rose-400"></span>
                </span>
                <span><strong class="text-white">Industrial Engineering (IE) Symbols</strong> - ASME/ISO standard shapes for detailed operations analysis</span>
              </li>
            </ul>
          </div>
        </section>

        <!-- Basic Symbols Section -->
        <section id="basic-symbols" class="mb-12 scroll-mt-6">
          <h2 class="text-xl font-semibold text-white mb-4 flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-blue-500/20 flex items-center justify-center">
              <svg class="w-4 h-4 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6z" />
              </svg>
            </span>
            Basic Symbols
          </h2>
          <p class="text-white/60 mb-6">
            Use these general-purpose symbols for most process mapping needs.
          </p>

          <div class="space-y-4">
            <div
              v-for="symbol in basicSymbols"
              :key="symbol.id"
              class="orbitos-glass-subtle rounded-xl p-6"
            >
              <div class="flex items-start gap-4">
                <!-- Symbol Preview -->
                <div class="flex-shrink-0">
                  <div
                    v-if="symbol.id === 'decision'"
                    class="w-16 h-16 flex items-center justify-center"
                  >
                    <div :class="[symbol.colorClass, 'w-12 h-12 rotate-45 rounded-sm']"></div>
                  </div>
                  <div
                    v-else
                    :class="[symbol.colorClass, 'w-16 h-10 rounded-lg flex items-center justify-center']"
                  >
                    <span class="text-white text-xs font-medium">{{ symbol.id.charAt(0).toUpperCase() }}</span>
                  </div>
                </div>

                <!-- Symbol Details -->
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-3 mb-2">
                    <h3 class="text-lg font-semibold text-white">{{ symbol.name }}</h3>
                    <span class="text-xs px-2 py-0.5 rounded-full bg-white/10 text-white/50">{{ symbol.shape }}</span>
                  </div>
                  <p class="text-white/70 mb-3">{{ symbol.description }}</p>

                  <div class="grid md:grid-cols-2 gap-4">
                    <div>
                      <h4 class="text-sm font-medium text-white/50 mb-1">Examples</h4>
                      <ul class="text-sm text-white/60 space-y-0.5">
                        <li v-for="ex in symbol.examples" :key="ex" class="flex items-center gap-1.5">
                          <span class="w-1 h-1 rounded-full bg-white/30"></span>
                          {{ ex }}
                        </li>
                      </ul>
                    </div>
                    <div>
                      <h4 class="text-sm font-medium text-white/50 mb-1">When to Use</h4>
                      <p class="text-sm text-white/60">{{ symbol.whenToUse }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- IE Symbols Section -->
        <section id="ie-symbols" class="mb-12 scroll-mt-6">
          <h2 class="text-xl font-semibold text-white mb-4 flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-rose-500/20 flex items-center justify-center">
              <svg class="w-4 h-4 text-rose-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z" />
              </svg>
            </span>
            Industrial Engineering (IE) Flowchart Symbols
          </h2>
          <p class="text-white/60 mb-6">
            These symbols follow ASME (American Society of Mechanical Engineers) and ISO standards for detailed operations analysis.
            They help identify value-adding vs. non-value-adding activities in lean manufacturing and process improvement.
          </p>

          <!-- Value-Add Legend -->
          <div class="orbitos-glass-subtle rounded-xl p-4 mb-6">
            <h4 class="text-sm font-semibold text-white mb-3">Understanding Value in Process Analysis</h4>
            <div class="grid md:grid-cols-3 gap-4 text-sm">
              <div class="flex items-start gap-2">
                <span class="w-3 h-3 rounded-full bg-emerald-500 mt-0.5 flex-shrink-0"></span>
                <div>
                  <span class="font-medium text-emerald-300">Value-Adding</span>
                  <p class="text-white/50">Customer would pay for this work (Operations, some Inspections)</p>
                </div>
              </div>
              <div class="flex items-start gap-2">
                <span class="w-3 h-3 rounded-full bg-amber-500 mt-0.5 flex-shrink-0"></span>
                <div>
                  <span class="font-medium text-amber-300">Required Waste</span>
                  <p class="text-white/50">Necessary but adds no value (Required inspections, compliance)</p>
                </div>
              </div>
              <div class="flex items-start gap-2">
                <span class="w-3 h-3 rounded-full bg-red-500 mt-0.5 flex-shrink-0"></span>
                <div>
                  <span class="font-medium text-red-300">Pure Waste</span>
                  <p class="text-white/50">Target for elimination (Transport, Delay, excess Storage)</p>
                </div>
              </div>
            </div>
          </div>

          <div class="space-y-6">
            <div
              v-for="symbol in ieSymbols"
              :key="symbol.id"
              class="orbitos-glass-subtle rounded-xl p-6"
            >
              <div class="flex items-start gap-4">
                <!-- Symbol Preview -->
                <div class="flex-shrink-0 w-20">
                  <!-- Operation - Large Circle (ASME primary value-add) -->
                  <svg v-if="symbol.id === 'operation'" width="64" height="64" viewBox="0 0 64 64">
                    <circle cx="32" cy="32" r="26" fill="none" stroke="rgb(16, 185, 129)" stroke-width="3"/>
                    <circle cx="32" cy="32" r="8" fill="rgb(16, 185, 129)" opacity="0.6"/>
                  </svg>
                  <!-- Inspection - Square -->
                  <svg v-else-if="symbol.id === 'inspection'" width="64" height="64" viewBox="0 0 64 64">
                    <rect x="8" y="8" width="48" height="48" rx="2" fill="none" :stroke="'rgb(244, 63, 94)'" stroke-width="3"/>
                    <path d="M22 32 L28 38 L42 24" fill="none" stroke="rgb(244, 63, 94)" stroke-width="3" stroke-linecap="round"/>
                  </svg>
                  <!-- Transport - Circle with arrow -->
                  <svg v-else-if="symbol.id === 'transport'" width="64" height="64" viewBox="0 0 64 64">
                    <circle cx="32" cy="32" r="24" fill="none" stroke="rgb(249, 115, 22)" stroke-width="3"/>
                    <path d="M20 32 L44 32 M38 26 L44 32 L38 38" fill="none" stroke="rgb(249, 115, 22)" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                  <!-- Delay - D shape -->
                  <svg v-else-if="symbol.id === 'delay'" width="64" height="64" viewBox="0 0 64 64">
                    <path d="M16 8 L16 56 L32 56 A24 24 0 0 0 32 8 Z" fill="none" stroke="rgb(156, 163, 175)" stroke-width="3"/>
                  </svg>
                  <!-- Storage - Inverted triangle -->
                  <svg v-else-if="symbol.id === 'storage'" width="64" height="64" viewBox="0 0 64 64">
                    <polygon points="32,56 8,12 56,12" fill="none" stroke="rgb(234, 179, 8)" stroke-width="3" stroke-linejoin="round"/>
                  </svg>
                  <!-- Document - Wavy bottom -->
                  <svg v-else-if="symbol.id === 'document'" width="64" height="64" viewBox="0 0 64 64">
                    <path d="M8 8 L56 8 L56 48 Q48 54, 40 48 Q32 42, 24 48 Q16 54, 8 48 Z" fill="none" stroke="rgb(99, 102, 241)" stroke-width="3"/>
                  </svg>
                  <!-- Database - Cylinder -->
                  <svg v-else-if="symbol.id === 'database'" width="64" height="64" viewBox="0 0 64 64">
                    <ellipse cx="32" cy="16" rx="20" ry="8" fill="none" stroke="rgb(20, 184, 166)" stroke-width="3"/>
                    <path d="M12 16 L12 48 A20 8 0 0 0 52 48 L52 16" fill="none" stroke="rgb(20, 184, 166)" stroke-width="3"/>
                  </svg>
                  <!-- Manual Input - Parallelogram -->
                  <svg v-else-if="symbol.id === 'manualInput'" width="64" height="64" viewBox="0 0 64 64">
                    <polygon points="8,48 20,16 56,16 44,48" fill="none" stroke="rgb(236, 72, 153)" stroke-width="3" stroke-linejoin="round"/>
                  </svg>
                  <!-- Display - Angled rectangle -->
                  <svg v-else-if="symbol.id === 'display'" width="64" height="64" viewBox="0 0 64 64">
                    <polygon points="12,12 52,12 56,52 8,52" fill="none" stroke="rgb(14, 165, 233)" stroke-width="3" stroke-linejoin="round"/>
                  </svg>
                </div>

                <!-- Symbol Details -->
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-3 mb-2">
                    <h3 class="text-lg font-semibold text-white">{{ symbol.name }}</h3>
                    <span class="text-xs px-2 py-0.5 rounded-full bg-white/10 text-white/50">{{ symbol.shape }}</span>
                  </div>
                  <p class="text-white/70 mb-4">{{ symbol.description }}</p>

                  <!-- Metadata Fields -->
                  <div class="mb-4">
                    <h4 class="text-sm font-medium text-white/50 mb-2">Metadata Fields</h4>
                    <div class="flex flex-wrap gap-2">
                      <div
                        v-for="field in symbol.fields"
                        :key="typeof field === 'string' ? field : field.name"
                        class="group relative"
                      >
                        <span class="text-xs px-2 py-1 rounded bg-white/5 text-white/70 border border-white/10">
                          {{ typeof field === 'string' ? field : field.name }}
                        </span>
                        <div
                          v-if="typeof field !== 'string'"
                          class="absolute bottom-full left-0 mb-1 px-2 py-1 bg-gray-900 text-xs text-white/80 rounded shadow-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap z-10 pointer-events-none"
                        >
                          {{ field.desc }}
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="grid md:grid-cols-2 gap-4">
                    <div>
                      <h4 class="text-sm font-medium text-white/50 mb-1">Examples</h4>
                      <ul class="text-sm text-white/60 space-y-0.5">
                        <li v-for="ex in symbol.examples" :key="ex" class="flex items-center gap-1.5">
                          <span class="w-1 h-1 rounded-full bg-white/30"></span>
                          {{ ex }}
                        </li>
                      </ul>
                    </div>
                    <div>
                      <h4 class="text-sm font-medium text-white/50 mb-1">When to Use</h4>
                      <p class="text-sm text-white/60 mb-2">{{ symbol.whenToUse }}</p>
                      <div v-if="symbol.tip" class="flex items-start gap-2 p-2 rounded bg-purple-500/10 border border-purple-500/20">
                        <svg class="w-4 h-4 text-purple-400 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                        </svg>
                        <span class="text-xs text-purple-200">{{ symbol.tip }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- ISE Analysis Section -->
        <section id="ise-analysis" class="mb-12 scroll-mt-6">
          <h2 class="text-xl font-semibold text-white mb-4 flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-cyan-500/20 flex items-center justify-center">
              <svg class="w-4 h-4 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            </span>
            Industrial & Systems Engineering Analysis
          </h2>
          <p class="text-white/60 mb-6">
            Use these metrics to analyze process performance, identify bottlenecks, and drive continuous improvement.
          </p>

          <!-- Takt Time -->
          <div class="orbitos-glass-subtle rounded-xl p-6 mb-4">
            <div class="flex items-start gap-4">
              <div class="w-12 h-12 rounded-lg bg-cyan-500/20 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6 text-cyan-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div class="flex-1">
                <h3 class="text-lg font-semibold text-white mb-2">Takt Time</h3>
                <p class="text-white/70 mb-4">
                  The rate at which you must complete units to meet customer demand. It's the "heartbeat" of your production system.
                </p>
                <div class="bg-cyan-900/30 rounded-lg p-4 mb-4 font-mono text-cyan-200">
                  <div class="text-sm text-cyan-400 mb-1">Formula:</div>
                  <div class="text-lg">Takt Time = Available Work Time / Customer Demand</div>
                </div>
                <div class="grid md:grid-cols-2 gap-4 text-sm">
                  <div>
                    <h4 class="font-medium text-white mb-2">Example</h4>
                    <p class="text-white/60">
                      If you work 480 min/day and need 120 units:<br/>
                      Takt = 480 / 120 = <span class="text-cyan-300 font-semibold">4 min/unit</span><br/>
                      Every 4 minutes, one unit must be completed.
                    </p>
                  </div>
                  <div>
                    <h4 class="font-medium text-white mb-2">Why It Matters</h4>
                    <p class="text-white/60">
                      Compare each activity's cycle time to takt. Activities longer than takt are bottlenecks.
                      Activities much faster than takt may indicate overproduction.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Bottleneck Identification -->
          <div class="orbitos-glass-subtle rounded-xl p-6 mb-4">
            <div class="flex items-start gap-4">
              <div class="w-12 h-12 rounded-lg bg-red-500/20 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
              </div>
              <div class="flex-1">
                <h3 class="text-lg font-semibold text-white mb-2">Bottleneck Identification</h3>
                <p class="text-white/70 mb-4">
                  The bottleneck is the activity that limits total system throughput. It's the constraint that determines your maximum capacity.
                </p>
                <div class="grid md:grid-cols-3 gap-3 mb-4">
                  <div class="bg-white/5 rounded-lg p-3">
                    <div class="text-2xl font-bold text-red-400 mb-1">1</div>
                    <div class="text-sm text-white font-medium">Find Longest Cycle</div>
                    <p class="text-xs text-white/50 mt-1">The activity with the highest cycle time is often the bottleneck</p>
                  </div>
                  <div class="bg-white/5 rounded-lg p-3">
                    <div class="text-2xl font-bold text-amber-400 mb-1">2</div>
                    <div class="text-sm text-white font-medium">Look for WIP Buildup</div>
                    <p class="text-xs text-white/50 mt-1">Queue/delay symbols with high wait times indicate upstream bottleneck</p>
                  </div>
                  <div class="bg-white/5 rounded-lg p-3">
                    <div class="text-2xl font-bold text-emerald-400 mb-1">3</div>
                    <div class="text-sm text-white font-medium">Compare to Takt</div>
                    <p class="text-xs text-white/50 mt-1">Any activity cycle time > takt time is a bottleneck candidate</p>
                  </div>
                </div>
                <div class="bg-red-900/20 border border-red-500/30 rounded-lg p-3">
                  <p class="text-sm text-red-200">
                    <strong>Theory of Constraints:</strong> Improving non-bottleneck activities does NOT increase system throughput.
                    Focus improvement efforts on the constraint first.
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- Process Efficiency / Value-Add Ratio -->
          <div class="orbitos-glass-subtle rounded-xl p-6 mb-4">
            <div class="flex items-start gap-4">
              <div class="w-12 h-12 rounded-lg bg-emerald-500/20 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div class="flex-1">
                <h3 class="text-lg font-semibold text-white mb-2">Process Efficiency (PCE)</h3>
                <p class="text-white/70 mb-4">
                  Process Cycle Efficiency measures how much of your total lead time is spent on value-adding work.
                  Most processes are 1-10% efficient, with 90%+ of time spent waiting.
                </p>
                <div class="bg-emerald-900/30 rounded-lg p-4 mb-4 font-mono text-emerald-200">
                  <div class="text-sm text-emerald-400 mb-1">Formula:</div>
                  <div class="text-lg">PCE = Value-Add Time / Total Lead Time × 100%</div>
                </div>
                <div class="grid md:grid-cols-2 gap-4 text-sm">
                  <div>
                    <h4 class="font-medium text-white mb-2">Calculating Value-Add Time</h4>
                    <p class="text-white/60 mb-2">Sum only the time spent on:</p>
                    <ul class="text-white/50 space-y-1">
                      <li class="flex items-center gap-2">
                        <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                        <span><strong class="text-white">Operation</strong> activities (the big circle)</span>
                      </li>
                      <li class="flex items-center gap-2">
                        <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                        <span>Some <strong class="text-white">Automated</strong> activities</span>
                      </li>
                    </ul>
                  </div>
                  <div>
                    <h4 class="font-medium text-white mb-2">Typical PCE Ranges</h4>
                    <div class="space-y-2">
                      <div class="flex items-center justify-between text-white/60">
                        <span>Office/transactional</span>
                        <span class="text-red-400">1-5%</span>
                      </div>
                      <div class="flex items-center justify-between text-white/60">
                        <span>Batch manufacturing</span>
                        <span class="text-amber-400">5-15%</span>
                      </div>
                      <div class="flex items-center justify-between text-white/60">
                        <span>Lean manufacturing</span>
                        <span class="text-emerald-400">20-40%</span>
                      </div>
                      <div class="flex items-center justify-between text-white/60">
                        <span>World-class flow</span>
                        <span class="text-cyan-400">40-80%</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- OEE (Overall Equipment Effectiveness) -->
          <div class="orbitos-glass-subtle rounded-xl p-6 mb-4">
            <div class="flex items-start gap-4">
              <div class="w-12 h-12 rounded-lg bg-purple-500/20 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </div>
              <div class="flex-1">
                <h3 class="text-lg font-semibold text-white mb-2">OEE (Overall Equipment Effectiveness)</h3>
                <p class="text-white/70 mb-4">
                  The gold standard for measuring equipment productivity in manufacturing. Use for Automated activities.
                </p>
                <div class="bg-purple-900/30 rounded-lg p-4 mb-4 font-mono text-purple-200">
                  <div class="text-sm text-purple-400 mb-1">Formula:</div>
                  <div class="text-lg">OEE = Availability × Performance × Quality</div>
                </div>
                <div class="grid md:grid-cols-3 gap-3">
                  <div class="bg-white/5 rounded-lg p-3">
                    <div class="text-sm font-medium text-blue-300 mb-1">Availability</div>
                    <p class="text-xs text-white/50">Running Time / Planned Time</p>
                    <p class="text-xs text-white/40 mt-1">Losses: breakdowns, changeover, setup</p>
                  </div>
                  <div class="bg-white/5 rounded-lg p-3">
                    <div class="text-sm font-medium text-amber-300 mb-1">Performance</div>
                    <p class="text-xs text-white/50">Actual Speed / Ideal Speed</p>
                    <p class="text-xs text-white/40 mt-1">Losses: slowdowns, minor stops, jams</p>
                  </div>
                  <div class="bg-white/5 rounded-lg p-3">
                    <div class="text-sm font-medium text-emerald-300 mb-1">Quality</div>
                    <p class="text-xs text-white/50">Good Units / Total Units</p>
                    <p class="text-xs text-white/40 mt-1">Losses: scrap, rework, defects</p>
                  </div>
                </div>
                <div class="mt-4 text-sm text-white/60">
                  <strong class="text-white">World-class OEE:</strong> 85%+ | <strong class="text-white">Average:</strong> 60% | <strong class="text-white">Typical:</strong> 40%
                </div>
              </div>
            </div>
          </div>

          <!-- Six Big Losses -->
          <div class="orbitos-glass-subtle rounded-xl p-6">
            <h3 class="text-lg font-semibold text-white mb-4">The Six Big Losses (TPM)</h3>
            <p class="text-white/60 mb-4">
              Total Productive Maintenance identifies six categories of waste that reduce OEE. Map these using your IE symbols.
            </p>
            <div class="grid md:grid-cols-2 gap-3 text-sm">
              <div class="bg-red-500/10 border border-red-500/20 rounded-lg p-3">
                <div class="font-medium text-red-300 mb-1">Availability Losses</div>
                <ul class="text-white/60 space-y-1">
                  <li>1. Equipment failure / breakdowns</li>
                  <li>2. Setup and adjustments (use Setup Time field)</li>
                </ul>
              </div>
              <div class="bg-amber-500/10 border border-amber-500/20 rounded-lg p-3">
                <div class="font-medium text-amber-300 mb-1">Performance Losses</div>
                <ul class="text-white/60 space-y-1">
                  <li>3. Idling and minor stops</li>
                  <li>4. Reduced speed operation</li>
                </ul>
              </div>
              <div class="bg-blue-500/10 border border-blue-500/20 rounded-lg p-3 md:col-span-2">
                <div class="font-medium text-blue-300 mb-1">Quality Losses</div>
                <ul class="text-white/60 space-y-1 grid md:grid-cols-2 gap-x-4">
                  <li>5. Defects and rework (use Inspection rework % field)</li>
                  <li>6. Startup/yield losses (use Inspection scrap % field)</li>
                </ul>
              </div>
            </div>
          </div>
        </section>

        <!-- Guidelines Section -->
        <section id="guidelines" class="mb-12 scroll-mt-6">
          <h2 class="text-xl font-semibold text-white mb-4 flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-emerald-500/20 flex items-center justify-center">
              <svg class="w-4 h-4 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>
            </span>
            Process Mapping Guidelines
          </h2>
          <p class="text-white/60 mb-6">
            Follow these principles to create clear, useful process documentation.
          </p>

          <div class="space-y-4">
            <div
              v-for="guideline in guidelines"
              :key="guideline.title"
              class="orbitos-glass-subtle rounded-xl p-6"
            >
              <div class="flex items-start gap-4">
                <div class="w-10 h-10 rounded-lg bg-emerald-500/20 flex items-center justify-center flex-shrink-0">
                  <svg class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="guideline.icon" />
                  </svg>
                </div>
                <div class="flex-1">
                  <h3 class="text-lg font-semibold text-white mb-2">{{ guideline.title }}</h3>
                  <p class="text-white/70 mb-4">{{ guideline.content }}</p>

                  <div class="grid md:grid-cols-2 gap-4">
                    <div>
                      <h4 class="text-sm font-medium text-emerald-400 mb-2 flex items-center gap-1">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                        </svg>
                        Do
                      </h4>
                      <ul class="text-sm text-white/60 space-y-1">
                        <li v-for="item in guideline.dos" :key="item" class="flex items-start gap-1.5">
                          <span class="w-1 h-1 rounded-full bg-emerald-400 mt-2 flex-shrink-0"></span>
                          {{ item }}
                        </li>
                      </ul>
                    </div>
                    <div>
                      <h4 class="text-sm font-medium text-red-400 mb-2 flex items-center gap-1">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                        </svg>
                        Don't
                      </h4>
                      <ul class="text-sm text-white/60 space-y-1">
                        <li v-for="item in guideline.donts" :key="item" class="flex items-start gap-1.5">
                          <span class="w-1 h-1 rounded-full bg-red-400 mt-2 flex-shrink-0"></span>
                          {{ item }}
                        </li>
                      </ul>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Best Practices Section -->
        <section id="best-practices" class="mb-12 scroll-mt-6">
          <h2 class="text-xl font-semibold text-white mb-4 flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-amber-500/20 flex items-center justify-center">
              <svg class="w-4 h-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
              </svg>
            </span>
            Best Practices
          </h2>

          <div class="grid md:grid-cols-2 gap-4">
            <div class="orbitos-glass-subtle rounded-xl p-5">
              <h3 class="font-semibold text-white mb-3 flex items-center gap-2">
                <span class="w-6 h-6 rounded bg-blue-500/20 flex items-center justify-center text-xs text-blue-400">1</span>
                Walk the Process
              </h3>
              <p class="text-sm text-white/60">
                Physically observe the work being done. Don't rely on what people say happens - watch what actually happens.
                You'll discover undocumented steps, workarounds, and improvement opportunities.
              </p>
            </div>

            <div class="orbitos-glass-subtle rounded-xl p-5">
              <h3 class="font-semibold text-white mb-3 flex items-center gap-2">
                <span class="w-6 h-6 rounded bg-blue-500/20 flex items-center justify-center text-xs text-blue-400">2</span>
                Involve the Doers
              </h3>
              <p class="text-sm text-white/60">
                Include the people who actually perform the work. They know the real steps, the shortcuts, and where problems occur.
                Their buy-in also helps when implementing improvements.
              </p>
            </div>

            <div class="orbitos-glass-subtle rounded-xl p-5">
              <h3 class="font-semibold text-white mb-3 flex items-center gap-2">
                <span class="w-6 h-6 rounded bg-blue-500/20 flex items-center justify-center text-xs text-blue-400">3</span>
                Map Current State First
              </h3>
              <p class="text-sm text-white/60">
                Document how work happens today before designing the ideal future state.
                Use "Current" state type for as-is processes, then create "Target" state for improvements.
              </p>
            </div>

            <div class="orbitos-glass-subtle rounded-xl p-5">
              <h3 class="font-semibold text-white mb-3 flex items-center gap-2">
                <span class="w-6 h-6 rounded bg-blue-500/20 flex items-center justify-center text-xs text-blue-400">4</span>
                Use Subprocesses
              </h3>
              <p class="text-sm text-white/60">
                Break complex activities into linked subprocesses. This keeps the main flow readable while preserving detail.
                Link activities to subprocesses for drill-down navigation.
              </p>
            </div>

            <div class="orbitos-glass-subtle rounded-xl p-5">
              <h3 class="font-semibold text-white mb-3 flex items-center gap-2">
                <span class="w-6 h-6 rounded bg-blue-500/20 flex items-center justify-center text-xs text-blue-400">5</span>
                Track Metrics
              </h3>
              <p class="text-sm text-white/60">
                Add durations, pass rates, and other metrics to activities.
                This data enables cycle time analysis and helps prioritize improvement efforts.
              </p>
            </div>

            <div class="orbitos-glass-subtle rounded-xl p-5">
              <h3 class="font-semibold text-white mb-3 flex items-center gap-2">
                <span class="w-6 h-6 rounded bg-blue-500/20 flex items-center justify-center text-xs text-blue-400">6</span>
                Review Regularly
              </h3>
              <p class="text-sm text-white/60">
                Processes evolve. Schedule periodic reviews to keep documentation current.
                Outdated maps are worse than no maps - they create false confidence.
              </p>
            </div>
          </div>
        </section>

        <!-- Common Mistakes Section -->
        <section id="common-mistakes" class="mb-12 scroll-mt-6">
          <h2 class="text-xl font-semibold text-white mb-4 flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-red-500/20 flex items-center justify-center">
              <svg class="w-4 h-4 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </span>
            Common Mistakes to Avoid
          </h2>

          <div class="orbitos-glass-subtle rounded-xl overflow-hidden">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-red-500/10 border-b border-white/10">
                  <th class="text-left p-4 text-red-300 font-medium">Mistake</th>
                  <th class="text-left p-4 text-red-300 font-medium">Why It's a Problem</th>
                  <th class="text-left p-4 text-emerald-300 font-medium">Better Approach</th>
                </tr>
              </thead>
              <tbody class="text-white/70">
                <tr class="border-b border-white/5">
                  <td class="p-4 font-medium text-white">Mapping the ideal, not the real</td>
                  <td class="p-4">You can't improve what you don't accurately understand</td>
                  <td class="p-4">Document current state first, then design target state</td>
                </tr>
                <tr class="border-b border-white/5">
                  <td class="p-4 font-medium text-white">Too much detail at once</td>
                  <td class="p-4">Overwhelming diagrams that no one reads</td>
                  <td class="p-4">Start high-level, drill down with subprocesses</td>
                </tr>
                <tr class="border-b border-white/5">
                  <td class="p-4 font-medium text-white">Missing exception paths</td>
                  <td class="p-4">Errors and edge cases cause the most problems</td>
                  <td class="p-4">Map the "unhappy paths" - what happens when things fail</td>
                </tr>
                <tr class="border-b border-white/5">
                  <td class="p-4 font-medium text-white">Vague activity names</td>
                  <td class="p-4">"Process order" tells you nothing specific</td>
                  <td class="p-4">Use verb + noun: "Validate customer credit"</td>
                </tr>
                <tr class="border-b border-white/5">
                  <td class="p-4 font-medium text-white">No resource assignments</td>
                  <td class="p-4">Can't analyze workload or identify bottlenecks</td>
                  <td class="p-4">Assign every activity to a role or system</td>
                </tr>
                <tr>
                  <td class="p-4 font-medium text-white">Mapping and forgetting</td>
                  <td class="p-4">Outdated documentation misleads users</td>
                  <td class="p-4">Schedule quarterly reviews, update as changes occur</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <!-- Quick Reference Card -->
        <section class="mb-12">
          <div class="orbitos-glass-subtle rounded-xl p-6 border-2 border-purple-500/30">
            <h3 class="text-lg font-semibold text-purple-300 mb-4 flex items-center gap-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
              Quick Start Checklist
            </h3>
            <div class="grid md:grid-cols-2 gap-x-8 gap-y-2 text-sm text-white/70">
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>Define the trigger (what starts the process)</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>Define the output (what you deliver)</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>List the major steps (5-15 activities)</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>Identify decision points</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>Assign resources to each step</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>Estimate durations</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>Map exception/error paths</span>
              </label>
              <label class="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" class="rounded border-white/20 bg-white/5 text-purple-500">
                <span>Review with stakeholders</span>
              </label>
            </div>
          </div>
        </section>

        <!-- Footer -->
        <div class="text-center text-white/40 text-sm pb-8">
          <p>Need help? Click the <span class="text-purple-400">?</span> button on the right edge for contextual assistance.</p>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
/* Smooth scroll behavior */
html {
  scroll-behavior: smooth;
}

/* Custom checkbox styling */
input[type="checkbox"] {
  appearance: none;
  -webkit-appearance: none;
  width: 1rem;
  height: 1rem;
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 0.25rem;
  background: rgba(255, 255, 255, 0.05);
  cursor: pointer;
  position: relative;
}

input[type="checkbox"]:checked {
  background: rgb(168, 85, 247);
  border-color: rgb(168, 85, 247);
}

input[type="checkbox"]:checked::after {
  content: '';
  position: absolute;
  left: 5px;
  top: 2px;
  width: 4px;
  height: 8px;
  border: solid white;
  border-width: 0 2px 2px 0;
  transform: rotate(45deg);
}
</style>
