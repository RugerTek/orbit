<script setup lang="ts">
import type { ChangePasswordRequest } from '~/types/user'
import { validatePassword } from '~/composables/useUserProfile'

interface Props {
  modelValue: boolean
  isLoading?: boolean
  error?: string | null
}

const props = withDefaults(defineProps<Props>(), {
  isLoading: false,
  error: null
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'submit', data: ChangePasswordRequest): void
}>()

// Form state
const formData = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

// Password visibility toggles
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

// Reset form when dialog closes
watch(() => props.modelValue, (isOpen) => {
  if (!isOpen) {
    formData.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    }
    showCurrentPassword.value = false
    showNewPassword.value = false
    showConfirmPassword.value = false
  }
})

// Password validation
const passwordValidation = computed(() => {
  return validatePassword(formData.value.newPassword)
})

const passwordsMatch = computed(() => {
  return formData.value.newPassword === formData.value.confirmPassword
})

const confirmPasswordError = computed(() => {
  if (!formData.value.confirmPassword) return null
  if (!passwordsMatch.value) return 'Passwords do not match'
  return null
})

const isValid = computed(() => {
  return (
    formData.value.currentPassword.length > 0 &&
    passwordValidation.value.isValid &&
    passwordsMatch.value &&
    formData.value.confirmPassword.length > 0
  )
})

const strengthColor = computed(() => {
  switch (passwordValidation.value.strength) {
    case 'strong': return 'bg-emerald-500'
    case 'medium': return 'bg-yellow-500'
    default: return 'bg-red-500'
  }
})

const strengthWidth = computed(() => {
  switch (passwordValidation.value.strength) {
    case 'strong': return 'w-full'
    case 'medium': return 'w-2/3'
    default: return 'w-1/3'
  }
})

const handleSubmit = () => {
  if (!isValid.value || props.isLoading) return
  emit('submit', {
    currentPassword: formData.value.currentPassword,
    newPassword: formData.value.newPassword,
    confirmPassword: formData.value.confirmPassword
  })
}
</script>

<template>
  <BaseDialog
    :model-value="modelValue"
    title="Change Password"
    subtitle="Enter your current password and choose a new one"
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

      <!-- Current Password -->
      <div>
        <label for="currentPassword" class="block text-sm font-medium text-white/80 mb-1">
          Current Password <span class="text-red-400">*</span>
        </label>
        <div class="relative">
          <input
            id="currentPassword"
            v-model="formData.currentPassword"
            :type="showCurrentPassword ? 'text' : 'password'"
            class="orbitos-input w-full pr-10"
            placeholder="Enter your current password"
            :disabled="isLoading"
            autofocus
          />
          <button
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-white/40 hover:text-white transition-colors"
            @click="showCurrentPassword = !showCurrentPassword"
            tabindex="-1"
          >
            <svg v-if="showCurrentPassword" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
            </svg>
            <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>
      </div>

      <!-- New Password -->
      <div>
        <label for="newPassword" class="block text-sm font-medium text-white/80 mb-1">
          New Password <span class="text-red-400">*</span>
        </label>
        <div class="relative">
          <input
            id="newPassword"
            v-model="formData.newPassword"
            :type="showNewPassword ? 'text' : 'password'"
            class="orbitos-input w-full pr-10"
            placeholder="Enter your new password"
            :disabled="isLoading"
          />
          <button
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-white/40 hover:text-white transition-colors"
            @click="showNewPassword = !showNewPassword"
            tabindex="-1"
          >
            <svg v-if="showNewPassword" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
            </svg>
            <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>

        <!-- Password Strength Meter -->
        <div v-if="formData.newPassword" class="mt-2">
          <div class="h-1.5 w-full bg-white/10 rounded-full overflow-hidden">
            <div
              class="h-full transition-all duration-300 rounded-full"
              :class="[strengthColor, strengthWidth]"
            />
          </div>
          <div class="flex items-center justify-between mt-1">
            <span class="text-xs capitalize" :class="{
              'text-red-400': passwordValidation.strength === 'weak',
              'text-yellow-400': passwordValidation.strength === 'medium',
              'text-emerald-400': passwordValidation.strength === 'strong'
            }">
              {{ passwordValidation.strength }} password
            </span>
          </div>

          <!-- Password Requirements -->
          <div class="mt-2 space-y-1">
            <div
              v-for="(check, key) in passwordValidation.checks"
              :key="key"
              class="flex items-center gap-2 text-xs"
              :class="check ? 'text-emerald-400' : 'text-white/40'"
            >
              <svg v-if="check" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              <svg v-else class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="10" stroke-width="2" />
              </svg>
              <span>
                {{ key === 'minLength' ? 'At least 8 characters' :
                   key === 'hasUppercase' ? 'One uppercase letter' :
                   key === 'hasLowercase' ? 'One lowercase letter' :
                   key === 'hasNumber' ? 'One number' :
                   'One special character (optional)' }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Confirm Password -->
      <div>
        <label for="confirmPassword" class="block text-sm font-medium text-white/80 mb-1">
          Confirm New Password <span class="text-red-400">*</span>
        </label>
        <div class="relative">
          <input
            id="confirmPassword"
            v-model="formData.confirmPassword"
            :type="showConfirmPassword ? 'text' : 'password'"
            class="orbitos-input w-full pr-10"
            :class="{ 'border-red-500/50': confirmPasswordError }"
            placeholder="Confirm your new password"
            :disabled="isLoading"
          />
          <button
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-white/40 hover:text-white transition-colors"
            @click="showConfirmPassword = !showConfirmPassword"
            tabindex="-1"
          >
            <svg v-if="showConfirmPassword" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
            </svg>
            <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
          </button>
        </div>
        <div v-if="confirmPasswordError" class="mt-1 text-xs text-red-400">
          {{ confirmPasswordError }}
        </div>
        <div v-else-if="formData.confirmPassword && passwordsMatch" class="mt-1 flex items-center gap-1 text-xs text-emerald-400">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          Passwords match
        </div>
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
          :disabled="!isValid || isLoading"
          @click="handleSubmit"
        >
          <span v-if="isLoading" class="flex items-center gap-2">
            <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            Changing...
          </span>
          <span v-else>Change Password</span>
        </button>
      </div>
    </template>
  </BaseDialog>
</template>
