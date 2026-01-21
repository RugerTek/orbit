<script setup lang="ts">
import type { OrgChartResource, ResourceSubtype } from '~/types/operations'

const props = defineProps<{
  modelValue: boolean
  people: OrgChartResource[]
  subtypes: ResourceSubtype[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'created': []
}>()

const { createVacancy, isLoading } = useOperations()

// Form state
const form = reactive({
  vacantPositionTitle: '',
  reportsToResourceId: '',
  resourceSubtypeId: '',
  description: '',
})

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
})

// Reset form when dialog opens
watch(isOpen, (open) => {
  if (open) {
    form.vacantPositionTitle = ''
    form.reportsToResourceId = ''
    form.resourceSubtypeId = props.subtypes[0]?.id || ''
    form.description = ''
  }
})

// Filter out vacancies for manager selection
const availableManagers = computed(() =>
  props.people.filter(p => !p.isVacant)
)

const canSubmit = computed(() =>
  form.vacantPositionTitle.trim() && form.resourceSubtypeId
)

const handleSubmit = async () => {
  if (!canSubmit.value || isLoading.value) return

  try {
    await createVacancy({
      vacantPositionTitle: form.vacantPositionTitle.trim(),
      reportsToResourceId: form.reportsToResourceId || undefined,
      resourceSubtypeId: form.resourceSubtypeId,
      description: form.description.trim() || undefined,
    })
    emit('created')
    isOpen.value = false
  } catch (error) {
    console.error('Failed to create vacancy:', error)
  }
}
</script>

<template>
  <BaseDialog
    v-model="isOpen"
    size="lg"
    title="Add Vacant Position"
    subtitle="Create a placeholder for an unfilled position in your org chart."
    @submit="handleSubmit"
  >
    <div class="space-y-4">
      <!-- Position Title -->
      <div>
        <label for="vacantPositionTitle" class="block text-sm font-medium text-white/70 mb-1">
          Position Title <span class="text-red-400">*</span>
        </label>
        <input
          id="vacantPositionTitle"
          v-model="form.vacantPositionTitle"
          type="text"
          class="orbitos-input w-full"
          placeholder="e.g., Senior Software Engineer"
          autofocus
        />
      </div>

      <!-- Resource Subtype -->
      <div>
        <label for="resourceSubtypeId" class="block text-sm font-medium text-white/70 mb-1">
          Position Type <span class="text-red-400">*</span>
        </label>
        <select
          id="resourceSubtypeId"
          v-model="form.resourceSubtypeId"
          class="orbitos-input w-full"
        >
          <option value="" disabled>Select a position type</option>
          <option v-for="subtype in subtypes" :key="subtype.id" :value="subtype.id">
            {{ subtype.name }}
          </option>
        </select>
      </div>

      <!-- Reports To -->
      <div>
        <label for="reportsToResourceId" class="block text-sm font-medium text-white/70 mb-1">
          Reports To
        </label>
        <select
          id="reportsToResourceId"
          v-model="form.reportsToResourceId"
          class="orbitos-input w-full"
        >
          <option value="">No manager (top-level position)</option>
          <option v-for="person in availableManagers" :key="person.id" :value="person.id">
            {{ person.name }}
          </option>
        </select>
        <p class="text-xs text-white/40 mt-1">Who will this position report to?</p>
      </div>

      <!-- Description -->
      <div>
        <label for="description" class="block text-sm font-medium text-white/70 mb-1">
          Description
        </label>
        <textarea
          id="description"
          v-model="form.description"
          rows="3"
          class="orbitos-input w-full"
          placeholder="Brief description of the role responsibilities..."
        />
      </div>
    </div>

    <template #footer="{ close }">
      <div class="flex justify-end gap-3">
        <button
          type="button"
          class="orbitos-btn-secondary px-4 py-2"
          @click="close"
        >
          Cancel
        </button>
        <button
          type="button"
          class="orbitos-btn-primary px-4 py-2"
          :disabled="!canSubmit || isLoading"
          @click="handleSubmit"
        >
          <span v-if="isLoading" class="flex items-center gap-2">
            <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Creating...
          </span>
          <span v-else>Create Vacancy</span>
        </button>
      </div>
    </template>
  </BaseDialog>
</template>
