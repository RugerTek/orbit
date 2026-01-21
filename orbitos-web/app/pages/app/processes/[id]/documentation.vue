<script setup lang="ts">
import type { DrillDownLevel, ProcessDocumentNode, DocumentExportOptions } from '~/types/operations'

definePageMeta({
  layout: 'app'
})

const route = useRoute()
const processId = computed(() => route.params.id as string)

const {
  loadProcessTree,
  generateMarkdown,
  generateHtml,
} = useProcessDocumentation()

// State
const isLoading = ref(false)
const processTree = ref<ProcessDocumentNode | null>(null)
const drillDownLevel = ref<DrillDownLevel>(0)
const renderedHtml = ref('')
const processName = ref('')

// Drill-down options
const drillDownOptions: { value: DrillDownLevel; label: string; description: string }[] = [
  { value: 0, label: 'This Process', description: 'Only this process, no subprocesses' },
  { value: 1, label: 'Level 1', description: 'Include immediate subprocesses' },
  { value: 2, label: 'Level 2', description: 'Two levels of subprocesses' },
  { value: 3, label: 'Level 3', description: 'Three levels deep' },
  { value: 'full', label: 'Full', description: 'All nested subprocesses' },
]

// Load process tree when drill-down level changes
async function loadDocumentation() {
  isLoading.value = true
  try {
    processTree.value = await loadProcessTree(processId.value, drillDownLevel.value)
    if (processTree.value) {
      processName.value = processTree.value.process.name
      const options: DocumentExportOptions = {
        drillDownLevel: drillDownLevel.value,
        format: 'html',
        includeInstructions: true,
      }
      const html = generateHtml(processTree.value, options)
      // Extract just the body content for inline rendering
      const bodyMatch = html.match(/<body>([\s\S]*)<\/body>/)
      renderedHtml.value = bodyMatch ? bodyMatch[1] : ''
    }
  } catch (error) {
    console.error('Failed to load process documentation:', error)
  } finally {
    isLoading.value = false
  }
}

// Select drill-down level
function selectLevel(level: DrillDownLevel) {
  drillDownLevel.value = level
  loadDocumentation()
}

// Initial load
onMounted(() => {
  loadDocumentation()
})

// Export to PDF via print
function exportToPdf() {
  window.print()
}

// Count subprocesses for display
const subprocessCount = computed(() => {
  if (!processTree.value) return 0
  return countSubprocesses(processTree.value)
})

function countSubprocesses(node: ProcessDocumentNode): number {
  let count = node.subprocesses.length
  for (const sub of node.subprocesses) {
    count += countSubprocesses(sub)
  }
  return count
}
</script>

<template>
  <div class="min-h-screen bg-[#0f0a1a]">
    <!-- Header - hidden when printing -->
    <div class="print:hidden sticky top-0 z-10 bg-[#0f0a1a]/95 backdrop-blur border-b border-white/10">
      <div class="max-w-6xl mx-auto px-6 py-4">
        <!-- Top row: Back + Title + Export -->
        <div class="flex items-center justify-between mb-4">
          <div class="flex items-center gap-4">
            <NuxtLink
              :to="`/app/processes/${processId}`"
              class="flex items-center gap-2 text-white/60 hover:text-white transition-colors"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
              </svg>
              <span>Back</span>
            </NuxtLink>

            <div class="h-6 w-px bg-white/20" />

            <div>
              <h1 class="text-lg font-semibold text-white">Process Documentation</h1>
              <p v-if="processName" class="text-sm text-white/50">{{ processName }}</p>
            </div>
          </div>

          <button
            @click="exportToPdf"
            :disabled="isLoading || !processTree"
            class="flex items-center gap-2 px-5 py-2.5 bg-purple-500 hover:bg-purple-600 disabled:opacity-50 disabled:cursor-not-allowed text-white font-medium rounded-xl transition-colors"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            Export to PDF
          </button>
        </div>

        <!-- Level selector -->
        <div class="flex items-center gap-2">
          <span class="text-sm text-white/60 mr-2">Depth:</span>
          <div class="flex gap-1 bg-white/5 p-1 rounded-xl">
            <button
              v-for="option in drillDownOptions"
              :key="option.value"
              @click="selectLevel(option.value)"
              :class="[
                'px-4 py-2 text-sm font-medium rounded-lg transition-all',
                drillDownLevel === option.value
                  ? 'bg-purple-500 text-white shadow-lg'
                  : 'text-white/60 hover:text-white hover:bg-white/5'
              ]"
              :title="option.description"
            >
              {{ option.label }}
            </button>
          </div>

          <!-- Subprocess count indicator -->
          <div v-if="processTree && !isLoading" class="ml-4 text-sm text-white/40">
            <span v-if="subprocessCount > 0">
              {{ subprocessCount }} subprocess{{ subprocessCount !== 1 ? 'es' : '' }} included
            </span>
            <span v-else>No subprocesses</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Document content -->
    <div class="max-w-4xl mx-auto px-6 py-8">
      <!-- Loading state -->
      <div v-if="isLoading" class="flex items-center justify-center py-20">
        <div class="flex flex-col items-center gap-4">
          <div class="w-10 h-10 border-4 border-purple-500/30 border-t-purple-500 rounded-full animate-spin" />
          <p class="text-white/60">Loading documentation...</p>
        </div>
      </div>

      <!-- Error state -->
      <div v-else-if="!processTree" class="flex flex-col items-center justify-center py-20">
        <div class="w-16 h-16 rounded-full bg-red-500/10 flex items-center justify-center mb-4">
          <svg class="w-8 h-8 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <h2 class="text-xl font-semibold text-white mb-2">Process not found</h2>
        <p class="text-white/60 mb-6">The process could not be loaded.</p>
        <NuxtLink
          to="/app/processes"
          class="px-4 py-2 bg-purple-500 hover:bg-purple-600 text-white rounded-lg transition-colors"
        >
          Back to Processes
        </NuxtLink>
      </div>

      <!-- Rendered documentation -->
      <div
        v-else
        class="process-documentation bg-white/[0.02] border border-white/10 rounded-2xl p-8 print:bg-transparent print:border-0 print:p-0 print:rounded-none"
        v-html="renderedHtml"
      />
    </div>
  </div>
</template>

<style>
/* Document styles for rendered HTML */
.process-documentation {
  color: #e2e8f0;
}

.process-documentation h1 {
  color: #a78bfa;
  border-bottom: 2px solid #a78bfa;
  padding-bottom: 0.5rem;
  margin-top: 0;
  margin-bottom: 1.5rem;
  font-size: 1.75rem;
  font-weight: 700;
}

.process-documentation h2 {
  color: #e2e8f0;
  border-bottom: 1px solid #2d2640;
  padding-bottom: 0.25rem;
  margin-top: 2rem;
  font-size: 1.375rem;
  font-weight: 600;
}

.process-documentation h3 {
  color: #c4b5fd;
  margin-top: 1.5rem;
  font-size: 1.125rem;
  font-weight: 600;
}

.process-documentation h4,
.process-documentation h5,
.process-documentation h6 {
  color: #94a3b8;
  margin-top: 1rem;
  font-size: 1rem;
  font-weight: 500;
}

.process-documentation table {
  width: 100%;
  border-collapse: collapse;
  margin: 1rem 0;
  background: rgba(139, 92, 246, 0.05);
  border-radius: 8px;
  overflow: hidden;
  font-size: 0.9rem;
}

.process-documentation th,
.process-documentation td {
  padding: 0.625rem 0.875rem;
  text-align: left;
  border-bottom: 1px solid #2d2640;
}

.process-documentation th {
  background: #8b5cf6;
  color: white;
  font-weight: 600;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.process-documentation tr:last-child td {
  border-bottom: none;
}

.process-documentation tbody tr:hover {
  background: rgba(139, 92, 246, 0.08);
}

.process-documentation code {
  background: rgba(139, 92, 246, 0.15);
  padding: 0.125rem 0.375rem;
  border-radius: 4px;
  font-family: 'Fira Code', 'Consolas', monospace;
  font-size: 0.85em;
  color: #c4b5fd;
}

.process-documentation hr {
  border: none;
  border-top: 1px solid #2d2640;
  margin: 2rem 0;
}

.process-documentation strong {
  color: #f1f5f9;
}

.process-documentation em {
  color: #94a3b8;
}

.process-documentation ul,
.process-documentation ol {
  padding-left: 1.25rem;
  margin: 0.75rem 0;
}

.process-documentation li {
  margin: 0.375rem 0;
  line-height: 1.6;
}

.process-documentation p {
  margin: 0.75rem 0;
  line-height: 1.7;
}

/* Print styles */
@media print {
  @page {
    margin: 1.5cm;
    size: A4;
  }

  body {
    background: white !important;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }

  .process-documentation {
    color: #1a1a1a !important;
  }

  .process-documentation h1 {
    color: #5b21b6 !important;
    border-bottom-color: #5b21b6 !important;
    font-size: 1.5rem !important;
  }

  .process-documentation h2 {
    color: #1a1a1a !important;
    border-bottom-color: #e5e7eb !important;
    font-size: 1.25rem !important;
  }

  .process-documentation h3 {
    color: #5b21b6 !important;
    font-size: 1.1rem !important;
  }

  .process-documentation h4,
  .process-documentation h5,
  .process-documentation h6 {
    color: #4b5563 !important;
  }

  .process-documentation table {
    background: white !important;
    border: 1px solid #e5e7eb !important;
  }

  .process-documentation th {
    background: #5b21b6 !important;
  }

  .process-documentation th,
  .process-documentation td {
    border-bottom-color: #e5e7eb !important;
    padding: 0.5rem 0.75rem !important;
  }

  .process-documentation tbody tr:hover {
    background: transparent !important;
  }

  .process-documentation code {
    background: #f3f4f6 !important;
    color: #5b21b6 !important;
  }

  .process-documentation strong {
    color: #1a1a1a !important;
  }

  .process-documentation em {
    color: #4b5563 !important;
  }

  .process-documentation hr {
    border-top-color: #e5e7eb !important;
  }
}
</style>
