<script setup lang="ts">
/**
 * HelpProvider - Global Help System Initializer
 *
 * Add this component once in your app layout to enable:
 * - Cmd+K spotlight search
 * - Help panel
 * - Walkthroughs
 * - Keyboard shortcuts
 */

const { setupKeyboardShortcuts, loadHelpIndex } = useHelp()

// Initialize help system
setupKeyboardShortcuts()

onMounted(() => {
  loadHelpIndex()
})
</script>

<template>
  <!-- Help Spotlight (Cmd+K) -->
  <HelpSpotlight />

  <!-- Slide-out Help Panel -->
  <HelpHelpPanel />

  <!-- Walkthrough Overlay -->
  <HelpWalkthroughOverlay />

  <!-- Pass through slot content -->
  <slot />
</template>
