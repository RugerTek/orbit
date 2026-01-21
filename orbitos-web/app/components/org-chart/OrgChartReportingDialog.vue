<script setup lang="ts">
import type { OrgChartResource } from '~/types/operations'

const props = defineProps<{
  modelValue: boolean
  person: OrgChartResource | null
  people: OrgChartResource[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'updated': []
}>()

const { updateReporting, fillVacancy, isLoading } = useOperations()

// Form state
const selectedManagerId = ref<string>('')
const fillForm = reactive({
  name: '',
  description: '',
})

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
})

// Reset form when dialog opens or person changes
watch([isOpen, () => props.person], ([open, person]) => {
  if (open && person) {
    selectedManagerId.value = person.reportsToResourceId || ''
    fillForm.name = ''
    fillForm.description = person.description || ''
  }
})

// Get valid managers (exclude self and descendants to prevent circular refs)
const getDescendantIds = (personId: string): Set<string> => {
  const descendants = new Set<string>()
  const queue = [personId]

  while (queue.length > 0) {
    const currentId = queue.shift()!
    const person = props.people.find(p => p.id === currentId)
    if (person?.directReports) {
      for (const child of person.directReports) {
        descendants.add(child.id)
        queue.push(child.id)
      }
    }
    // Also check flat list for children
    for (const p of props.people) {
      if (p.reportsToResourceId === currentId && !descendants.has(p.id)) {
        descendants.add(p.id)
        queue.push(p.id)
      }
    }
  }

  return descendants
}

const availableManagers = computed(() => {
  if (!props.person) return []
  const excludeIds = new Set([props.person.id, ...getDescendantIds(props.person.id)])
  return props.people.filter(p => !excludeIds.has(p.id) && !p.isVacant)
})

const hasChanges = computed(() => {
  if (!props.person) return false
  return selectedManagerId.value !== (props.person.reportsToResourceId || '')
})

const canFill = computed(() =>
  props.person?.isVacant && fillForm.name.trim()
)

const handleUpdateReporting = async () => {
  if (!props.person || !hasChanges.value || isLoading.value) return

  try {
    await updateReporting(
      props.person.id,
      selectedManagerId.value || null
    )
    emit('updated')
    isOpen.value = false
  } catch (error) {
    console.error('Failed to update reporting:', error)
  }
}

const handleFillVacancy = async () => {
  if (!props.person || !canFill.value || isLoading.value) return

  try {
    await fillVacancy(props.person.id, {
      name: fillForm.name.trim(),
      description: fillForm.description.trim() || undefined,
    })
    emit('updated')
    isOpen.value = false
  } catch (error) {
    console.error('Failed to fill vacancy:', error)
  }
}
</script>

<template>
  <BaseDialog
    v-model="isOpen"
    size="lg"
    :title="person?.isVacant ? 'Edit Vacant Position' : 'Edit Reporting'"
    :subtitle="person?.isVacant
      ? `Fill or update the ${person.vacantPositionTitle} position`
      : `Update reporting relationship for ${person?.name}`"
  >
    <div v-if="person" class="space-y-6">
      <!-- Current person info -->
      <div class="flex items-center gap-3 p-3 rounded-lg bg-white/5">
        <div
          :class="[
            'w-12 h-12 rounded-full flex items-center justify-center text-lg font-medium',
            person.isVacant ? 'bg-amber-500/20 text-amber-300' : 'bg-purple-500/20 text-purple-300'
          ]"
        >
          {{ person.isVacant ? '?' : person.name.charAt(0).toUpperCase() }}
        </div>
        <div>
          <div class="font-medium text-white">
            {{ person.isVacant ? person.vacantPositionTitle : person.name }}
          </div>
          <div class="text-sm text-white/50">
            {{ person.description || person.resourceSubtypeName }}
          </div>
          <div v-if="person.directReportsCount > 0" class="text-xs text-white/40 mt-1">
            {{ person.directReportsCount }} direct report{{ person.directReportsCount !== 1 ? 's' : '' }}
          </div>
        </div>
      </div>

      <!-- Fill vacancy section (only for vacancies) -->
      <div v-if="person.isVacant" class="space-y-4 p-4 rounded-lg border border-amber-500/30 bg-amber-500/5">
        <h3 class="text-sm font-medium text-amber-300">Fill This Position</h3>

        <div>
          <label for="fillName" class="block text-sm font-medium text-white/70 mb-1">
            Person's Name <span class="text-red-400">*</span>
          </label>
          <input
            id="fillName"
            v-model="fillForm.name"
            type="text"
            class="orbitos-input w-full"
            placeholder="Enter the person's full name"
          />
        </div>

        <div>
          <label for="fillDescription" class="block text-sm font-medium text-white/70 mb-1">
            Title / Description
          </label>
          <input
            id="fillDescription"
            v-model="fillForm.description"
            type="text"
            class="orbitos-input w-full"
            placeholder="e.g., Senior Software Engineer"
          />
        </div>

        <button
          type="button"
          class="w-full orbitos-btn-primary py-2"
          :disabled="!canFill || isLoading"
          @click="handleFillVacancy"
        >
          <span v-if="isLoading">Filling...</span>
          <span v-else>Fill Position</span>
        </button>
      </div>

      <!-- Reporting relationship -->
      <div class="space-y-4">
        <h3 class="text-sm font-medium text-white/70">Reporting Relationship</h3>

        <div>
          <label for="managerId" class="block text-sm font-medium text-white/70 mb-1">
            Reports To
          </label>
          <select
            id="managerId"
            v-model="selectedManagerId"
            class="orbitos-input w-full"
          >
            <option value="">No manager (top-level)</option>
            <option v-for="manager in availableManagers" :key="manager.id" :value="manager.id">
              {{ manager.name }}
              <template v-if="manager.description"> - {{ manager.description }}</template>
            </option>
          </select>
          <p class="text-xs text-white/40 mt-1">
            Changing this will update the org chart hierarchy.
          </p>
        </div>

        <!-- Warning about moving someone with reports -->
        <div v-if="person.directReportsCount > 0 && hasChanges" class="p-3 rounded-lg bg-amber-500/10 border border-amber-500/30">
          <p class="text-sm text-amber-300">
            <strong>Note:</strong> {{ person.name || person.vacantPositionTitle }}'s direct reports will stay with them.
          </p>
        </div>
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
          v-if="hasChanges"
          type="button"
          class="orbitos-btn-primary px-4 py-2"
          :disabled="isLoading"
          @click="handleUpdateReporting"
        >
          <span v-if="isLoading">Saving...</span>
          <span v-else>Save Changes</span>
        </button>
      </div>
    </template>
  </BaseDialog>
</template>
