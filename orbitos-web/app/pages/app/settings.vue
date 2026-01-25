<script setup lang="ts">
definePageMeta({
  layout: 'app'
})

const { authToken, initializeAuth, logout } = useAuth()
const { profile, fetchProfile } = useUserProfile()

// Delete account dialog state
const showDeleteDialog = ref(false)
const deleteConfirmText = ref('')
const isDeleting = ref(false)

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

  if (!profile.value) {
    await fetchProfile()
  }
})

// Check if delete confirmation matches
const canDelete = computed(() => {
  return deleteConfirmText.value === 'DELETE'
})

// Handle delete account (placeholder - not actually implemented)
const handleDeleteAccount = async () => {
  if (!canDelete.value) return
  isDeleting.value = true

  // Simulate deletion
  await new Promise(resolve => setTimeout(resolve, 1500))

  // In a real implementation, this would call an API endpoint
  // For now, just log out
  await logout()
}

// Reset delete dialog when closing
watch(showDeleteDialog, (isOpen) => {
  if (!isOpen) {
    deleteConfirmText.value = ''
  }
})
</script>

<template>
  <div class="max-w-4xl mx-auto">
    <!-- Page Header -->
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-white">Settings</h1>
      <p class="text-white/60 mt-1">Manage your application preferences</p>
    </div>

    <div class="space-y-6">
      <!-- Account Section -->
      <div class="orbitos-glass p-6">
        <h2 class="orbitos-heading-sm mb-4">Account</h2>

        <div class="space-y-4">
          <!-- Email -->
          <div class="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/10">
            <div>
              <p class="text-sm font-medium text-white">Email Address</p>
              <p class="text-xs text-white/40 mt-0.5">{{ profile?.email || 'Loading...' }}</p>
            </div>
            <span class="text-xs text-white/30 bg-white/5 px-2 py-1 rounded">Cannot be changed</span>
          </div>

          <!-- Profile Link -->
          <NuxtLink
            to="/app/profile"
            class="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/10 hover:bg-white/10 transition-colors"
          >
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-lg bg-purple-500/20 flex items-center justify-center text-purple-400">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Edit Profile</p>
                <p class="text-xs text-white/40">Update your name and personal information</p>
              </div>
            </div>
            <svg class="w-5 h-5 text-white/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </NuxtLink>

          <!-- Security Link -->
          <NuxtLink
            to="/app/profile"
            class="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/10 hover:bg-white/10 transition-colors"
          >
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-lg bg-emerald-500/20 flex items-center justify-center text-emerald-400">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Security</p>
                <p class="text-xs text-white/40">Manage passwords and sign-in methods</p>
              </div>
            </div>
            <svg class="w-5 h-5 text-white/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </NuxtLink>

          <!-- AI Usage Analytics Link -->
          <NuxtLink
            to="/app/usage"
            class="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/10 hover:bg-white/10 transition-colors"
          >
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-lg bg-purple-500/20 flex items-center justify-center text-purple-400">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">AI Usage Analytics</p>
                <p class="text-xs text-white/40">View token consumption, costs, and model usage</p>
              </div>
            </div>
            <svg class="w-5 h-5 text-white/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </NuxtLink>
        </div>
      </div>

      <!-- Preferences Section -->
      <div class="orbitos-glass p-6">
        <h2 class="orbitos-heading-sm mb-4">Preferences</h2>

        <div class="space-y-4">
          <!-- Theme (placeholder) -->
          <div class="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/10">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-lg bg-indigo-500/20 flex items-center justify-center text-indigo-400">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Theme</p>
                <p class="text-xs text-white/40">Dark mode is enabled by default</p>
              </div>
            </div>
            <span class="text-xs text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded-full">Dark</span>
          </div>

          <!-- Language (placeholder) -->
          <div class="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/10">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-lg bg-blue-500/20 flex items-center justify-center text-blue-400">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5h12M9 3v2m1.048 9.5A18.022 18.022 0 016.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Language</p>
                <p class="text-xs text-white/40">Currently set to English</p>
              </div>
            </div>
            <span class="text-xs text-white/50 bg-white/5 px-2 py-1 rounded-full">English</span>
          </div>

          <!-- Notifications (placeholder) -->
          <div class="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/10">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-lg bg-yellow-500/20 flex items-center justify-center text-yellow-400">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Notifications</p>
                <p class="text-xs text-white/40">In-app notifications are enabled</p>
              </div>
            </div>
            <span class="text-xs text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded-full">On</span>
          </div>
        </div>

        <p class="text-xs text-white/30 mt-4">
          Additional preference settings will be available in a future update.
        </p>
      </div>

      <!-- Danger Zone -->
      <div class="orbitos-glass p-6 border-red-500/20">
        <h2 class="text-lg font-bold text-red-400 mb-2">Danger Zone</h2>
        <p class="text-white/60 text-sm mb-4">
          Irreversible actions that will permanently affect your account.
        </p>

        <div class="space-y-3">
          <!-- Delete Account -->
          <div class="flex items-center justify-between p-4 rounded-xl bg-red-500/5 border border-red-500/20">
            <div>
              <p class="text-sm font-medium text-white">Delete Account</p>
              <p class="text-xs text-white/40">Permanently delete your account and all data</p>
            </div>
            <button
              class="text-xs text-red-400 hover:text-red-300 bg-red-500/10 hover:bg-red-500/20 px-3 py-1.5 rounded-lg border border-red-500/20 transition-all"
              @click="showDeleteDialog = true"
            >
              Delete Account
            </button>
          </div>
        </div>
      </div>

      <!-- About Section -->
      <div class="orbitos-glass p-6">
        <h2 class="orbitos-heading-sm mb-4">About</h2>

        <div class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <p class="text-white/40 text-xs">Version</p>
            <p class="text-white">1.0.0-beta</p>
          </div>
          <div>
            <p class="text-white/40 text-xs">Build</p>
            <p class="text-white">{{ new Date().toISOString().split('T')[0] }}</p>
          </div>
        </div>

        <div class="mt-4 pt-4 border-t border-white/10">
          <p class="text-xs text-white/40">
            OrbitOS - AI-native business operating system.
            Built with Nuxt 4, Vue 3, and .NET 8.
          </p>
        </div>
      </div>
    </div>

    <!-- Delete Account Dialog -->
    <BaseDialog
      v-model="showDeleteDialog"
      title="Delete Account"
      subtitle="This action cannot be undone"
      size="md"
    >
      <div class="space-y-4">
        <div class="rounded-xl bg-red-500/10 border border-red-500/30 p-4">
          <div class="flex gap-3">
            <svg class="w-5 h-5 text-red-400 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <div class="text-sm text-red-400">
              <p class="font-medium">Warning: This action is irreversible</p>
              <p class="mt-1 text-red-400/80">
                All your data, including organizations you own, will be permanently deleted.
                This cannot be undone.
              </p>
            </div>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-white/80 mb-2">
            Type <span class="font-mono text-red-400">DELETE</span> to confirm
          </label>
          <input
            v-model="deleteConfirmText"
            type="text"
            class="orbitos-input w-full"
            placeholder="Type DELETE to confirm"
            :disabled="isDeleting"
          />
        </div>
      </div>

      <template #footer="{ close }">
        <div class="flex justify-end gap-3">
          <button
            type="button"
            class="orbitos-btn-secondary"
            :disabled="isDeleting"
            @click="close"
          >
            Cancel
          </button>
          <button
            type="button"
            class="rounded-xl bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-600 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="!canDelete || isDeleting"
            @click="handleDeleteAccount"
          >
            <span v-if="isDeleting" class="flex items-center gap-2">
              <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
              Deleting...
            </span>
            <span v-else>Delete My Account</span>
          </button>
        </div>
      </template>
    </BaseDialog>
  </div>
</template>
