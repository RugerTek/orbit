<script setup lang="ts">
/**
 * FieldHelpTooltip - Intelligent Field-Level Help
 *
 * Hover/click to show help for any form field
 * Content pulled from specs validation rules
 * Shows examples and best practices
 */

import type { FieldHelp } from '~/types/help'

const props = defineProps<{
  entityId: string
  fieldName: string
  position?: 'top' | 'bottom' | 'left' | 'right'
}>()

const { getFieldHelp } = useHelp()

const isVisible = ref(false)
const tooltipRef = ref<HTMLElement | null>(null)

// Get field help data
const fieldHelp = computed((): FieldHelp | null => {
  return getFieldHelp(props.entityId, props.fieldName)
})

// Position classes
const positionClasses = computed(() => {
  switch (props.position || 'top') {
    case 'top':
      return 'bottom-full left-1/2 -translate-x-1/2 mb-2'
    case 'bottom':
      return 'top-full left-1/2 -translate-x-1/2 mt-2'
    case 'left':
      return 'right-full top-1/2 -translate-y-1/2 mr-2'
    case 'right':
      return 'left-full top-1/2 -translate-y-1/2 ml-2'
    default:
      return 'bottom-full left-1/2 -translate-x-1/2 mb-2'
  }
})

// Arrow classes
const arrowClasses = computed(() => {
  switch (props.position || 'top') {
    case 'top':
      return 'top-full left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-b-transparent border-t-slate-700'
    case 'bottom':
      return 'bottom-full left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-t-transparent border-b-slate-700'
    case 'left':
      return 'left-full top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-r-transparent border-l-slate-700'
    case 'right':
      return 'right-full top-1/2 -translate-y-1/2 border-t-transparent border-b-transparent border-l-transparent border-r-slate-700'
    default:
      return 'top-full left-1/2 -translate-x-1/2 border-l-transparent border-r-transparent border-b-transparent border-t-slate-700'
  }
})

// Show/hide handlers
const showTooltip = () => {
  isVisible.value = true
}

const hideTooltip = () => {
  isVisible.value = false
}

const toggleTooltip = () => {
  isVisible.value = !isVisible.value
}

// Click outside to close (manual implementation to avoid VueUse dependency)
const handleClickOutside = (event: MouseEvent) => {
  if (tooltipRef.value && !tooltipRef.value.contains(event.target as Node)) {
    isVisible.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div class="relative inline-flex" ref="tooltipRef">
    <!-- Trigger Button -->
    <button
      type="button"
      class="flex h-5 w-5 items-center justify-center rounded-full text-slate-500 hover:bg-slate-700 hover:text-slate-300 transition-colors"
      @mouseenter="showTooltip"
      @mouseleave="hideTooltip"
      @click.stop="toggleTooltip"
    >
      <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    </button>

    <!-- Tooltip -->
    <Transition
      enter-active-class="transition-all duration-200 ease-out"
      enter-from-class="opacity-0 scale-95"
      enter-to-class="opacity-100 scale-100"
      leave-active-class="transition-all duration-150 ease-in"
      leave-from-class="opacity-100 scale-100"
      leave-to-class="opacity-0 scale-95"
    >
      <div
        v-if="isVisible && fieldHelp"
        :class="[
          'absolute z-50 w-72 rounded-lg border border-slate-700 bg-slate-800 p-4 shadow-xl',
          positionClasses
        ]"
      >
        <!-- Arrow -->
        <div :class="['absolute w-0 h-0 border-8', arrowClasses]" />

        <!-- Header -->
        <div class="flex items-start justify-between mb-2">
          <h4 class="font-semibold text-white">{{ fieldHelp.field }}</h4>
          <span
            v-if="fieldHelp.required"
            class="rounded bg-red-500/20 px-1.5 py-0.5 text-[10px] font-medium text-red-300"
          >
            Required
          </span>
        </div>

        <!-- Description -->
        <p class="text-sm text-slate-300 mb-3">{{ fieldHelp.description }}</p>

        <!-- Help Text -->
        <div class="rounded bg-slate-700/50 p-2 mb-3">
          <p class="text-xs text-slate-400">{{ fieldHelp.helpText }}</p>
        </div>

        <!-- Validation Rules -->
        <div v-if="fieldHelp.validation" class="space-y-1 mb-3">
          <p class="text-xs font-medium text-slate-500 uppercase">Validation</p>
          <ul class="space-y-1">
            <li v-if="fieldHelp.validation.minLength" class="flex items-center gap-2 text-xs text-slate-400">
              <svg class="h-3 w-3 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Minimum {{ fieldHelp.validation.minLength }} characters
            </li>
            <li v-if="fieldHelp.validation.maxLength" class="flex items-center gap-2 text-xs text-slate-400">
              <svg class="h-3 w-3 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Maximum {{ fieldHelp.validation.maxLength }} characters
            </li>
          </ul>
        </div>

        <!-- Examples -->
        <div v-if="fieldHelp.examples && fieldHelp.examples.length > 0" class="space-y-1">
          <p class="text-xs font-medium text-slate-500 uppercase">Examples</p>
          <div class="flex flex-wrap gap-1">
            <code
              v-for="example in fieldHelp.examples"
              :key="example"
              class="rounded bg-purple-500/20 px-2 py-0.5 text-xs text-purple-300"
            >
              {{ example }}
            </code>
          </div>
        </div>

        <!-- Link to full docs -->
        <NuxtLink
          :to="`/help/concepts/${fieldHelp.entity}`"
          class="mt-3 flex items-center gap-1 text-xs text-purple-400 hover:text-purple-300"
        >
          Learn more about {{ fieldHelp.entityName }}
          <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </NuxtLink>
      </div>
    </Transition>

    <!-- Fallback when no help available -->
    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isVisible && !fieldHelp"
        :class="[
          'absolute z-50 rounded-lg border border-slate-700 bg-slate-800 px-3 py-2 shadow-xl',
          positionClasses
        ]"
      >
        <p class="text-xs text-slate-400">Help not available for this field</p>
      </div>
    </Transition>
  </div>
</template>
