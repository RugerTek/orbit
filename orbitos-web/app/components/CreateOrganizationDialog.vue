<script setup lang="ts">
import { useOrganizations } from '~/composables/useOrganizations'

interface Props {
  modelValue: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'created'): void
}>()

const { createOrganization, isLoading } = useOrganizations()

const form = reactive({
  name: '',
  description: '',
})

const formError = ref<string | null>(null)

const isFormValid = computed(() => form.name.trim().length > 0)

const handleSubmit = async () => {
  if (!isFormValid.value) return

  formError.value = null
  try {
    await createOrganization({
      name: form.name.trim(),
      description: form.description.trim() || undefined,
    })
    emit('created')
    close()
    // Reload the page to refresh all data for the new organization
    window.location.reload()
  } catch (e) {
    formError.value = 'Failed to create organization. Please try again.'
  }
}

const close = () => {
  emit('update:modelValue', false)
  // Reset form after close animation
  setTimeout(() => {
    form.name = ''
    form.description = ''
    formError.value = null
  }, 200)
}
</script>

<template>
  <BaseDialog
    :model-value="modelValue"
    size="md"
    title="Create Organization"
    subtitle="Set up a new organization for another company or business unit"
    @update:model-value="emit('update:modelValue', $event)"
    @submit="handleSubmit"
  >
    <form @submit.prevent="handleSubmit" class="space-y-4">
      <!-- Error message -->
      <div v-if="formError" class="p-3 rounded-lg bg-red-500/20 border border-red-500/30 text-red-300 text-sm">
        {{ formError }}
      </div>

      <!-- Organization Name -->
      <div>
        <label for="org-name" class="block text-sm font-medium text-white/70 mb-1.5">
          Organization Name <span class="text-red-400">*</span>
        </label>
        <input
          id="org-name"
          v-model="form.name"
          type="text"
          class="orbitos-input w-full"
          placeholder="e.g., Acme Corporation"
          autofocus
          required
        />
      </div>

      <!-- Description -->
      <div>
        <label for="org-description" class="block text-sm font-medium text-white/70 mb-1.5">
          Description
        </label>
        <textarea
          id="org-description"
          v-model="form.description"
          class="orbitos-input w-full resize-none"
          rows="3"
          placeholder="Brief description of this organization..."
        />
      </div>
    </form>

    <template #footer="{ close: dialogClose }">
      <div class="flex gap-3 justify-end">
        <button
          type="button"
          class="orbitos-btn-secondary"
          @click="dialogClose"
        >
          Cancel
        </button>
        <button
          type="submit"
          class="orbitos-btn-primary"
          :disabled="!isFormValid || isLoading"
          @click="handleSubmit"
        >
          <svg v-if="isLoading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          {{ isLoading ? 'Creating...' : 'Create Organization' }}
        </button>
      </div>
    </template>
  </BaseDialog>
</template>
