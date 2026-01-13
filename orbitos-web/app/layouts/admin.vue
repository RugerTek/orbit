<script setup lang="ts">
import { useAuth } from '~/composables/useAuth'

const { user, isAuthenticated, logout } = useAuth()
const route = useRoute()

const navigation = [
  { name: 'Dashboard', href: '/admin', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6' },
  { name: 'Users', href: '/admin/users', icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z' },
  { name: 'Organizations', href: '/admin/organizations', icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4' },
  { name: 'Roles', href: '/admin/roles', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z' },
  { name: 'Functions', href: '/admin/functions', icon: 'M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4' },
]

const isActive = (href: string) => {
  if (href === '/admin') return route.path === '/admin'
  return route.path.startsWith(href)
}

const sidebarOpen = ref(false)

const getUserDisplayName = computed(() => {
  if (!user.value) return ''
  if ('displayName' in user.value) return user.value.displayName
  if ('name' in user.value) return (user.value as { name: string }).name
  return ''
})
</script>

<template>
  <div class="min-h-screen bg-slate-900">
    <!-- Mobile sidebar backdrop -->
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 z-40 bg-black/50 lg:hidden"
      @click="sidebarOpen = false"
    />

    <!-- Sidebar -->
    <aside
      :class="[
        'fixed inset-y-0 left-0 z-50 w-64 transform bg-slate-800 border-r border-slate-700 transition-transform duration-200 lg:translate-x-0',
        sidebarOpen ? 'translate-x-0' : '-translate-x-full'
      ]"
    >
      <div class="flex h-full flex-col">
        <!-- Logo -->
        <div class="flex h-16 items-center gap-3 px-6 border-b border-slate-700">
          <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-purple-500 to-blue-600">
            <svg class="h-5 w-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313-12.454z" />
            </svg>
          </div>
          <div>
            <span class="text-lg font-bold text-white">OrbitOS</span>
            <span class="ml-2 rounded bg-purple-500/20 px-2 py-0.5 text-xs font-medium text-purple-300">Admin</span>
          </div>
        </div>

        <!-- Navigation -->
        <nav class="flex-1 space-y-1 px-3 py-4">
          <NuxtLink
            v-for="item in navigation"
            :key="item.name"
            :to="item.href"
            :class="[
              'group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all',
              isActive(item.href)
                ? 'bg-purple-500/20 text-purple-300'
                : 'text-slate-400 hover:bg-slate-700/50 hover:text-white'
            ]"
            @click="sidebarOpen = false"
          >
            <svg
              :class="[
                'h-5 w-5 flex-shrink-0 transition-colors',
                isActive(item.href) ? 'text-purple-400' : 'text-slate-500 group-hover:text-slate-300'
              ]"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="item.icon" />
            </svg>
            {{ item.name }}
          </NuxtLink>
        </nav>

        <!-- User section -->
        <div class="border-t border-slate-700 p-4">
          <div class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-medium text-white">
              {{ getUserDisplayName.charAt(0).toUpperCase() }}
            </div>
            <div class="flex-1 min-w-0">
              <p class="truncate text-sm font-medium text-white">{{ getUserDisplayName }}</p>
              <p class="truncate text-xs text-slate-400">Super Admin</p>
            </div>
          </div>
          <div class="mt-3 flex gap-2">
            <NuxtLink
              to="/"
              class="flex-1 rounded-lg bg-slate-700 px-3 py-2 text-center text-xs font-medium text-slate-300 hover:bg-slate-600 transition-colors"
            >
              Exit Admin
            </NuxtLink>
            <button
              @click="logout"
              class="flex-1 rounded-lg bg-red-500/20 px-3 py-2 text-xs font-medium text-red-400 hover:bg-red-500/30 transition-colors"
            >
              Sign Out
            </button>
          </div>
        </div>
      </div>
    </aside>

    <!-- Main content -->
    <div class="lg:pl-64">
      <!-- Top bar -->
      <header class="sticky top-0 z-30 flex h-16 items-center gap-4 border-b border-slate-700 bg-slate-900/95 backdrop-blur px-4 lg:px-8">
        <button
          class="lg:hidden rounded-lg p-2 text-slate-400 hover:bg-slate-800 hover:text-white"
          @click="sidebarOpen = true"
        >
          <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <div class="flex-1" />
      </header>

      <!-- Page content -->
      <main class="p-4 lg:p-8">
        <slot />
      </main>
    </div>
  </div>
</template>
