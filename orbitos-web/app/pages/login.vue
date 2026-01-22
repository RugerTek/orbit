<script setup lang="ts">
import { useAuth } from '~/composables/useAuth'

const { user, isAuthenticated, isLoading, login, loginWithEmail, loginWithGoogle, loginWithGoogleCode, logout, initializeAuth } = useAuth()

const config = useRuntimeConfig()
const email = ref('')
const password = ref('')
const loginError = ref('')
const isLoggingIn = ref(false)
const isGoogleLoading = ref(false)

onMounted(() => {
  initializeAuth()
  loadGoogleScript()
})

const loadGoogleScript = () => {
  if (document.getElementById('google-signin-script')) return

  const script = document.createElement('script')
  script.id = 'google-signin-script'
  script.src = 'https://accounts.google.com/gsi/client'
  script.async = true
  script.defer = true
  script.onload = initializeGoogleSignIn
  document.head.appendChild(script)
}

const initializeGoogleSignIn = () => {
  if (typeof google === 'undefined') return

  // Initialize for One Tap (may not work on localhost due to FedCM)
  google.accounts.id.initialize({
    client_id: config.public.googleClientId,
    callback: handleGoogleCallback,
    auto_select: false,
    use_fedcm_for_prompt: false,
  })

  // Also initialize OAuth2 client for popup flow (more reliable on localhost)
  googleOAuth2Client.value = google.accounts.oauth2.initCodeClient({
    client_id: config.public.googleClientId,
    scope: 'email profile openid',
    ux_mode: 'popup',
    callback: handleOAuth2Response,
  })
}

const googleOAuth2Client = ref<{ requestCode: () => void } | null>(null)

const handleGoogleCallback = async (response: { credential: string }) => {
  loginError.value = ''
  isGoogleLoading.value = true
  try {
    await loginWithGoogle(response.credential)
  } catch (error: unknown) {
    if (error && typeof error === 'object' && 'data' in error) {
      const errData = error as { data?: { Message?: string } }
      loginError.value = errData.data?.Message || 'Google sign-in failed'
    } else {
      loginError.value = 'Google sign-in failed'
    }
  } finally {
    isGoogleLoading.value = false
  }
}

const handleOAuth2Response = async (response: { code: string }) => {
  loginError.value = ''
  isGoogleLoading.value = true
  try {
    // Exchange auth code for token on backend
    await loginWithGoogleCode(response.code)
  } catch (error: unknown) {
    if (error && typeof error === 'object' && 'data' in error) {
      const errData = error as { data?: { Message?: string } }
      loginError.value = errData.data?.Message || 'Google sign-in failed'
    } else {
      loginError.value = 'Google sign-in failed'
    }
  } finally {
    isGoogleLoading.value = false
  }
}

const triggerGoogleSignIn = () => {
  if (googleOAuth2Client.value) {
    // Use OAuth2 popup flow (works better on localhost)
    googleOAuth2Client.value.requestCode()
  } else if (typeof google !== 'undefined') {
    // Fallback to One Tap
    google.accounts.id.prompt()
  }
}

const handleEmailLogin = async () => {
  loginError.value = ''
  isLoggingIn.value = true
  try {
    await loginWithEmail(email.value, password.value)
  } catch (error: unknown) {
    if (error && typeof error === 'object' && 'data' in error) {
      const errData = error as { data?: { Message?: string } }
      loginError.value = errData.data?.Message || 'Invalid email or password'
    } else {
      loginError.value = 'Invalid email or password'
    }
  } finally {
    isLoggingIn.value = false
  }
}

const getUserDisplayName = computed(() => {
  if (!user.value) return ''
  if ('displayName' in user.value) return user.value.displayName
  if ('name' in user.value) return user.value.name
  return user.value.username
})

const getUserEmail = computed(() => {
  if (!user.value) return ''
  if ('email' in user.value) return user.value.email
  if ('username' in user.value) return user.value.username
  return ''
})

declare const google: {
  accounts: {
    id: {
      initialize: (config: { client_id: string; callback: (response: { credential: string }) => void; auto_select: boolean; use_fedcm_for_prompt?: boolean }) => void
      prompt: () => void
      renderButton: (element: HTMLElement, options: Record<string, unknown>) => void
    }
    oauth2: {
      initCodeClient: (config: { client_id: string; scope: string; ux_mode: string; callback: (response: { code: string }) => void }) => { requestCode: () => void }
    }
  }
}
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-slate-900 via-purple-900 to-slate-900 flex items-center justify-center p-4 relative overflow-hidden">
    <!-- Animated background elements -->
    <div class="absolute inset-0 overflow-hidden pointer-events-none">
      <div class="absolute -top-40 -right-40 bg-purple-500 rounded-full mix-blend-multiply filter blur-3xl opacity-20 animate-pulse" style="width: 20rem; height: 20rem;"></div>
      <div class="absolute -bottom-40 -left-40 bg-blue-500 rounded-full mix-blend-multiply filter blur-3xl opacity-20 animate-pulse delay-1000" style="width: 20rem; height: 20rem;"></div>
      <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-indigo-500 rounded-full mix-blend-multiply filter blur-3xl opacity-10 animate-pulse delay-500" style="width: 24rem; height: 24rem;"></div>
    </div>

    <!-- Glass card -->
    <div class="relative backdrop-blur-xl bg-white/10 border border-white/20 rounded-3xl shadow-2xl p-8 w-full max-w-md">
      <!-- Back to home -->
      <NuxtLink to="/" class="absolute top-4 left-4 text-white/40 hover:text-white transition-colors flex items-center gap-1 text-sm">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
        Back
      </NuxtLink>

      <!-- Logo/Brand -->
      <div class="text-center mb-8">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-purple-500 to-blue-600 mb-4 shadow-lg shadow-purple-500/30" style="width: 64px; height: 64px;">
          <svg class="w-8 h-8 text-white" style="width: 32px; height: 32px;" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z"></path>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 4a2 2 0 0 0 2 2a2 2 0 0 0 -2 2a2 2 0 0 0 -2 -2a2 2 0 0 0 2 -2"></path>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11h2m-1 -1v2"></path>
          </svg>
        </div>
        <h1 class="text-3xl font-bold text-white tracking-tight">OrbitOS</h1>
        <p class="text-white/60 mt-2 text-sm">AI-Native Business Operating System</p>
      </div>

      <!-- Loading state -->
      <div v-if="isLoading" class="flex flex-col items-center justify-center py-8">
        <div class="w-10 h-10 border-4 border-purple-500/30 border-t-purple-500 rounded-full animate-spin mb-4"></div>
        <p class="text-white/60 text-sm">Initializing...</p>
      </div>

      <!-- Authenticated state -->
      <div v-else-if="isAuthenticated" class="space-y-6">
        <div class="text-center py-4">
          <div class="inline-flex items-center justify-center w-20 h-20 rounded-full bg-gradient-to-br from-green-400 to-emerald-600 mb-4 shadow-lg shadow-green-500/30">
            <svg class="w-10 h-10 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
            </svg>
          </div>
          <p class="text-white/60 text-sm">Welcome back</p>
          <p class="text-xl font-semibold text-white mt-1">{{ getUserDisplayName }}</p>
          <p class="text-white/40 text-sm mt-1">{{ getUserEmail }}</p>
        </div>

        <div class="space-y-3">
          <NuxtLink
            to="/admin"
            class="group relative flex items-center justify-center w-full py-4 px-6 bg-gradient-to-r from-purple-600 to-blue-600 rounded-2xl font-semibold text-white shadow-lg shadow-purple-500/30 hover:shadow-purple-500/50 transition-all duration-300 hover:scale-[1.02]"
          >
            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            <span>Super Admin Panel</span>
            <svg class="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6"></path>
            </svg>
          </NuxtLink>

          <NuxtLink
            to="/app"
            class="group relative flex items-center justify-center w-full py-4 px-6 bg-white/10 border border-white/20 rounded-2xl font-semibold text-white shadow-lg shadow-slate-900/30 hover:bg-white/15 transition-all duration-300"
          >
            <svg class="w-5 h-5 mr-2 text-purple-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
            </svg>
            <span>Operations App</span>
            <svg class="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6"></path>
            </svg>
          </NuxtLink>

          <button
            @click="logout"
            class="w-full py-3 px-6 bg-white/5 border border-white/10 rounded-2xl font-medium text-white/70 hover:bg-white/10 hover:text-white transition-all duration-300"
          >
            Sign Out
          </button>
        </div>
      </div>

      <!-- Login state -->
      <div v-else class="space-y-6">
        <!-- Email/Password Form -->
        <form @submit.prevent="handleEmailLogin" class="space-y-4">
          <div>
            <label for="email" class="block text-white/60 text-sm mb-2">Email</label>
            <input
              id="email"
              v-model="email"
              type="email"
              required
              placeholder="you@company.com"
              class="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-white/30 focus:outline-none focus:border-purple-500 focus:ring-1 focus:ring-purple-500 transition-all"
            />
          </div>
          <div>
            <label for="password" class="block text-white/60 text-sm mb-2">Password</label>
            <input
              id="password"
              v-model="password"
              type="password"
              required
              placeholder="Enter your password"
              class="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-white/30 focus:outline-none focus:border-purple-500 focus:ring-1 focus:ring-purple-500 transition-all"
            />
          </div>

          <!-- Error message -->
          <div v-if="loginError" class="px-4 py-3 bg-red-500/10 border border-red-500/20 rounded-xl">
            <p class="text-red-400 text-sm text-center">{{ loginError }}</p>
          </div>

          <button
            type="submit"
            :disabled="isLoggingIn"
            class="group relative flex items-center justify-center w-full py-4 px-6 bg-gradient-to-r from-purple-600 to-blue-600 rounded-2xl font-semibold text-white shadow-lg shadow-purple-500/30 hover:shadow-purple-500/50 transition-all duration-300 hover:scale-[1.02] disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100"
          >
            <div v-if="isLoggingIn" class="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin mr-2"></div>
            <span>{{ isLoggingIn ? 'Signing in...' : 'Sign In' }}</span>
          </button>
        </form>

        <div class="flex items-center gap-4">
          <div class="flex-1 h-px bg-white/10"></div>
          <span class="text-white/30 text-xs uppercase tracking-wider">or continue with</span>
          <div class="flex-1 h-px bg-white/10"></div>
        </div>

        <!-- Social Login Buttons -->
        <div class="space-y-3">
          <!-- Google Sign-In Button -->
          <button
            @click="triggerGoogleSignIn"
            :disabled="isGoogleLoading"
            class="group relative flex items-center justify-center w-full py-4 px-6 bg-white rounded-2xl font-semibold text-gray-700 hover:bg-gray-50 transition-all duration-300 disabled:opacity-50"
          >
            <div v-if="isGoogleLoading" class="w-5 h-5 border-2 border-gray-300 border-t-gray-600 rounded-full animate-spin mr-3"></div>
            <svg v-else class="w-5 h-5 mr-3" viewBox="0 0 24 24">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
            </svg>
            <span>{{ isGoogleLoading ? 'Signing in...' : 'Sign in with Google' }}</span>
          </button>

          <!-- Microsoft SSO Button -->
          <button
            @click="login"
            class="group relative flex items-center justify-center w-full py-4 px-6 bg-white/5 border border-white/10 rounded-2xl font-semibold text-white hover:bg-white/10 transition-all duration-300"
          >
            <svg class="w-5 h-5 mr-3" viewBox="0 0 21 21" fill="currentColor">
              <path d="M0 0h10v10H0V0zm11 0h10v10H11V0zM0 11h10v10H0V11zm11 0h10v10H11V11z"/>
            </svg>
            <span>Sign in with Microsoft</span>
          </button>
        </div>

        <div class="flex items-center justify-center gap-6 text-white/40 pt-2">
          <div class="flex items-center gap-2 text-xs">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
            </svg>
            <span>SOC 2 Compliant</span>
          </div>
          <div class="flex items-center gap-2 text-xs">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
            </svg>
            <span>Enterprise Ready</span>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="mt-8 pt-6 border-t border-white/10 text-center">
        <p class="text-white/30 text-xs">&copy; 2026 OrbitOS. All rights reserved.</p>
      </div>
    </div>
  </div>
</template>

<style>
@keyframes pulse {
  0%, 100% { opacity: 0.2; transform: scale(1); }
  50% { opacity: 0.3; transform: scale(1.05); }
}

.animate-pulse {
  animation: pulse 4s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

.delay-500 {
  animation-delay: 500ms;
}

.delay-1000 {
  animation-delay: 1000ms;
}
</style>
