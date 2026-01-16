<script setup lang="ts">
import { useAuth } from '~/composables/useAuth'

const { user, logout } = useAuth()
const { openSpotlight, setupKeyboardShortcuts, loadHelpIndex } = useHelp()
const route = useRoute()

// Initialize help system keyboard shortcuts
setupKeyboardShortcuts()

const navigation = [
  { name: 'Overview', href: '/app', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' },
  { name: 'Canvas', href: '/app/canvas', icon: 'M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z' },
  { name: 'Goals', href: '/app/goals', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
  { name: 'Health', href: '/app/health', icon: 'M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z' },
  { name: 'People', href: '/app/people', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z' },
  { name: 'Roles', href: '/app/roles', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z' },
  { name: 'Functions', href: '/app/functions', icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4' },
  { name: 'Processes', href: '/app/processes', icon: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15' },
  { name: 'Resources', href: '/app/resources', icon: 'M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z' },
]

const isActive = (href: string) => {
  if (href === '/app') return route.path === '/app'
  return route.path.startsWith(href)
}

const sidebarOpen = ref(false)

const getUserDisplayName = computed(() => {
  if (!user.value) return ''
  if ('displayName' in user.value) return user.value.displayName
  if ('name' in user.value) return (user.value as { name: string }).name
  if ('username' in user.value) return (user.value as { username: string }).username
  return ''
})
</script>

<template>
  <div class="orbitos-bg">
    <!-- Animated background blobs -->
    <div class="fixed inset-0 overflow-hidden pointer-events-none">
      <div class="orbitos-blob-purple absolute -top-40 -right-40" style="width: 20rem; height: 20rem;"></div>
      <div class="orbitos-blob-blue absolute top-1/3 -left-40 animation-delay-1000" style="width: 20rem; height: 20rem;"></div>
      <div class="orbitos-blob-indigo absolute -bottom-40 right-1/3 animation-delay-500" style="width: 24rem; height: 24rem;"></div>
    </div>

    <!-- Mobile sidebar backdrop -->
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm lg:hidden"
      @click="sidebarOpen = false"
    />

    <!-- Sidebar -->
    <aside
      :class="[
        'fixed inset-y-0 left-0 z-50 w-64 transform transition-transform duration-300 lg:translate-x-0',
        sidebarOpen ? 'translate-x-0' : '-translate-x-full'
      ]"
    >
      <div class="flex h-full flex-col backdrop-blur-xl bg-white/10 border-r border-white/20">
        <!-- Logo -->
        <div class="flex h-16 items-center gap-3 px-6 border-b border-white/20">
          <div class="flex items-center justify-center rounded-xl bg-gradient-to-br from-purple-500 to-blue-600 shadow-lg shadow-purple-500/30" style="width: 2.5rem; height: 2.5rem;">
            <svg class="text-white" style="width: 1.25rem; height: 1.25rem;" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 4a2 2 0 0 0 2 2a2 2 0 0 0 -2 2a2 2 0 0 0 -2 -2a2 2 0 0 0 2 -2" />
            </svg>
          </div>
          <div>
            <span class="text-lg font-bold text-white">OrbitOS</span>
            <span class="ml-2 rounded-full bg-purple-500/20 border border-purple-500/30 px-2 py-0.5 text-xs font-medium text-purple-300">Ops</span>
          </div>
        </div>

        <!-- Navigation -->
        <nav class="flex-1 space-y-1 px-3 py-4 overflow-y-auto">
          <NuxtLink
            v-for="item in navigation"
            :key="item.name"
            :to="item.href"
            :class="[
              'group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200',
              isActive(item.href)
                ? 'bg-purple-500/20 border border-purple-500/30 text-white'
                : 'text-white/60 hover:bg-white/10 hover:text-white border border-transparent'
            ]"
            @click="sidebarOpen = false"
          >
            <svg
              :class="[
                'h-5 w-5 flex-shrink-0 transition-colors',
                isActive(item.href) ? 'text-purple-400' : 'text-white/40 group-hover:text-purple-400'
              ]"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="item.icon" />
            </svg>
            {{ item.name }}
            <span v-if="isActive(item.href)" class="ml-auto h-2 w-2 rounded-full bg-purple-400 animate-pulse"></span>
          </NuxtLink>
        </nav>

        <!-- User section -->
        <div class="border-t border-white/20 p-4">
          <div class="flex items-center gap-3 rounded-xl bg-white/10 p-3">
            <div class="flex items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white shadow-lg shadow-purple-500/30" style="width: 2.5rem; height: 2.5rem;">
              {{ getUserDisplayName ? getUserDisplayName.charAt(0).toUpperCase() : 'O' }}
            </div>
            <div class="flex-1 min-w-0">
              <p class="truncate text-sm font-medium text-white">{{ getUserDisplayName || 'Operator' }}</p>
              <p class="truncate text-xs text-white/40">Operations Team</p>
            </div>
          </div>
          <div class="mt-3 flex gap-2">
            <NuxtLink
              to="/"
              class="flex-1 rounded-xl bg-white/5 border border-white/10 px-3 py-2.5 text-center text-xs font-medium text-white/70 hover:bg-white/10 hover:text-white transition-all"
            >
              Back Home
            </NuxtLink>
            <button
              class="flex-1 rounded-xl bg-red-500/10 border border-red-500/20 px-3 py-2.5 text-xs font-medium text-red-400 hover:bg-red-500/20 transition-all"
              @click="logout"
            >
              Sign Out
            </button>
          </div>
        </div>
      </div>
    </aside>

    <!-- Main content area -->
    <div class="lg:pl-64 relative">
      <!-- Header -->
      <header class="sticky top-0 z-30 flex h-16 items-center gap-4 backdrop-blur-xl bg-white/5 border-b border-white/10 px-4 lg:px-8">
        <button
          class="lg:hidden rounded-xl p-2.5 text-white/60 hover:bg-white/10 hover:text-white transition-all border border-white/10"
          @click="sidebarOpen = true"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <!-- Search bar (opens help spotlight) -->
        <div class="flex-1 max-w-xl">
          <button
            class="w-full orbitos-input flex items-center gap-2 cursor-pointer text-left"
            @click="openSpotlight"
          >
            <svg class="h-4 w-4 text-white/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <span class="text-white/30 text-sm">Search help & operations...</span>
            <span class="ml-auto hidden sm:flex items-center gap-1 text-xs text-white/20">
              <kbd class="rounded bg-white/10 px-1.5 py-0.5">⌘</kbd>
              <kbd class="rounded bg-white/10 px-1.5 py-0.5">K</kbd>
            </span>
          </button>
        </div>

        <!-- Status indicator -->
        <div class="hidden sm:flex items-center gap-2 rounded-full bg-emerald-500/10 border border-emerald-500/20 px-4 py-2 text-xs text-emerald-400">
          <span class="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
          Org health: Stable
        </div>

        <!-- Notifications -->
        <button class="rounded-xl p-2.5 text-white/60 hover:bg-white/10 hover:text-white transition-all border border-white/10 relative">
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
          </svg>
          <span class="absolute top-1.5 right-1.5 h-2 w-2 rounded-full bg-purple-500"></span>
        </button>
      </header>

      <!-- Page content -->
      <main class="p-4 lg:p-8">
        <slot />
      </main>
    </div>

    <!-- AI Assistant -->
    <AiAssistant />

    <!-- Help System Components -->
    <HelpHelpSpotlight />
    <HelpHelpPanel />
    <HelpWalkthroughOverlay />
    <HelpHelpButton />
  </div>
</template>
