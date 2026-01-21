<script setup lang="ts">
/**
 * =============================================================================
 * BaseDialog - Reusable Modal/Dialog Component
 * =============================================================================
 * A properly implemented dialog component that handles click events correctly.
 *
 * FEATURES:
 * - Clicking backdrop closes the dialog
 * - Clicking inside dialog content does NOT close it
 * - Escape key closes the dialog
 * - Enter key submits (when not in textarea)
 * - Teleports to body to avoid z-index issues
 * - Supports different sizes (sm, md, lg, xl, 2xl)
 * - Accessible with proper focus management
 *
 * USAGE:
 * <BaseDialog v-model="showDialog" size="lg" title="My Dialog" @submit="handleSubmit">
 *   <template #default>
 *     <!-- Your form/content here -->
 *   </template>
 *   <template #footer="{ close }">
 *     <button @click="close">Cancel</button>
 *     <button @click="handleSubmit">Submit</button>
 *   </template>
 * </BaseDialog>
 *
 * CRITICAL USAGE NOTES:
 * =====================
 * 1. COMPONENT NAME: Use <BaseDialog>, NOT <UiBaseDialog>
 *    - Nuxt auto-imports from components/ directory
 *    - UiBaseDialog would look for components/ui/BaseDialog.vue which doesn't exist
 *    - Using wrong name causes form content to render outside the dialog!
 *
 * 2. NO v-if NEEDED: Don't add v-if="showDialog" on BaseDialog
 *    - The component handles conditional rendering internally via v-if on Teleport
 *    - Adding v-if is redundant but harmless
 *
 * 3. FOOTER SLOT: Use the { close } parameter for the Cancel button
 *    - <template #footer="{ close }"> gives you access to close()
 *    - This ensures consistent dialog closing behavior
 *
 * COMMON MISTAKES TO AVOID:
 * - Using <UiBaseDialog> instead of <BaseDialog> (causes content leak)
 * - Creating custom dialogs without using this component
 * - Using @click.self on backdrops (unreliable, use separate backdrop div)
 * =============================================================================
 */

interface Props {
  /** Controls dialog visibility (v-model) */
  modelValue: boolean
  /** Dialog title (optional - can use slot instead) */
  title?: string
  /** Dialog subtitle/description */
  subtitle?: string
  /** Dialog max-width size */
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl'
  /** Whether clicking backdrop closes the dialog */
  closeOnBackdrop?: boolean
  /** Whether pressing Escape closes the dialog */
  closeOnEscape?: boolean
  /** Whether pressing Enter triggers submit event (when not in textarea) */
  submitOnEnter?: boolean
  /** Whether to show a close button in the header */
  showCloseButton?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  size: 'lg',
  closeOnBackdrop: true,
  closeOnEscape: true,
  submitOnEnter: true,
  showCloseButton: false
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'close'): void
  (e: 'submit'): void
}>()

// Size classes mapping
const sizeClasses: Record<string, string> = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-xl',
  '2xl': 'max-w-2xl'
}

const sizeClass = computed(() => sizeClasses[props.size] || sizeClasses.lg)

// Close the dialog
const close = () => {
  emit('update:modelValue', false)
  emit('close')
}

// Handle backdrop click
const handleBackdropClick = () => {
  if (props.closeOnBackdrop) {
    close()
  }
}

// Handle keyboard events
const handleKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Escape' && props.closeOnEscape) {
    close()
  }
  // Enter key submits the form (unless in a textarea or select)
  if (e.key === 'Enter' && props.submitOnEnter) {
    const target = e.target as HTMLElement
    const tagName = target.tagName.toLowerCase()
    // Don't submit if in textarea (allow newlines) or select (allow selection)
    if (tagName !== 'textarea' && tagName !== 'select') {
      e.preventDefault()
      emit('submit')
    }
  }
}

// Reference to the dialog wrapper for focus management
const dialogWrapper = ref<HTMLElement | null>(null)

// Global Escape key handler (for when focus is outside dialog)
const handleGlobalKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Escape' && props.closeOnEscape && props.modelValue) {
    close()
  }
}

// Watch for dialog open to focus the wrapper
watch(() => props.modelValue, (isOpen) => {
  if (isOpen) {
    // Add global escape listener
    document.addEventListener('keydown', handleGlobalKeydown)
    // Focus the dialog wrapper on next tick
    nextTick(() => {
      dialogWrapper.value?.focus()
    })
  } else {
    // Remove global escape listener
    document.removeEventListener('keydown', handleGlobalKeydown)
  }
})

// Clean up on unmount
onUnmounted(() => {
  document.removeEventListener('keydown', handleGlobalKeydown)
})
</script>

<template>
  <!--
    IMPORTANT: We use v-if on the entire template to prevent slot content from
    being rendered when the dialog is closed. This fixes the issue where form
    content would appear at the bottom of the page.
  -->
  <Teleport v-if="modelValue" to="body">
    <div
      ref="dialogWrapper"
      class="fixed inset-0 z-[100] overflow-y-auto"
      tabindex="-1"
      @keydown="handleKeydown"
    >
      <!-- Backdrop - separate clickable layer for closing -->
      <div
        class="fixed inset-0 bg-black/60 backdrop-blur-sm"
        @click="handleBackdropClick"
        aria-hidden="true"
      />

      <!-- Centering wrapper - uses min-height and flexbox for proper centering with scroll -->
      <div class="flex min-h-full items-center justify-center p-4">
        <!-- Dialog content container - @click.stop prevents backdrop click -->
        <div
          class="relative w-full orbitos-glass p-6"
          :class="sizeClass"
          role="dialog"
          aria-modal="true"
          :aria-labelledby="title ? 'dialog-title' : undefined"
          @click.stop
        >
          <!-- Header -->
          <div v-if="title || $slots.header" class="mb-4">
            <slot name="header">
              <div class="flex items-start justify-between">
                <div>
                  <h2 v-if="title" id="dialog-title" class="orbitos-heading-sm">
                    {{ title }}
                  </h2>
                  <p v-if="subtitle" class="text-sm orbitos-text mt-1">
                    {{ subtitle }}
                  </p>
                </div>
                <button
                  v-if="showCloseButton"
                  type="button"
                  class="text-white/40 hover:text-white transition-colors p-1 -m-1"
                  @click="close"
                  aria-label="Close dialog"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </slot>
          </div>

          <!-- Content -->
          <div class="dialog-content">
            <slot />
          </div>

          <!-- Footer -->
          <div v-if="$slots.footer" class="mt-6">
            <slot name="footer" :close="close" />
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
