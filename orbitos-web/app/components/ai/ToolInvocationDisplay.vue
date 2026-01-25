<script setup lang="ts">
interface ToolInvocation {
  name: string
  parameters: Record<string, any>
  isExpanded: boolean
}

const props = defineProps<{
  content: string
}>()

// Parse <invoke> tags from content
const parsed = computed(() => {
  const invocations: ToolInvocation[] = []
  let textParts: string[] = []
  let lastIndex = 0

  // Regex to match <invoke name="...">...</invoke> or self-closing
  const invokeRegex = /<invoke\s+name="([^"]+)">([\s\S]*?)<\/invoke>|<invoke\s+name="([^"]+)"\s*\/>/g
  let match

  const content = props.content || ''

  while ((match = invokeRegex.exec(content)) !== null) {
    // Add text before this match
    if (match.index > lastIndex) {
      textParts.push(content.substring(lastIndex, match.index))
    }

    const toolName = match[1] || match[3]
    const paramsContent = match[2] || ''

    // Parse parameters from <parameter name="...">value</parameter>
    const params: Record<string, any> = {}
    const paramRegex = /<parameter\s+name="([^"]+)">([\s\S]*?)<\/parameter>/g
    let paramMatch

    while ((paramMatch = paramRegex.exec(paramsContent)) !== null) {
      const paramName = paramMatch[1]
      let paramValue = paramMatch[2].trim()

      // Try to parse JSON values
      if (paramValue.startsWith('[') || paramValue.startsWith('{')) {
        try {
          params[paramName] = JSON.parse(paramValue)
        } catch {
          params[paramName] = paramValue
        }
      } else {
        params[paramName] = paramValue
      }
    }

    invocations.push({
      name: toolName,
      parameters: params,
      isExpanded: false
    })

    // Add placeholder for invocation
    textParts.push(`__INVOKE_${invocations.length - 1}__`)
    lastIndex = match.index + match[0].length
  }

  // Add remaining text
  if (lastIndex < content.length) {
    textParts.push(content.substring(lastIndex))
  }

  return {
    invocations,
    textParts,
    hasInvocations: invocations.length > 0
  }
})

// Track expanded state locally
const expandedStates = ref<boolean[]>([])

watch(() => parsed.value.invocations.length, (count) => {
  expandedStates.value = new Array(count).fill(false)
}, { immediate: true })

function toggleExpanded(index: number) {
  expandedStates.value[index] = !expandedStates.value[index]
}

// Get friendly tool name
function getToolDisplayName(toolName: string): string {
  const names: Record<string, string> = {
    'get_processes': 'Get Processes',
    'get_functions': 'Get Functions',
    'get_roles': 'Get Roles',
    'get_resources': 'Get Resources',
    'get_people': 'Get People',
    'get_activities': 'Get Activities',
    'get_goals': 'Get Goals',
    'create_process': 'Create Process',
    'create_function': 'Create Function',
    'create_functions': 'Create Functions',
    'create_role': 'Create Role',
    'create_resource': 'Create Resource',
    'create_activity': 'Create Activity',
    'create_goal': 'Create Goal',
    'update_process': 'Update Process',
    'update_function': 'Update Function',
    'update_role': 'Update Role',
    'update_resource': 'Update Resource',
    'delete_process': 'Delete Process',
    'delete_function': 'Delete Function',
    'delete_role': 'Delete Role',
  }
  return names[toolName] || toolName.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())
}

// Get tool icon
function getToolIcon(toolName: string): string {
  if (toolName.startsWith('get_')) return '🔍'
  if (toolName.startsWith('create_')) return '➕'
  if (toolName.startsWith('update_')) return '✏️'
  if (toolName.startsWith('delete_')) return '🗑️'
  return '⚡'
}

// Format parameter value for display
function formatParamValue(value: any): string {
  if (Array.isArray(value)) {
    return `${value.length} items`
  }
  if (typeof value === 'object' && value !== null) {
    return JSON.stringify(value, null, 2)
  }
  return String(value)
}

// Check if value is complex (array or object)
function isComplexValue(value: any): boolean {
  return Array.isArray(value) || (typeof value === 'object' && value !== null)
}
</script>

<template>
  <div class="tool-invocation-display">
    <template v-for="(part, idx) in parsed.textParts" :key="idx">
      <!-- Regular text -->
      <template v-if="!part.startsWith('__INVOKE_')">
        <span class="whitespace-pre-wrap">{{ part }}</span>
      </template>

      <!-- Tool invocation card -->
      <template v-else>
        <div
          class="tool-card my-3 rounded-lg border border-purple-500/30 bg-purple-500/10 overflow-hidden"
        >
          <!-- Header - always visible -->
          <button
            class="w-full flex items-center gap-3 px-3 py-2 hover:bg-purple-500/10 transition-colors text-left"
            @click="toggleExpanded(parseInt(part.replace('__INVOKE_', '').replace('__', '')))"
          >
            <span class="text-lg">
              {{ getToolIcon(parsed.invocations[parseInt(part.replace('__INVOKE_', '').replace('__', ''))].name) }}
            </span>
            <span class="flex-1 font-medium text-purple-300">
              {{ getToolDisplayName(parsed.invocations[parseInt(part.replace('__INVOKE_', '').replace('__', ''))].name) }}
            </span>
            <span class="text-white/40 text-xs">
              {{ Object.keys(parsed.invocations[parseInt(part.replace('__INVOKE_', '').replace('__', ''))].parameters).length }} params
            </span>
            <svg
              :class="[
                'w-4 h-4 text-white/40 transition-transform',
                expandedStates[parseInt(part.replace('__INVOKE_', '').replace('__', ''))] ? 'rotate-180' : ''
              ]"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          <!-- Expanded content -->
          <div
            v-if="expandedStates[parseInt(part.replace('__INVOKE_', '').replace('__', ''))]"
            class="border-t border-purple-500/20 p-3 bg-black/20"
          >
            <div
              v-for="(value, key) in parsed.invocations[parseInt(part.replace('__INVOKE_', '').replace('__', ''))].parameters"
              :key="key"
              class="mb-3 last:mb-0"
            >
              <div class="text-xs text-purple-400 uppercase tracking-wide mb-1">{{ key }}</div>

              <!-- Simple values -->
              <div v-if="!isComplexValue(value)" class="text-sm text-white/80">
                {{ value }}
              </div>

              <!-- Array of objects (like functions list) -->
              <div v-else-if="Array.isArray(value)" class="space-y-2">
                <div
                  v-for="(item, itemIdx) in value"
                  :key="itemIdx"
                  class="bg-white/5 rounded-lg p-2 text-sm"
                >
                  <template v-if="typeof item === 'object'">
                    <div v-for="(itemValue, itemKey) in item" :key="itemKey" class="flex gap-2">
                      <span class="text-white/40 min-w-[80px]">{{ itemKey }}:</span>
                      <span class="text-white/80">{{ itemValue }}</span>
                    </div>
                  </template>
                  <template v-else>
                    {{ item }}
                  </template>
                </div>
              </div>

              <!-- Object -->
              <div v-else class="bg-white/5 rounded-lg p-2 text-sm">
                <div v-for="(objValue, objKey) in value" :key="objKey" class="flex gap-2">
                  <span class="text-white/40 min-w-[80px]">{{ objKey }}:</span>
                  <span class="text-white/80">{{ objValue }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </template>
  </div>
</template>

<style scoped>
.tool-invocation-display {
  line-height: 1.6;
}

.tool-card {
  display: block;
}
</style>
