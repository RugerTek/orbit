<script setup lang="ts">
import { useOrganizations } from '~/composables/useOrganizations'

const { organizations, currentOrganization, setCurrentOrganization } = useOrganizations()

const emit = defineEmits<{
  (e: 'create-org'): void
}>()

const showDropdown = ref(false)
const dropdownRef = ref<HTMLElement | null>(null)

// Close dropdown when clicking outside
const handleClickOutside = (event: MouseEvent) => {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    showDropdown.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})

const selectOrganization = (orgId: string) => {
  setCurrentOrganization(orgId)
  showDropdown.value = false
  // Reload the page to refresh all data for the new org
  window.location.reload()
}

const openCreateDialog = () => {
  showDropdown.value = false
  emit('create-org')
}

// Get initials for org avatar
const getOrgInitials = (name: string) => {
  return name
    .split(' ')
    .map(word => word[0])
    .join('')
    .toUpperCase()
    .slice(0, 2)
}
</script>

<template>
  <div ref="dropdownRef" class="relative">
    <!-- Current Organization Button -->
    <button
      class="w-full flex items-center gap-3 rounded-xl bg-white/10 p-3 hover:bg-white/15 transition-all cursor-pointer text-left"
      @click="showDropdown = !showDropdown"
    >
      <!-- Org Avatar -->
      <div
        v-if="currentOrganization?.logoUrl"
        class="flex-shrink-0 w-10 h-10 rounded-lg overflow-hidden"
      >
        <img :src="currentOrganization.logoUrl" :alt="currentOrganization.name" class="w-full h-full object-cover" />
      </div>
      <div
        v-else
        class="flex-shrink-0 flex items-center justify-center w-10 h-10 rounded-lg bg-gradient-to-br from-purple-500 to-blue-600 text-sm font-bold text-white"
      >
        {{ currentOrganization ? getOrgInitials(currentOrganization.name) : 'O' }}
      </div>

      <!-- Org Info -->
      <div class="flex-1 min-w-0">
        <p class="truncate text-sm font-medium text-white">
          {{ currentOrganization?.name || 'Select Organization' }}
        </p>
        <p class="truncate text-xs text-white/40">
          {{ currentOrganization?.role || 'No organization selected' }}
        </p>
      </div>

      <!-- Chevron -->
      <svg
        class="w-4 h-4 text-white/40 transition-transform"
        :class="{ 'rotate-180': showDropdown }"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <!-- Dropdown Menu -->
    <Transition
      enter-active-class="transition duration-100 ease-out"
      enter-from-class="transform scale-95 opacity-0"
      enter-to-class="transform scale-100 opacity-100"
      leave-active-class="transition duration-75 ease-in"
      leave-from-class="transform scale-100 opacity-100"
      leave-to-class="transform scale-95 opacity-0"
    >
      <div
        v-if="showDropdown"
        class="absolute bottom-full left-0 right-0 mb-2 rounded-xl bg-slate-800/95 backdrop-blur-xl border border-white/20 shadow-xl overflow-hidden z-50"
      >
        <!-- Organizations List -->
        <div class="max-h-64 overflow-y-auto py-2">
          <p class="px-3 py-1.5 text-xs font-medium text-white/40 uppercase tracking-wider">
            Your Organizations
          </p>
          <button
            v-for="org in organizations"
            :key="org.id"
            class="w-full flex items-center gap-3 px-3 py-2 hover:bg-white/10 transition-colors text-left"
            :class="{ 'bg-purple-500/20': org.id === currentOrganization?.id }"
            @click="selectOrganization(org.id)"
          >
            <!-- Org Avatar -->
            <div
              v-if="org.logoUrl"
              class="flex-shrink-0 w-8 h-8 rounded-lg overflow-hidden"
            >
              <img :src="org.logoUrl" :alt="org.name" class="w-full h-full object-cover" />
            </div>
            <div
              v-else
              class="flex-shrink-0 flex items-center justify-center w-8 h-8 rounded-lg bg-gradient-to-br from-purple-500/50 to-blue-600/50 text-xs font-bold text-white"
            >
              {{ getOrgInitials(org.name) }}
            </div>

            <!-- Org Info -->
            <div class="flex-1 min-w-0">
              <p class="truncate text-sm font-medium text-white">{{ org.name }}</p>
              <p class="truncate text-xs text-white/40">{{ org.role }}</p>
            </div>

            <!-- Check mark for current -->
            <svg
              v-if="org.id === currentOrganization?.id"
              class="w-4 h-4 text-purple-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </button>

          <!-- Empty state -->
          <p v-if="organizations.length === 0" class="px-3 py-4 text-sm text-white/40 text-center">
            No organizations yet
          </p>
        </div>

        <!-- Divider -->
        <div class="border-t border-white/10" />

        <!-- Create New Organization -->
        <button
          class="w-full flex items-center gap-3 px-3 py-3 hover:bg-white/10 transition-colors text-left"
          @click="openCreateDialog"
        >
          <div class="flex-shrink-0 flex items-center justify-center w-8 h-8 rounded-lg border-2 border-dashed border-white/30">
            <svg class="w-4 h-4 text-white/50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
          </div>
          <span class="text-sm font-medium text-white/70">Create new organization</span>
        </button>
      </div>
    </Transition>
  </div>
</template>
