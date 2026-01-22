<script setup lang="ts">
interface Props {
  avatarUrl?: string
  displayName?: string
  firstName?: string
  lastName?: string
  size?: 'sm' | 'md' | 'lg' | 'xl'
}

const props = withDefaults(defineProps<Props>(), {
  size: 'lg'
})

const sizeClasses: Record<string, string> = {
  sm: 'w-8 h-8 text-sm',
  md: 'w-12 h-12 text-lg',
  lg: 'w-20 h-20 text-2xl',
  xl: 'w-32 h-32 text-4xl'
}

const initials = computed(() => {
  if (props.firstName && props.lastName) {
    return `${props.firstName[0]}${props.lastName[0]}`.toUpperCase()
  }
  if (props.displayName) {
    return props.displayName.charAt(0).toUpperCase()
  }
  return '?'
})

const imageError = ref(false)

const showImage = computed(() => {
  return props.avatarUrl && !imageError.value
})

const handleImageError = () => {
  imageError.value = true
}

// Reset error when avatarUrl changes
watch(() => props.avatarUrl, () => {
  imageError.value = false
})
</script>

<template>
  <div
    :class="[
      'rounded-full flex items-center justify-center font-medium overflow-hidden',
      sizeClasses[size]
    ]"
  >
    <img
      v-if="showImage"
      :src="avatarUrl"
      :alt="`${displayName || 'User'}'s avatar`"
      class="w-full h-full object-cover"
      @error="handleImageError"
    />
    <div
      v-else
      class="w-full h-full flex items-center justify-center bg-gradient-to-br from-purple-500 to-blue-600 text-white shadow-lg shadow-purple-500/30"
    >
      {{ initials }}
    </div>
  </div>
</template>
