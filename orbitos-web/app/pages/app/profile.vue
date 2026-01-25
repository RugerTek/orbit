<script setup lang="ts">
import type { UpdateProfileRequest, ChangePasswordRequest } from '~/types/user'

definePageMeta({
  layout: 'app'
})

const { authToken, initializeAuth } = useAuth()
const { profile, isLoading, error, fetchProfile, updateProfile, changePassword, clearError } = useUserProfile()
const config = useRuntimeConfig()

// Dialog states
const showEditDialog = ref(false)
const showPasswordDialog = ref(false)

// Operation states
const editLoading = ref(false)
const editError = ref<string | null>(null)
const passwordLoading = ref(false)
const passwordError = ref<string | null>(null)

// Success messages
const showSuccessMessage = ref(false)
const successMessage = ref('')

// Fetch profile on mount - wait for auth to be ready
onMounted(async () => {
  // Ensure auth is initialized (may already be done by layout)
  if (!authToken.value) {
    await initializeAuth()
  }

  // Wait briefly for auth state to stabilize (handles race conditions)
  if (!authToken.value) {
    await new Promise(resolve => setTimeout(resolve, 100))
  }

  // Now fetch profile with valid auth token
  await fetchProfile()
})

// Format date helper
const formatDate = (dateStr?: string) => {
  if (!dateStr) return 'Not available'
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

// Edit profile handler
const handleEditSubmit = async (data: UpdateProfileRequest) => {
  editError.value = null
  editLoading.value = true

  try {
    await updateProfile(data)
    showEditDialog.value = false
    showSuccess('Profile updated successfully')
  } catch (e: unknown) {
    editError.value = e instanceof Error ? e.message : 'Failed to update profile'
  } finally {
    editLoading.value = false
  }
}

// Change password handler
const handlePasswordSubmit = async (data: ChangePasswordRequest) => {
  passwordError.value = null
  passwordLoading.value = true

  try {
    await changePassword(data)
    showPasswordDialog.value = false
    showSuccess('Password changed successfully')
  } catch (e: unknown) {
    passwordError.value = e instanceof Error ? e.message : 'Failed to change password'
  } finally {
    passwordLoading.value = false
  }
}

// Link Google handler
const handleLinkGoogle = () => {
  // Initialize Google Sign-In
  const googleClientId = config.public.googleClientId
  if (!googleClientId) {
    showError('Google Sign-In is not configured')
    return
  }

  // Use Google's popup flow
  // This is a simplified version - in production you'd use the Google Identity Services library
  window.open(
    `https://accounts.google.com/o/oauth2/v2/auth?client_id=${googleClientId}&redirect_uri=${encodeURIComponent(window.location.origin)}&response_type=code&scope=email%20profile&prompt=consent`,
    'Link Google Account',
    'width=500,height=600'
  )
}

// Show success message
const showSuccess = (message: string) => {
  successMessage.value = message
  showSuccessMessage.value = true
  setTimeout(() => {
    showSuccessMessage.value = false
  }, 3000)
}

// Show error message
const showError = (message: string) => {
  editError.value = message
}

// Clear dialog errors when closing
watch(showEditDialog, (isOpen) => {
  if (!isOpen) editError.value = null
})

watch(showPasswordDialog, (isOpen) => {
  if (!isOpen) passwordError.value = null
})
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <!-- Page Header -->
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-white">My Profile</h1>
      <p class="text-white/60 mt-1">Manage your account settings and preferences</p>
    </div>

    <!-- Success Toast -->
    <Transition
      enter-active-class="transform ease-out duration-300 transition"
      enter-from-class="translate-y-2 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition ease-in duration-100"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="showSuccessMessage"
        class="fixed top-20 right-4 z-50 rounded-xl bg-emerald-500/20 border border-emerald-500/30 px-4 py-3 text-sm text-emerald-400 shadow-lg backdrop-blur-xl"
      >
        <div class="flex items-center gap-2">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          {{ successMessage }}
        </div>
      </div>
    </Transition>

    <!-- Loading State -->
    <div v-if="isLoading && !profile" class="space-y-6">
      <div class="orbitos-glass p-6 animate-pulse">
        <div class="flex items-start gap-6">
          <div class="w-20 h-20 rounded-full bg-white/10" />
          <div class="flex-1 space-y-3">
            <div class="h-6 w-48 bg-white/10 rounded" />
            <div class="h-4 w-32 bg-white/10 rounded" />
            <div class="h-4 w-64 bg-white/10 rounded" />
          </div>
        </div>
      </div>
      <div class="orbitos-glass p-6 animate-pulse">
        <div class="h-6 w-24 bg-white/10 rounded mb-4" />
        <div class="space-y-3">
          <div class="h-16 bg-white/10 rounded-xl" />
          <div class="h-16 bg-white/10 rounded-xl" />
          <div class="h-16 bg-white/10 rounded-xl" />
        </div>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error && !profile" class="orbitos-glass p-6">
      <div class="text-center py-8">
        <svg class="w-12 h-12 text-red-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <h3 class="text-lg font-medium text-white mb-2">Failed to load profile</h3>
        <p class="text-white/60 mb-4">{{ error }}</p>
        <button
          class="orbitos-btn-primary"
          @click="fetchProfile"
        >
          Try Again
        </button>
      </div>
    </div>

    <!-- Profile Content -->
    <div v-else-if="profile" class="space-y-6">
      <!-- Profile Card -->
      <div class="orbitos-glass p-6">
        <div class="flex flex-col sm:flex-row items-start gap-6">
          <!-- Avatar -->
          <ProfileAvatar
            :avatar-url="profile.avatarUrl"
            :display-name="profile.displayName"
            :first-name="profile.firstName"
            :last-name="profile.lastName"
            size="lg"
          />

          <!-- Info -->
          <div class="flex-1">
            <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
              <div>
                <h2 class="text-xl font-bold text-white">{{ profile.displayName }}</h2>
                <p class="text-white/60 mt-1">{{ profile.email }}</p>
                <p v-if="profile.firstName || profile.lastName" class="text-white/40 text-sm mt-1">
                  {{ [profile.firstName, profile.lastName].filter(Boolean).join(' ') }}
                </p>
              </div>
              <button
                class="orbitos-btn-secondary flex items-center gap-2"
                @click="showEditDialog = true"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                Edit Profile
              </button>
            </div>

            <!-- Stats -->
            <div class="mt-6 grid grid-cols-2 sm:grid-cols-3 gap-4">
              <div class="text-center sm:text-left">
                <p class="text-white/40 text-xs uppercase tracking-wider">Member Since</p>
                <p class="text-white font-medium mt-1">{{ formatDate(profile.createdAt) }}</p>
              </div>
              <div class="text-center sm:text-left">
                <p class="text-white/40 text-xs uppercase tracking-wider">Last Login</p>
                <p class="text-white font-medium mt-1">{{ formatDate(profile.lastLoginAt) }}</p>
              </div>
              <div class="text-center sm:text-left col-span-2 sm:col-span-1">
                <p class="text-white/40 text-xs uppercase tracking-wider">Auth Methods</p>
                <div class="flex items-center gap-2 mt-1 justify-center sm:justify-start">
                  <span
                    v-if="profile.hasPassword"
                    class="text-xs bg-purple-500/20 text-purple-300 px-2 py-0.5 rounded-full"
                  >
                    Password
                  </span>
                  <span
                    v-if="profile.hasGoogleId"
                    class="text-xs bg-blue-500/20 text-blue-300 px-2 py-0.5 rounded-full"
                  >
                    Google
                  </span>
                  <span
                    v-if="profile.hasAzureAdId"
                    class="text-xs bg-cyan-500/20 text-cyan-300 px-2 py-0.5 rounded-full"
                  >
                    Microsoft
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Security Section -->
      <SecuritySection
        :profile="profile"
        @change-password="showPasswordDialog = true"
        @link-google="handleLinkGoogle"
      />

      <!-- Danger Zone -->
      <div class="orbitos-glass p-6 border-red-500/20">
        <h2 class="text-lg font-bold text-red-400 mb-2">Danger Zone</h2>
        <p class="text-white/60 text-sm mb-4">
          Once you delete your account, there is no going back. Please be certain.
        </p>
        <button
          class="rounded-xl bg-red-500/10 border border-red-500/30 px-4 py-2.5 text-sm font-medium text-red-400 hover:bg-red-500/20 transition-all"
          disabled
        >
          Delete Account
        </button>
        <p class="text-xs text-white/30 mt-2">Account deletion is currently disabled. Contact support for assistance.</p>
      </div>
    </div>

    <!-- Edit Profile Dialog -->
    <ProfileEditDialog
      v-model="showEditDialog"
      :profile="profile"
      :is-loading="editLoading"
      :error="editError"
      @submit="handleEditSubmit"
    />

    <!-- Change Password Dialog -->
    <ChangePasswordDialog
      v-model="showPasswordDialog"
      :is-loading="passwordLoading"
      :error="passwordError"
      @submit="handlePasswordSubmit"
    />
  </div>
</template>
