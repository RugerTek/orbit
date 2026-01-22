<script setup lang="ts">
import type { UserProfile, UpdateProfileRequest } from '~/types/user'

interface Props {
  modelValue: boolean
  profile: UserProfile | null
  isLoading?: boolean
  error?: string | null
}

const props = withDefaults(defineProps<Props>(), {
  isLoading: false,
  error: null
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'submit', data: UpdateProfileRequest): void
}>()

// Form state
const formData = ref<UpdateProfileRequest>({
  displayName: '',
  firstName: '',
  lastName: ''
})

// Watch for profile changes to populate form
watch(() => props.profile, (newProfile) => {
  if (newProfile) {
    formData.value = {
      displayName: newProfile.displayName,
      firstName: newProfile.firstName || '',
      lastName: newProfile.lastName || ''
    }
  }
}, { immediate: true })

// Reset form when dialog opens
watch(() => props.modelValue, (isOpen) => {
  if (isOpen && props.profile) {
    formData.value = {
      displayName: props.profile.displayName,
      firstName: props.profile.firstName || '',
      lastName: props.profile.lastName || ''
    }
  }
})

// Validation
const displayNameError = computed(() => {
  const name = formData.value.displayName.trim()
  if (!name) return 'Display name is required'
  if (name.length < 2) return 'Display name must be at least 2 characters'
  if (name.length > 100) return 'Display name must be 100 characters or less'
  return null
})

const firstNameError = computed(() => {
  const name = formData.value.firstName?.trim() || ''
  if (name.length > 50) return 'First name must be 50 characters or less'
  return null
})

const lastNameError = computed(() => {
  const name = formData.value.lastName?.trim() || ''
  if (name.length > 50) return 'Last name must be 50 characters or less'
  return null
})

const isValid = computed(() => {
  return !displayNameError.value && !firstNameError.value && !lastNameError.value
})

const hasChanges = computed(() => {
  if (!props.profile) return false
  return (
    formData.value.displayName.trim() !== props.profile.displayName ||
    (formData.value.firstName?.trim() || '') !== (props.profile.firstName || '') ||
    (formData.value.lastName?.trim() || '') !== (props.profile.lastName || '')
  )
})

const handleSubmit = () => {
  if (!isValid.value || props.isLoading) return
  emit('submit', {
    displayName: formData.value.displayName.trim(),
    firstName: formData.value.firstName?.trim() || undefined,
    lastName: formData.value.lastName?.trim() || undefined
  })
}

const close = () => {
  emit('update:modelValue', false)
}
</script>

<template>
  <BaseDialog
    :model-value="modelValue"
    title="Edit Profile"
    subtitle="Update your personal information"
    size="lg"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="handleSubmit"
  >
    <div class="space-y-4">
      <!-- Error Alert -->
      <div
        v-if="error"
        class="rounded-xl bg-red-500/10 border border-red-500/30 p-4 text-sm text-red-400"
      >
        {{ error }}
      </div>

      <!-- Display Name -->
      <div>
        <label for="displayName" class="block text-sm font-medium text-white/80 mb-1">
          Display Name <span class="text-red-400">*</span>
        </label>
        <input
          id="displayName"
          v-model="formData.displayName"
          type="text"
          class="orbitos-input w-full"
          :class="{ 'border-red-500/50': displayNameError && formData.displayName }"
          placeholder="How you want to be called"
          :disabled="isLoading"
          autofocus
        />
        <p v-if="displayNameError" class="mt-1 text-xs text-red-400">
          {{ displayNameError }}
        </p>
      </div>

      <!-- First Name -->
      <div>
        <label for="firstName" class="block text-sm font-medium text-white/80 mb-1">
          First Name
        </label>
        <input
          id="firstName"
          v-model="formData.firstName"
          type="text"
          class="orbitos-input w-full"
          :class="{ 'border-red-500/50': firstNameError }"
          placeholder="Your first name"
          :disabled="isLoading"
        />
        <p v-if="firstNameError" class="mt-1 text-xs text-red-400">
          {{ firstNameError }}
        </p>
      </div>

      <!-- Last Name -->
      <div>
        <label for="lastName" class="block text-sm font-medium text-white/80 mb-1">
          Last Name
        </label>
        <input
          id="lastName"
          v-model="formData.lastName"
          type="text"
          class="orbitos-input w-full"
          :class="{ 'border-red-500/50': lastNameError }"
          placeholder="Your last name"
          :disabled="isLoading"
        />
        <p v-if="lastNameError" class="mt-1 text-xs text-red-400">
          {{ lastNameError }}
        </p>
      </div>
    </div>

    <template #footer="{ close: closeDialog }">
      <div class="flex justify-end gap-3">
        <button
          type="button"
          class="orbitos-btn-secondary"
          :disabled="isLoading"
          @click="closeDialog"
        >
          Cancel
        </button>
        <button
          type="button"
          class="orbitos-btn-primary"
          :disabled="!isValid || isLoading || !hasChanges"
          @click="handleSubmit"
        >
          <span v-if="isLoading" class="flex items-center gap-2">
            <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            Saving...
          </span>
          <span v-else>Save Changes</span>
        </button>
      </div>
    </template>
  </BaseDialog>
</template>
