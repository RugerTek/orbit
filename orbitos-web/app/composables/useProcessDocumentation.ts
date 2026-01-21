import type {
  ProcessWithActivities,
  ActivityWithDetails,
  DrillDownLevel,
  DocumentExportOptions,
  ProcessDocumentNode,
} from '~/types/operations'

/**
 * Composable for generating ISO 9001:2015 aligned process documentation
 * with hierarchical subprocess drill-down support.
 */
export function useProcessDocumentation() {
  const { fetchProcessById } = useOperations()

  /**
   * Load a process tree with subprocesses up to the specified depth
   */
  async function loadProcessTree(
    processId: string,
    maxDepth: DrillDownLevel,
    currentDepth: number = 0,
    visited: Set<string> = new Set()
  ): Promise<ProcessDocumentNode | null> {
    // Prevent circular references
    if (visited.has(processId)) {
      console.warn(`Circular subprocess reference detected: ${processId}`)
      return null
    }
    visited.add(processId)

    const process = await fetchProcessById(processId)
    if (!process) return null

    // Check if we've reached max depth
    const atMaxDepth = maxDepth === 'full' ? false : currentDepth >= maxDepth

    if (atMaxDepth) {
      return { process, subprocesses: [], depth: currentDepth }
    }

    // Load subprocesses for activities that have linkedProcessId
    const subprocesses: ProcessDocumentNode[] = []
    for (const activity of process.activities) {
      if (activity.linkedProcessId) {
        const subNode = await loadProcessTree(
          activity.linkedProcessId,
          maxDepth,
          currentDepth + 1,
          visited
        )
        if (subNode) {
          subprocesses.push(subNode)
        }
      }
    }

    return { process, subprocesses, depth: currentDepth }
  }

  /**
   * Format activity type for display
   */
  function formatActivityType(type: string): string {
    const typeMap: Record<string, string> = {
      manual: 'Manual',
      automated: 'Automated',
      hybrid: 'Hybrid',
      decision: 'Decision Point',
      handoff: 'Handoff',
    }
    return typeMap[type] || type
  }

  /**
   * Format frequency for display
   */
  function formatFrequency(frequency?: string): string {
    if (!frequency) return 'Not specified'
    const freqMap: Record<string, string> = {
      daily: 'Daily',
      weekly: 'Weekly',
      monthly: 'Monthly',
      on_demand: 'On Demand',
      continuous: 'Continuous',
    }
    return freqMap[frequency] || frequency
  }

  /**
   * Format status for display
   */
  function formatStatus(status: string): string {
    const statusMap: Record<string, string> = {
      draft: 'Draft',
      active: 'Active',
      deprecated: 'Deprecated',
    }
    return statusMap[status] || status
  }

  /**
   * Format state type for display
   */
  function formatStateType(stateType: string): string {
    return stateType === 'current' ? 'Current State' : 'Target State'
  }

  /**
   * Generate markdown for a single process (non-recursive section)
   */
  function generateProcessSection(
    process: ProcessWithActivities,
    options: DocumentExportOptions,
    sectionPrefix: string = '',
    headingLevel: number = 1
  ): string {
    const h = (level: number) => '#'.repeat(Math.min(level, 6))
    const lines: string[] = []

    // Process title
    if (sectionPrefix) {
      lines.push(`${h(headingLevel)} ${sectionPrefix} ${process.name}`)
    } else {
      lines.push(`${h(headingLevel)} Process Documentation: ${process.name}`)
    }
    lines.push('')

    // Document control table (only for main process)
    if (!sectionPrefix) {
      lines.push('**Document Control**')
      lines.push('')
      lines.push('| Field | Value |')
      lines.push('|-------|-------|')
      lines.push(`| Process ID | \`${process.id}\` |`)
      lines.push(`| Status | ${formatStatus(process.status)} |`)
      lines.push(`| State Type | ${formatStateType(process.stateType)} |`)
      lines.push(`| Owner | ${process.owner?.name || 'Not assigned'} |`)
      lines.push(`| Generated | ${new Date().toISOString().split('T')[0]} |`)
      lines.push('')
      lines.push('---')
      lines.push('')
    }

    // Purpose and Scope
    lines.push(`${h(headingLevel + 1)} Purpose and Scope`)
    lines.push('')
    if (process.purpose) {
      lines.push(`**Purpose:** ${process.purpose}`)
      lines.push('')
    }
    if (process.description) {
      lines.push(process.description)
      lines.push('')
    }
    if (!process.purpose && !process.description) {
      lines.push('*No purpose or description defined.*')
      lines.push('')
    }

    // Trigger / Input Conditions
    lines.push(`${h(headingLevel + 1)} Trigger / Input Conditions`)
    lines.push('')
    lines.push(process.trigger || '*No trigger defined.*')
    lines.push('')

    // Output / Deliverables
    lines.push(`${h(headingLevel + 1)} Output / Deliverables`)
    lines.push('')
    lines.push(process.output || '*No output defined.*')
    lines.push('')

    // Process Flow
    lines.push(`${h(headingLevel + 1)} Process Flow`)
    lines.push('')
    lines.push(`**Frequency:** ${formatFrequency(process.frequency)}`)
    lines.push('')
    lines.push(`**Total Activities:** ${process.activities.length}`)
    lines.push('')

    // Activity Sequence Table
    if (process.activities.length > 0) {
      const sortedActivities = [...process.activities].sort((a, b) => a.order - b.order)

      lines.push(`${h(headingLevel + 2)} Activity Sequence`)
      lines.push('')
      lines.push('| Step | Activity | Type | Duration | Assigned To | Subprocess |')
      lines.push('|------|----------|------|----------|-------------|------------|')

      sortedActivities.forEach((activity, index) => {
        const duration = activity.estimatedDuration
          ? `${activity.estimatedDuration} min`
          : '-'
        const assignee = activity.assignedResource?.name || '-'
        const subprocess = activity.linkedProcess?.name || '-'
        lines.push(
          `| ${index + 1} | ${activity.name} | ${formatActivityType(activity.activityType)} | ${duration} | ${assignee} | ${subprocess} |`
        )
      })
      lines.push('')

      // Activity Details
      if (options.includeInstructions) {
        lines.push(`${h(headingLevel + 2)} Activity Details`)
        lines.push('')

        sortedActivities.forEach((activity, index) => {
          lines.push(`${h(headingLevel + 3)} Step ${index + 1}: ${activity.name}`)
          lines.push('')
          lines.push(`- **Type:** ${formatActivityType(activity.activityType)}`)

          if (activity.description) {
            lines.push(`- **Description:** ${activity.description}`)
          }

          if (activity.estimatedDuration) {
            lines.push(`- **Estimated Duration:** ${activity.estimatedDuration} minutes`)
          }

          if (activity.assignedResource) {
            lines.push(`- **Assigned To:** ${activity.assignedResource.name}`)
          }

          if (activity.function) {
            lines.push(`- **Linked Function:** ${activity.function.name}`)
          }

          if (activity.linkedProcess) {
            lines.push(
              `- **Subprocess:** ${activity.linkedProcess.name} *(see subprocess section)*`
            )
          }

          if (activity.instructions) {
            lines.push('')
            lines.push('**Instructions:**')
            lines.push('')
            lines.push(activity.instructions)
          }

          lines.push('')
        })
      }
    } else {
      lines.push('*No activities defined for this process.*')
      lines.push('')
    }

    return lines.join('\n')
  }

  /**
   * Generate full markdown documentation for a process tree
   */
  function generateMarkdown(
    node: ProcessDocumentNode,
    options: DocumentExportOptions
  ): string {
    const sections: string[] = []

    // Main process section
    sections.push(generateProcessSection(node.process, options))

    // Subprocesses section
    if (node.subprocesses.length > 0) {
      sections.push('---')
      sections.push('')
      sections.push('## Subprocesses')
      sections.push('')

      node.subprocesses.forEach((subNode, index) => {
        sections.push(
          generateSubprocessSection(subNode, options, `${index + 1}`, 3)
        )
      })
    }

    // Revision history (only for main document)
    sections.push('---')
    sections.push('')
    sections.push('## Revision History')
    sections.push('')
    sections.push('| Version | Date | Changes |')
    sections.push('|---------|------|---------|')
    sections.push(`| 1.0 | ${new Date().toISOString().split('T')[0]} | Auto-generated documentation |`)
    sections.push('')
    sections.push('---')
    sections.push('')
    sections.push('*Generated by OrbitOS Process Documentation*')

    return sections.join('\n')
  }

  /**
   * Generate markdown for a subprocess (recursive)
   */
  function generateSubprocessSection(
    node: ProcessDocumentNode,
    options: DocumentExportOptions,
    sectionNumber: string,
    headingLevel: number
  ): string {
    const sections: string[] = []

    // This subprocess
    sections.push(
      generateProcessSection(
        node.process,
        options,
        `${sectionNumber}.`,
        headingLevel
      )
    )

    // Nested subprocesses
    if (node.subprocesses.length > 0) {
      node.subprocesses.forEach((subNode, index) => {
        sections.push(
          generateSubprocessSection(
            subNode,
            options,
            `${sectionNumber}.${index + 1}`,
            headingLevel + 1
          )
        )
      })
    }

    return sections.join('\n')
  }

  /**
   * Generate HTML document with styling
   */
  function generateHtml(
    node: ProcessDocumentNode,
    options: DocumentExportOptions
  ): string {
    const markdown = generateMarkdown(node, options)

    // Convert markdown to basic HTML
    const htmlContent = markdownToHtml(markdown)

    return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Process Documentation: ${node.process.name}</title>
  <style>
    :root {
      --primary: #8b5cf6;
      --primary-dark: #7c3aed;
      --bg: #0f0a1a;
      --surface: #1a1425;
      --text: #e2e8f0;
      --text-muted: #94a3b8;
      --border: #2d2640;
    }

    * {
      box-sizing: border-box;
    }

    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
      line-height: 1.6;
      color: var(--text);
      background: var(--bg);
      max-width: 900px;
      margin: 0 auto;
      padding: 2rem;
    }

    h1 {
      color: var(--primary);
      border-bottom: 2px solid var(--primary);
      padding-bottom: 0.5rem;
      margin-top: 2rem;
    }

    h2 {
      color: var(--text);
      border-bottom: 1px solid var(--border);
      padding-bottom: 0.25rem;
      margin-top: 2rem;
    }

    h3 {
      color: var(--text);
      margin-top: 1.5rem;
    }

    h4, h5, h6 {
      color: var(--text-muted);
      margin-top: 1rem;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      margin: 1rem 0;
      background: var(--surface);
      border-radius: 8px;
      overflow: hidden;
    }

    th, td {
      padding: 0.75rem 1rem;
      text-align: left;
      border-bottom: 1px solid var(--border);
    }

    th {
      background: var(--primary);
      color: white;
      font-weight: 600;
    }

    tr:last-child td {
      border-bottom: none;
    }

    tr:hover {
      background: rgba(139, 92, 246, 0.1);
    }

    code {
      background: var(--surface);
      padding: 0.125rem 0.375rem;
      border-radius: 4px;
      font-family: 'Fira Code', 'Consolas', monospace;
      font-size: 0.9em;
      color: var(--primary);
    }

    hr {
      border: none;
      border-top: 1px solid var(--border);
      margin: 2rem 0;
    }

    strong {
      color: var(--text);
    }

    em {
      color: var(--text-muted);
    }

    ul, ol {
      padding-left: 1.5rem;
    }

    li {
      margin: 0.25rem 0;
    }

    blockquote {
      border-left: 4px solid var(--primary);
      padding-left: 1rem;
      margin: 1rem 0;
      color: var(--text-muted);
      background: var(--surface);
      padding: 1rem;
      border-radius: 0 8px 8px 0;
    }

    @media print {
      body {
        background: white;
        color: #1a1a1a;
        max-width: none;
        padding: 0;
      }

      h1 { color: #4c1d95; }
      h2, h3 { color: #1a1a1a; }

      table {
        background: white;
      }

      th {
        background: #4c1d95;
      }

      tr:hover {
        background: transparent;
      }

      code {
        background: #f3f4f6;
        color: #4c1d95;
      }

      blockquote {
        background: #f9fafb;
      }
    }
  </style>
</head>
<body>
${htmlContent}
</body>
</html>`
  }

  /**
   * Basic markdown to HTML converter
   */
  function markdownToHtml(markdown: string): string {
    let html = markdown

    // Escape HTML entities first
    html = html
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')

    // Headers (process in order from h6 to h1 to avoid conflicts)
    html = html.replace(/^######\s+(.+)$/gm, '<h6>$1</h6>')
    html = html.replace(/^#####\s+(.+)$/gm, '<h5>$1</h5>')
    html = html.replace(/^####\s+(.+)$/gm, '<h4>$1</h4>')
    html = html.replace(/^###\s+(.+)$/gm, '<h3>$1</h3>')
    html = html.replace(/^##\s+(.+)$/gm, '<h2>$1</h2>')
    html = html.replace(/^#\s+(.+)$/gm, '<h1>$1</h1>')

    // Bold and italic
    html = html.replace(/\*\*\*(.+?)\*\*\*/g, '<strong><em>$1</em></strong>')
    html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    html = html.replace(/\*(.+?)\*/g, '<em>$1</em>')

    // Inline code
    html = html.replace(/`([^`]+)`/g, '<code>$1</code>')

    // Horizontal rules
    html = html.replace(/^---$/gm, '<hr>')

    // Tables
    html = html.replace(
      /^\|(.+)\|$/gm,
      (match, content) => {
        const cells = content.split('|').map((c: string) => c.trim())
        return `<tr>${cells.map((c: string) => `<td>${c}</td>`).join('')}</tr>`
      }
    )

    // Detect table header rows (with --- separators) and wrap tables
    const tableRegex = /<tr>(<td>-+<\/td>)+<\/tr>/g
    html = html.replace(tableRegex, '')

    // Wrap consecutive tr elements in table
    html = html.replace(
      /(<tr>[\s\S]*?<\/tr>)(\s*<tr>[\s\S]*?<\/tr>)*/g,
      (match) => {
        // Check if first row should be header
        const rows = match.match(/<tr>[\s\S]*?<\/tr>/g) || []
        if (rows.length > 0) {
          // Make first row a header
          const headerRow = rows[0].replace(/<td>/g, '<th>').replace(/<\/td>/g, '</th>')
          const bodyRows = rows.slice(1).join('\n')
          return `<table>\n<thead>${headerRow}</thead>\n<tbody>${bodyRows}</tbody>\n</table>`
        }
        return `<table>${match}</table>`
      }
    )

    // Lists (simple implementation)
    html = html.replace(/^- (.+)$/gm, '<li>$1</li>')
    html = html.replace(/(<li>[\s\S]*?<\/li>)(\s*<li>[\s\S]*?<\/li>)*/g, '<ul>$&</ul>')

    // Paragraphs - wrap remaining text blocks
    const blocks = html.split(/\n\n+/)
    html = blocks
      .map(block => {
        block = block.trim()
        if (!block) return ''
        if (block.startsWith('<')) return block
        return `<p>${block.replace(/\n/g, '<br>')}</p>`
      })
      .join('\n\n')

    return html
  }

  /**
   * Download document as file
   */
  function downloadDocument(
    content: string,
    filename: string,
    format: 'md' | 'html'
  ): void {
    const mimeType = format === 'html' ? 'text/html' : 'text/markdown'
    const blob = new Blob([content], { type: mimeType })
    const url = URL.createObjectURL(blob)

    const a = document.createElement('a')
    a.href = url
    a.download = `${filename}.${format}`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)

    URL.revokeObjectURL(url)
  }

  /**
   * Get drill-down level description
   */
  function getDrillDownDescription(level: DrillDownLevel): string {
    const descriptions: Record<DrillDownLevel, string> = {
      0: 'This process only',
      1: 'Include immediate subprocesses',
      2: 'Two levels deep',
      3: 'Three levels deep',
      full: 'All nested subprocesses',
    }
    return descriptions[level]
  }

  return {
    loadProcessTree,
    generateMarkdown,
    generateHtml,
    downloadDocument,
    getDrillDownDescription,
    formatActivityType,
    formatFrequency,
    formatStatus,
    formatStateType,
  }
}
