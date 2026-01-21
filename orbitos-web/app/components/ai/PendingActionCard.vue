<script setup lang="ts">
import type { PendingAction } from '~/composables/usePendingActions'

const props = defineProps<{
  action: PendingAction
  compact?: boolean
}>()

const emit = defineEmits<{
  approve: [actionId: string]
  reject: [actionId: string]
  viewDetails: [actionId: string]
}>()

const { getStatusColor, getActionTypeColor, parseProposedData, parsePreviousData } = usePendingActions()

const isExpanded = ref(false)

const proposedData = computed(() => parseProposedData(props.action))
const previousData = computed(() => parsePreviousData(props.action))

const isPending = computed(() => props.action.status === 'Pending')

const actionTypeIcon = computed(() => {
  switch (props.action.actionType) {
    case 'Create':
      return '+'
    case 'Update':
      return '✎'
    case 'Delete':
      return '×'
    default:
      return '•'
  }
})

const statusBadgeClasses = computed(() => {
  const color = getStatusColor(props.action.status)
  const baseClasses = 'px-2 py-0.5 rounded-full text-xs font-medium'
  switch (color) {
    case 'warning':
      return `${baseClasses} bg-amber-500/20 text-amber-400`
    case 'success':
      return `${baseClasses} bg-emerald-500/20 text-emerald-400`
    case 'error':
      return `${baseClasses} bg-red-500/20 text-red-400`
    case 'info':
      return `${baseClasses} bg-blue-500/20 text-blue-400`
    default:
      return `${baseClasses} bg-slate-500/20 text-slate-400`
  }
})

const actionTypeBadgeClasses = computed(() => {
  const color = getActionTypeColor(props.action.actionType)
  const baseClasses = 'w-6 h-6 rounded-md flex items-center justify-center text-sm font-bold'
  switch (color) {
    case 'success':
      return `${baseClasses} bg-emerald-500/20 text-emerald-400`
    case 'info':
      return `${baseClasses} bg-blue-500/20 text-blue-400`
    case 'error':
      return `${baseClasses} bg-red-500/20 text-red-400`
    default:
      return `${baseClasses} bg-slate-500/20 text-slate-400`
  }
})

const changedFields = computed(() => {
  if (props.action.actionType !== 'Update' || !previousData.value) return []

  const changes: { field: string; from: unknown; to: unknown }[] = []
  for (const [key, value] of Object.entries(proposedData.value)) {
    const prev = previousData.value[key]
    if (prev !== value) {
      changes.push({ field: key, from: prev, to: value })
    }
  }
  return changes
})
</script>

<template>
  <div class="rounded-lg border border-slate-600/50 bg-slate-700/30 overflow-hidden">
    <!-- Header -->
    <div class="flex items-center justify-between px-3 py-2 bg-slate-700/50">
      <div class="flex items-center gap-2">
        <div :class="actionTypeBadgeClasses">
          {{ actionTypeIcon }}
        </div>
        <div>
          <span class="text-sm font-medium text-white">
            {{ action.actionType }} {{ action.entityType }}
          </span>
          <span v-if="action.entityName" class="text-slate-400 text-sm">
            : {{ action.entityName }}
          </span>
        </div>
      </div>
      <span :class="statusBadgeClasses">
        {{ action.status }}
      </span>
    </div>

    <!-- Content -->
    <div class="px-3 py-2 space-y-2">
      <!-- Reason -->
      <p class="text-xs text-slate-400 italic">
        "{{ action.reason }}"
      </p>

      <!-- Proposed Data Preview (collapsed) -->
      <div v-if="!isExpanded && !compact" class="text-xs text-slate-300">
        <template v-if="action.actionType === 'Create'">
          <span class="text-emerald-400">Creating:</span>
          {{ proposedData.name || Object.values(proposedData)[0] }}
        </template>
        <template v-else-if="action.actionType === 'Update'">
          <span class="text-blue-400">Updating {{ changedFields.length }} field(s)</span>
        </template>
        <template v-else-if="action.actionType === 'Delete'">
          <span class="text-red-400">Deleting this item</span>
        </template>
      </div>

      <!-- Expanded Details -->
      <Transition
        enter-active-class="transition-all duration-200 ease-out"
        enter-from-class="opacity-0 max-h-0"
        enter-to-class="opacity-100 max-h-96"
        leave-active-class="transition-all duration-150 ease-in"
        leave-from-class="opacity-100 max-h-96"
        leave-to-class="opacity-0 max-h-0"
      >
        <div v-if="isExpanded" class="space-y-2 overflow-hidden">
          <!-- For Create: show all fields -->
          <div v-if="action.actionType === 'Create'" class="bg-slate-800/50 rounded p-2">
            <p class="text-xs font-medium text-slate-400 mb-1">New Data:</p>
            <div class="space-y-1">
              <div v-for="(value, key) in proposedData" :key="key" class="text-xs">
                <span class="text-slate-500">{{ key }}:</span>
                <span class="text-emerald-300 ml-1">{{ value }}</span>
              </div>
            </div>
          </div>

          <!-- For Update: show diff -->
          <div v-if="action.actionType === 'Update' && changedFields.length > 0" class="bg-slate-800/50 rounded p-2">
            <p class="text-xs font-medium text-slate-400 mb-1">Changes:</p>
            <div class="space-y-1">
              <div v-for="change in changedFields" :key="change.field" class="text-xs">
                <span class="text-slate-500">{{ change.field }}:</span>
                <span class="text-red-400 line-through ml-1">{{ change.from }}</span>
                <span class="text-slate-500 mx-1">→</span>
                <span class="text-emerald-300">{{ change.to }}</span>
              </div>
            </div>
          </div>

          <!-- For Delete: show what will be deleted -->
          <div v-if="action.actionType === 'Delete' && previousData" class="bg-slate-800/50 rounded p-2">
            <p class="text-xs font-medium text-red-400 mb-1">Will be deleted:</p>
            <div class="space-y-1">
              <div v-for="(value, key) in previousData" :key="key" class="text-xs">
                <span class="text-slate-500">{{ key }}:</span>
                <span class="text-red-300 ml-1">{{ value }}</span>
              </div>
            </div>
          </div>

          <!-- Agent info -->
          <div v-if="action.agentName" class="text-xs text-slate-500">
            Proposed by: {{ action.agentName }}
          </div>
        </div>
      </Transition>

      <!-- Expand/Collapse Button -->
      <button
        v-if="!compact"
        class="text-xs text-purple-400 hover:text-purple-300 flex items-center gap-1"
        @click="isExpanded = !isExpanded"
      >
        <svg
          class="w-3 h-3 transition-transform"
          :class="{ 'rotate-180': isExpanded }"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
        {{ isExpanded ? 'Show less' : 'Show details' }}
      </button>
    </div>

    <!-- Actions (only for Pending status) -->
    <div v-if="isPending" class="flex items-center gap-2 px-3 py-2 border-t border-slate-600/30 bg-slate-800/30">
      <button
        class="flex-1 px-3 py-1.5 rounded-lg bg-emerald-500/20 text-emerald-400 text-sm font-medium hover:bg-emerald-500/30 transition-colors"
        @click="emit('approve', action.id)"
      >
        ✓ Approve
      </button>
      <button
        class="flex-1 px-3 py-1.5 rounded-lg bg-red-500/20 text-red-400 text-sm font-medium hover:bg-red-500/30 transition-colors"
        @click="emit('reject', action.id)"
      >
        ✗ Reject
      </button>
      <button
        class="px-3 py-1.5 rounded-lg bg-slate-600/50 text-slate-400 text-sm hover:bg-slate-600/70 transition-colors"
        @click="emit('viewDetails', action.id)"
      >
        Edit
      </button>
    </div>

    <!-- Status footer for non-pending -->
    <div v-else class="px-3 py-2 border-t border-slate-600/30 bg-slate-800/30 text-xs text-slate-500">
      <template v-if="action.status === 'Executed'">
        ✓ Executed {{ action.executedAt ? new Date(action.executedAt).toLocaleString() : '' }}
      </template>
      <template v-else-if="action.status === 'Rejected'">
        ✗ Rejected {{ action.reviewedAt ? new Date(action.reviewedAt).toLocaleString() : '' }}
      </template>
      <template v-else-if="action.status === 'Failed'">
        ⚠ Failed -
        <button class="text-purple-400 hover:underline" @click="emit('viewDetails', action.id)">
          View error
        </button>
      </template>
    </div>
  </div>
</template>
