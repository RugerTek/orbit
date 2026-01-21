<script setup lang="ts">
/**
 * =============================================================================
 * SearchableAssigner - Reusable Searchable Multi-Select Assignment Component
 * =============================================================================
 * A component for assigning/unassigning items with a searchable list.
 * Shows assigned items as removable chips and available items in a scrollable list.
 *
 * USAGE:
 * <SearchableAssigner
 *   :assigned="assignedFunctions"
 *   :available="availableFunctions"
 *   label="Assigned Functions"
 *   search-placeholder="Search functions..."
 *   :loading="isLoading"
 *   @add="handleAddFunction"
 *   @remove="handleRemoveFunction"
 * />
 * =============================================================================
 */

export interface AssignableItem {
  id: string
  name: string
  subtitle?: string
}

interface Props {
  /** Items currently assigned */
  assigned: AssignableItem[]
  /** Items available for assignment (not yet assigned) */
  available: AssignableItem[]
  /** Label for the section */
  label?: string
  /** Placeholder for search input */
  searchPlaceholder?: string
  /** Loading state */
  loading?: boolean
  /** Text shown when no items are assigned */
  emptyAssignedText?: string
  /** Text shown when no available items */
  emptyAvailableText?: string
  /** Text shown when search returns no results */
  noResultsText?: string
}

const props = withDefaults(defineProps<Props>(), {
  label: 'Assigned Items',
  searchPlaceholder: 'Search...',
  loading: false,
  emptyAssignedText: 'No items assigned',
  emptyAvailableText: 'No items available',
  noResultsText: 'No matching items found',
})

const emit = defineEmits<{
  (e: 'add', id: string): void
  (e: 'remove', id: string): void
}>()

// Local state
const searchQuery = ref('')
const isAdding = ref<string | null>(null)
const isRemoving = ref<string | null>(null)

// Filter available items based on search
const filteredAvailable = computed(() => {
  if (!searchQuery.value) return props.available
  const query = searchQuery.value.toLowerCase()
  return props.available.filter(item =>
    item.name.toLowerCase().includes(query) ||
    item.subtitle?.toLowerCase().includes(query)
  )
})

// Handle adding an item
const handleAdd = async (id: string) => {
  isAdding.value = id
  try {
    emit('add', id)
  } finally {
    // Let parent handle async, just clear visual state after a short delay
    setTimeout(() => {
      isAdding.value = null
    }, 300)
  }
}

// Handle removing an item
const handleRemove = async (id: string) => {
  isRemoving.value = id
  try {
    emit('remove', id)
  } finally {
    setTimeout(() => {
      isRemoving.value = null
    }, 300)
  }
}
</script>

<template>
  <div class="space-y-3">
    <!-- Label -->
    <label v-if="label" class="orbitos-label">{{ label }}</label>

    <!-- Search Input -->
    <div class="relative">
      <svg
        class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
      </svg>
      <input
        v-model="searchQuery"
        type="text"
        class="orbitos-input pl-10"
        :placeholder="searchPlaceholder"
      />
    </div>

    <!-- Assigned Items as Chips -->
    <div v-if="assigned.length > 0" class="flex flex-wrap gap-2">
      <span
        v-for="item in assigned"
        :key="item.id"
        class="inline-flex items-center gap-1.5 rounded-full bg-purple-500/20 border border-purple-500/30 px-3 py-1 text-sm text-purple-300"
      >
        {{ item.name }}
        <button
          @click="handleRemove(item.id)"
          :disabled="isRemoving === item.id || loading"
          class="rounded-full p-0.5 hover:bg-purple-500/30 transition-colors disabled:opacity-50"
          :title="`Remove ${item.name}`"
        >
          <svg v-if="isRemoving === item.id" class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <svg v-else class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </span>
    </div>
    <div v-else class="text-sm text-white/40 italic">
      {{ emptyAssignedText }}
    </div>

    <!-- Divider -->
    <div class="border-t border-white/10 pt-3">
      <div class="text-xs uppercase text-white/40 mb-2">Available to add</div>
    </div>

    <!-- Available Items List -->
    <div class="space-y-1.5 max-h-48 overflow-y-auto">
      <!-- Loading state -->
      <div v-if="loading" class="flex items-center justify-center py-6">
        <div class="orbitos-spinner orbitos-spinner-sm"></div>
      </div>

      <!-- Empty state -->
      <div
        v-else-if="available.length === 0"
        class="text-center py-6 text-white/40 text-sm"
      >
        {{ emptyAvailableText }}
      </div>

      <!-- No search results -->
      <div
        v-else-if="filteredAvailable.length === 0"
        class="text-center py-6 text-white/40 text-sm"
      >
        {{ noResultsText }}
      </div>

      <!-- Items list -->
      <button
        v-else
        v-for="item in filteredAvailable"
        :key="item.id"
        @click="handleAdd(item.id)"
        :disabled="isAdding === item.id || loading"
        class="w-full text-left rounded-lg bg-white/5 hover:bg-white/10 px-3 py-2 transition-colors disabled:opacity-50 group"
      >
        <div class="flex items-center justify-between">
          <div class="min-w-0 flex-1">
            <div class="text-white text-sm font-medium truncate">{{ item.name }}</div>
            <div v-if="item.subtitle" class="text-xs text-white/40 truncate">{{ item.subtitle }}</div>
          </div>
          <div class="ml-2 flex-shrink-0">
            <svg
              v-if="isAdding === item.id"
              class="w-4 h-4 text-purple-400 animate-spin"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <svg
              v-else
              class="w-4 h-4 text-white/20 group-hover:text-purple-400 transition-colors"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
          </div>
        </div>
      </button>
    </div>
  </div>
</template>
