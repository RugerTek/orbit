<script setup lang="ts">
interface Props {
  /** Category ID in the knowledge base (e.g., 'roles', 'goals', 'org-design') */
  category?: string
  /** Specific article ID to link to (e.g., 'goals/smart-goals') */
  articleId?: string
  /** Button label - defaults to 'Best Practices' */
  label?: string
  /** Additional CSS classes */
  class?: string
}

const props = withDefaults(defineProps<Props>(), {
  label: 'Best Practices'
})

const linkTo = computed(() => {
  if (props.articleId) {
    return `/app/knowledge-base/${props.articleId}`
  }
  // For now, link to main knowledge base - could filter by category in the future
  return '/app/knowledge-base'
})
</script>

<template>
  <NuxtLink
    :to="linkTo"
    class="orbitos-btn-secondary py-2 px-4 text-sm inline-flex items-center gap-2"
    :class="props.class"
  >
    <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
    </svg>
    {{ label }}
  </NuxtLink>
</template>
