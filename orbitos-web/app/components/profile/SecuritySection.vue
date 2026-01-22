<script setup lang="ts">
import type { UserProfile } from '~/types/user'

interface Props {
  profile: UserProfile | null
}

defineProps<Props>()

const emit = defineEmits<{
  (e: 'changePassword'): void
  (e: 'linkGoogle'): void
}>()

const formatDate = (dateStr?: string) => {
  if (!dateStr) return 'Never'
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<template>
  <div class="orbitos-glass p-6">
    <h2 class="orbitos-heading-sm mb-4">Security</h2>

    <div class="space-y-4">
      <!-- Sign-in Methods -->
      <div>
        <h3 class="text-sm font-medium text-white/60 mb-3">Sign-in Methods</h3>
        <div class="space-y-3">
          <!-- Password -->
          <div class="flex items-center justify-between p-3 rounded-xl bg-white/5 border border-white/10">
            <div class="flex items-center gap-3">
              <div
                class="w-10 h-10 rounded-lg flex items-center justify-center"
                :class="profile?.hasPassword ? 'bg-emerald-500/20 text-emerald-400' : 'bg-white/5 text-white/30'"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Password</p>
                <p class="text-xs text-white/40">
                  {{ profile?.hasPassword ? 'Password authentication enabled' : 'Not set up' }}
                </p>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span
                v-if="profile?.hasPassword"
                class="flex items-center gap-1 text-xs text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded-full"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                Active
              </span>
              <button
                v-if="profile?.hasPassword"
                class="text-xs text-purple-400 hover:text-purple-300 transition-colors"
                @click="emit('changePassword')"
              >
                Change
              </button>
            </div>
          </div>

          <!-- Google -->
          <div class="flex items-center justify-between p-3 rounded-xl bg-white/5 border border-white/10">
            <div class="flex items-center gap-3">
              <div
                class="w-10 h-10 rounded-lg flex items-center justify-center"
                :class="profile?.hasGoogleId ? 'bg-emerald-500/20 text-emerald-400' : 'bg-white/5 text-white/30'"
              >
                <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                  <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                  <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" />
                  <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Google</p>
                <p class="text-xs text-white/40">
                  {{ profile?.hasGoogleId ? 'Google account linked' : 'Not connected' }}
                </p>
              </div>
            </div>
            <div>
              <span
                v-if="profile?.hasGoogleId"
                class="flex items-center gap-1 text-xs text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded-full"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                Linked
              </span>
              <button
                v-else
                class="text-xs text-purple-400 hover:text-purple-300 transition-colors bg-purple-500/10 px-3 py-1.5 rounded-lg border border-purple-500/20"
                @click="emit('linkGoogle')"
              >
                Link Account
              </button>
            </div>
          </div>

          <!-- Microsoft -->
          <div class="flex items-center justify-between p-3 rounded-xl bg-white/5 border border-white/10">
            <div class="flex items-center gap-3">
              <div
                class="w-10 h-10 rounded-lg flex items-center justify-center"
                :class="profile?.hasAzureAdId ? 'bg-emerald-500/20 text-emerald-400' : 'bg-white/5 text-white/30'"
              >
                <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M11.4 24H0V12.6h11.4V24zM24 24H12.6V12.6H24V24zM11.4 11.4H0V0h11.4v11.4zm12.6 0H12.6V0H24v11.4z" />
                </svg>
              </div>
              <div>
                <p class="text-sm font-medium text-white">Microsoft</p>
                <p class="text-xs text-white/40">
                  {{ profile?.hasAzureAdId ? 'Microsoft account linked' : 'Not connected' }}
                </p>
              </div>
            </div>
            <span
              v-if="profile?.hasAzureAdId"
              class="flex items-center gap-1 text-xs text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded-full"
            >
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              Linked
            </span>
            <span v-else class="text-xs text-white/30">
              Not available
            </span>
          </div>
        </div>
      </div>

      <!-- Session Info -->
      <div class="pt-4 border-t border-white/10">
        <h3 class="text-sm font-medium text-white/60 mb-3">Session Information</h3>
        <div class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <p class="text-white/40 text-xs">Last Login</p>
            <p class="text-white">{{ formatDate(profile?.lastLoginAt) }}</p>
          </div>
          <div>
            <p class="text-white/40 text-xs">Account Created</p>
            <p class="text-white">{{ formatDate(profile?.createdAt) }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
