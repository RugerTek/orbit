<script setup lang="ts">
import type {
  Partner,
  PartnerType,
  PartnerStatus,
  StrategicValue,
  Channel,
  ChannelType,
  ChannelCategory,
  ChannelStatus,
  ChannelOwnership,
  ValueProposition,
  ValuePropositionStatus,
  CustomerRelationship,
  CustomerRelationshipType,
  CustomerRelationshipStatus,
  RevenueStream,
  RevenueStreamType,
  RevenueStreamStatus,
  PricingMechanism,
} from '~/types/operations'

type EntityType = 'partner' | 'channel' | 'valueProposition' | 'customerRelationship' | 'revenueStream'
type Entity = Partner | Channel | ValueProposition | CustomerRelationship | RevenueStream | null

const props = defineProps<{
  entity: Entity
  entityType: EntityType
}>()

const emit = defineEmits<{
  close: []
  save: [entity: Entity]
}>()

const {
  createPartner,
  updatePartner,
  deletePartner,
  createChannel,
  updateChannel,
  deleteChannel,
  createValueProposition,
  updateValueProposition,
  deleteValueProposition,
  createCustomerRelationship,
  updateCustomerRelationship,
  deleteCustomerRelationship,
  createRevenueStream,
  updateRevenueStream,
  deleteRevenueStream,
} = useOperations()

const isEditing = computed(() => props.entity !== null)
const isSaving = ref(false)
const isDeleting = ref(false)
const activeTab = ref<'basic' | 'details' | 'advanced'>('basic')

// Form state based on entity type
const partnerForm = ref({
  name: '',
  description: '',
  type: 'Strategic' as PartnerType,
  status: 'Active' as PartnerStatus,
  strategicValue: 'Medium' as StrategicValue,
  website: '',
  relationshipStrength: 3,
})

const channelForm = ref({
  name: '',
  description: '',
  type: 'Direct' as ChannelType,
  category: 'Sales' as ChannelCategory,
  status: 'Active' as ChannelStatus,
  ownership: 'Owned' as ChannelOwnership,
})

const valuePropositionForm = ref({
  name: '',
  headline: '',
  description: '',
  status: 'Draft' as ValuePropositionStatus,
  customerJobsJson: '',
  painsJson: '',
  gainsJson: '',
  painRelieversJson: '',
  gainCreatorsJson: '',
})

const customerRelationshipForm = ref({
  name: '',
  description: '',
  type: 'PersonalAssistance' as CustomerRelationshipType,
  status: 'Active' as CustomerRelationshipStatus,
})

const revenueStreamForm = ref({
  name: '',
  description: '',
  type: 'Subscription' as RevenueStreamType,
  status: 'Active' as RevenueStreamStatus,
  pricingMechanism: 'Fixed' as PricingMechanism,
})

// Initialize form from entity
watchEffect(() => {
  if (props.entity && props.entityType === 'partner') {
    const p = props.entity as Partner
    partnerForm.value = {
      name: p.name || '',
      description: p.description || '',
      type: p.type || 'Strategic',
      status: p.status || 'Active',
      strategicValue: p.strategicValue || 'Medium',
      website: p.website || '',
      relationshipStrength: p.relationshipStrength || 3,
    }
  } else if (props.entity && props.entityType === 'channel') {
    const c = props.entity as Channel
    channelForm.value = {
      name: c.name || '',
      description: c.description || '',
      type: c.type || 'Direct',
      category: c.category || 'Sales',
      status: c.status || 'Active',
      ownership: c.ownership || 'Owned',
    }
  } else if (props.entity && props.entityType === 'valueProposition') {
    const v = props.entity as ValueProposition
    valuePropositionForm.value = {
      name: v.name || '',
      headline: v.headline || '',
      description: v.description || '',
      status: v.status || 'Draft',
      customerJobsJson: v.customerJobsJson || '',
      painsJson: v.painsJson || '',
      gainsJson: v.gainsJson || '',
      painRelieversJson: v.painRelieversJson || '',
      gainCreatorsJson: v.gainCreatorsJson || '',
    }
  } else if (props.entity && props.entityType === 'customerRelationship') {
    const cr = props.entity as CustomerRelationship
    customerRelationshipForm.value = {
      name: cr.name || '',
      description: cr.description || '',
      type: cr.type || 'PersonalAssistance',
      status: cr.status || 'Active',
    }
  } else if (props.entity && props.entityType === 'revenueStream') {
    const r = props.entity as RevenueStream
    revenueStreamForm.value = {
      name: r.name || '',
      description: r.description || '',
      type: r.type || 'Subscription',
      status: r.status || 'Active',
      pricingMechanism: r.pricingMechanism || 'Fixed',
    }
  }
})

// Option lists
const partnerTypes: PartnerType[] = ['Supplier', 'Distributor', 'Strategic', 'Technology', 'Agency', 'Reseller', 'Affiliate', 'JointVenture']
const partnerStatuses: PartnerStatus[] = ['Prospective', 'Active', 'OnHold', 'Terminated']
const strategicValues: StrategicValue[] = ['Critical', 'High', 'Medium', 'Low']

const channelTypes: ChannelType[] = ['Direct', 'Indirect', 'Digital', 'Physical', 'Hybrid']
const channelCategories: ChannelCategory[] = ['Sales', 'Marketing', 'Distribution', 'Support', 'Communication']
const channelStatuses: ChannelStatus[] = ['Planned', 'Active', 'Optimizing', 'Sunset', 'Inactive']
const channelOwnerships: ChannelOwnership[] = ['Owned', 'Partner', 'ThirdParty']

const valuePropositionStatuses: ValuePropositionStatus[] = ['Draft', 'Validated', 'Active', 'Testing', 'Retired']

const customerRelationshipTypes: CustomerRelationshipType[] = ['PersonalAssistance', 'DedicatedAssistance', 'SelfService', 'AutomatedService', 'Communities', 'CoCreation']
const customerRelationshipStatuses: CustomerRelationshipStatus[] = ['Planned', 'Active', 'Optimizing', 'Sunset']

const revenueStreamTypes: RevenueStreamType[] = ['AssetSale', 'UsageFee', 'Subscription', 'Licensing', 'Brokerage', 'Advertising', 'Leasing', 'Commission']
const revenueStreamStatuses: RevenueStreamStatus[] = ['Planned', 'Active', 'Growing', 'Mature', 'Declining', 'Sunset']
const pricingMechanisms: PricingMechanism[] = ['Fixed', 'Dynamic', 'Negotiated', 'Auction', 'MarketDependent', 'VolumeDependent']

// Title based on entity type
const modalTitle = computed(() => {
  const prefix = isEditing.value ? 'Edit' : 'Add'
  switch (props.entityType) {
    case 'partner': return `${prefix} Partner`
    case 'channel': return `${prefix} Channel`
    case 'valueProposition': return `${prefix} Value Proposition`
    case 'customerRelationship': return `${prefix} Customer Relationship`
    case 'revenueStream': return `${prefix} Revenue Stream`
    default: return `${prefix} Item`
  }
})

// Current form name for validation
const currentFormName = computed(() => {
  switch (props.entityType) {
    case 'partner': return partnerForm.value.name
    case 'channel': return channelForm.value.name
    case 'valueProposition': return valuePropositionForm.value.name
    case 'customerRelationship': return customerRelationshipForm.value.name
    case 'revenueStream': return revenueStreamForm.value.name
    default: return ''
  }
})

// Save handler
const handleSave = async () => {
  if (!currentFormName.value.trim()) return

  isSaving.value = true
  try {
    let result: Entity = null

    if (props.entityType === 'partner') {
      if (isEditing.value && props.entity) {
        result = await updatePartner(props.entity.id, partnerForm.value)
      } else {
        result = await createPartner(partnerForm.value)
      }
    } else if (props.entityType === 'channel') {
      if (isEditing.value && props.entity) {
        result = await updateChannel(props.entity.id, channelForm.value)
      } else {
        result = await createChannel(channelForm.value)
      }
    } else if (props.entityType === 'valueProposition') {
      if (isEditing.value && props.entity) {
        result = await updateValueProposition(props.entity.id, valuePropositionForm.value)
      } else {
        result = await createValueProposition(valuePropositionForm.value)
      }
    } else if (props.entityType === 'customerRelationship') {
      if (isEditing.value && props.entity) {
        result = await updateCustomerRelationship(props.entity.id, customerRelationshipForm.value)
      } else {
        result = await createCustomerRelationship(customerRelationshipForm.value)
      }
    } else if (props.entityType === 'revenueStream') {
      if (isEditing.value && props.entity) {
        result = await updateRevenueStream(props.entity.id, revenueStreamForm.value)
      } else {
        result = await createRevenueStream(revenueStreamForm.value)
      }
    }

    emit('save', result)
  } catch (e) {
    console.error('Failed to save:', e)
  } finally {
    isSaving.value = false
  }
}

// Delete handler
const handleDelete = async () => {
  if (!props.entity || !isEditing.value) return
  if (!confirm('Are you sure you want to delete this item? This action cannot be undone.')) return

  isDeleting.value = true
  try {
    if (props.entityType === 'partner') {
      await deletePartner(props.entity.id)
    } else if (props.entityType === 'channel') {
      await deleteChannel(props.entity.id)
    } else if (props.entityType === 'valueProposition') {
      await deleteValueProposition(props.entity.id)
    } else if (props.entityType === 'customerRelationship') {
      await deleteCustomerRelationship(props.entity.id)
    } else if (props.entityType === 'revenueStream') {
      await deleteRevenueStream(props.entity.id)
    }
    emit('close')
  } catch (e) {
    console.error('Failed to delete:', e)
  } finally {
    isDeleting.value = false
  }
}

// Format label for display
const formatLabel = (str: string) => {
  return str.replace(/([A-Z])/g, ' $1').trim()
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center">
    <!-- Backdrop -->
    <div class="absolute inset-0 bg-black/70 backdrop-blur-sm" @click="$emit('close')"></div>

    <!-- Modal -->
    <div class="relative z-10 w-full max-w-2xl max-h-[90vh] overflow-hidden rounded-2xl bg-slate-900 border border-white/10 shadow-2xl flex flex-col">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-white/10 px-6 py-4">
        <h2 class="text-lg font-semibold text-white">{{ modalTitle }}</h2>
        <button
          class="rounded-lg p-2 text-white/40 hover:bg-white/10 hover:text-white transition-colors"
          @click="$emit('close')"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Tabs -->
      <div class="flex border-b border-white/10 px-6">
        <button
          v-for="tab in [{ id: 'basic', label: 'Basic Info' }, { id: 'details', label: 'Details' }] as const"
          :key="tab.id"
          :class="[
            'px-4 py-3 text-sm font-medium border-b-2 -mb-px transition-colors',
            activeTab === tab.id
              ? 'border-purple-500 text-purple-300'
              : 'border-transparent text-white/50 hover:text-white/80'
          ]"
          @click="activeTab = tab.id"
        >
          {{ tab.label }}
        </button>
      </div>

      <!-- Form Content -->
      <div class="flex-1 overflow-y-auto p-6">
        <!-- Partner Form -->
        <template v-if="entityType === 'partner'">
          <div v-show="activeTab === 'basic'" class="space-y-4">
            <div>
              <label class="orbitos-label">Name *</label>
              <input
                v-model="partnerForm.name"
                type="text"
                class="orbitos-input"
                placeholder="Partner name"
                required
              />
            </div>

            <div>
              <label class="orbitos-label">Description</label>
              <textarea
                v-model="partnerForm.description"
                rows="3"
                class="orbitos-input"
                placeholder="Brief description of the partnership"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="orbitos-label">Type</label>
                <select v-model="partnerForm.type" class="orbitos-input">
                  <option v-for="t in partnerTypes" :key="t" :value="t">{{ formatLabel(t) }}</option>
                </select>
              </div>

              <div>
                <label class="orbitos-label">Status</label>
                <select v-model="partnerForm.status" class="orbitos-input">
                  <option v-for="s in partnerStatuses" :key="s" :value="s">{{ formatLabel(s) }}</option>
                </select>
              </div>
            </div>
          </div>

          <div v-show="activeTab === 'details'" class="space-y-4">
            <div>
              <label class="orbitos-label">Strategic Value</label>
              <select v-model="partnerForm.strategicValue" class="orbitos-input">
                <option v-for="v in strategicValues" :key="v" :value="v">{{ v }}</option>
              </select>
            </div>

            <div>
              <label class="orbitos-label">Website</label>
              <input
                v-model="partnerForm.website"
                type="url"
                class="orbitos-input"
                placeholder="https://example.com"
              />
            </div>

            <div>
              <label class="orbitos-label">Relationship Strength (1-5)</label>
              <div class="flex items-center gap-4">
                <input
                  v-model.number="partnerForm.relationshipStrength"
                  type="range"
                  min="1"
                  max="5"
                  class="flex-1 accent-purple-500"
                />
                <span class="text-white font-medium w-8 text-center">{{ partnerForm.relationshipStrength }}</span>
              </div>
            </div>
          </div>
        </template>

        <!-- Channel Form -->
        <template v-else-if="entityType === 'channel'">
          <div v-show="activeTab === 'basic'" class="space-y-4">
            <div>
              <label class="orbitos-label">Name *</label>
              <input
                v-model="channelForm.name"
                type="text"
                class="orbitos-input"
                placeholder="Channel name"
                required
              />
            </div>

            <div>
              <label class="orbitos-label">Description</label>
              <textarea
                v-model="channelForm.description"
                rows="3"
                class="orbitos-input"
                placeholder="How this channel works"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="orbitos-label">Type</label>
                <select v-model="channelForm.type" class="orbitos-input">
                  <option v-for="t in channelTypes" :key="t" :value="t">{{ t }}</option>
                </select>
              </div>

              <div>
                <label class="orbitos-label">Category</label>
                <select v-model="channelForm.category" class="orbitos-input">
                  <option v-for="c in channelCategories" :key="c" :value="c">{{ c }}</option>
                </select>
              </div>
            </div>
          </div>

          <div v-show="activeTab === 'details'" class="space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="orbitos-label">Status</label>
                <select v-model="channelForm.status" class="orbitos-input">
                  <option v-for="s in channelStatuses" :key="s" :value="s">{{ s }}</option>
                </select>
              </div>

              <div>
                <label class="orbitos-label">Ownership</label>
                <select v-model="channelForm.ownership" class="orbitos-input">
                  <option v-for="o in channelOwnerships" :key="o" :value="o">{{ formatLabel(o) }}</option>
                </select>
              </div>
            </div>
          </div>
        </template>

        <!-- Value Proposition Form -->
        <template v-else-if="entityType === 'valueProposition'">
          <div v-show="activeTab === 'basic'" class="space-y-4">
            <div>
              <label class="orbitos-label">Name *</label>
              <input
                v-model="valuePropositionForm.name"
                type="text"
                class="orbitos-input"
                placeholder="Value proposition name"
                required
              />
            </div>

            <div>
              <label class="orbitos-label">Headline *</label>
              <input
                v-model="valuePropositionForm.headline"
                type="text"
                class="orbitos-input"
                placeholder="One-line value statement"
                required
              />
            </div>

            <div>
              <label class="orbitos-label">Description</label>
              <textarea
                v-model="valuePropositionForm.description"
                rows="3"
                class="orbitos-input"
                placeholder="Detailed description"
              ></textarea>
            </div>

            <div>
              <label class="orbitos-label">Status</label>
              <select v-model="valuePropositionForm.status" class="orbitos-input">
                <option v-for="s in valuePropositionStatuses" :key="s" :value="s">{{ s }}</option>
              </select>
            </div>
          </div>

          <div v-show="activeTab === 'details'" class="space-y-4">
            <div>
              <label class="orbitos-label">Customer Jobs (one per line)</label>
              <textarea
                v-model="valuePropositionForm.customerJobsJson"
                rows="3"
                class="orbitos-input"
                placeholder="What jobs do customers need to get done?"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="orbitos-label">Customer Pains</label>
                <textarea
                  v-model="valuePropositionForm.painsJson"
                  rows="3"
                  class="orbitos-input"
                  placeholder="What pains do they experience?"
                ></textarea>
              </div>

              <div>
                <label class="orbitos-label">Customer Gains</label>
                <textarea
                  v-model="valuePropositionForm.gainsJson"
                  rows="3"
                  class="orbitos-input"
                  placeholder="What gains do they seek?"
                ></textarea>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="orbitos-label">Pain Relievers</label>
                <textarea
                  v-model="valuePropositionForm.painRelieversJson"
                  rows="3"
                  class="orbitos-input"
                  placeholder="How do you relieve pains?"
                ></textarea>
              </div>

              <div>
                <label class="orbitos-label">Gain Creators</label>
                <textarea
                  v-model="valuePropositionForm.gainCreatorsJson"
                  rows="3"
                  class="orbitos-input"
                  placeholder="How do you create gains?"
                ></textarea>
              </div>
            </div>
          </div>
        </template>

        <!-- Customer Relationship Form -->
        <template v-else-if="entityType === 'customerRelationship'">
          <div v-show="activeTab === 'basic'" class="space-y-4">
            <div>
              <label class="orbitos-label">Name *</label>
              <input
                v-model="customerRelationshipForm.name"
                type="text"
                class="orbitos-input"
                placeholder="Relationship type name"
                required
              />
            </div>

            <div>
              <label class="orbitos-label">Description</label>
              <textarea
                v-model="customerRelationshipForm.description"
                rows="3"
                class="orbitos-input"
                placeholder="How this relationship type works"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="orbitos-label">Type</label>
                <select v-model="customerRelationshipForm.type" class="orbitos-input">
                  <option v-for="t in customerRelationshipTypes" :key="t" :value="t">{{ formatLabel(t) }}</option>
                </select>
              </div>

              <div>
                <label class="orbitos-label">Status</label>
                <select v-model="customerRelationshipForm.status" class="orbitos-input">
                  <option v-for="s in customerRelationshipStatuses" :key="s" :value="s">{{ s }}</option>
                </select>
              </div>
            </div>
          </div>

          <div v-show="activeTab === 'details'" class="space-y-4">
            <div class="text-white/40 text-sm text-center py-8">
              Additional relationship details coming soon
            </div>
          </div>
        </template>

        <!-- Revenue Stream Form -->
        <template v-else-if="entityType === 'revenueStream'">
          <div v-show="activeTab === 'basic'" class="space-y-4">
            <div>
              <label class="orbitos-label">Name *</label>
              <input
                v-model="revenueStreamForm.name"
                type="text"
                class="orbitos-input"
                placeholder="Revenue stream name"
                required
              />
            </div>

            <div>
              <label class="orbitos-label">Description</label>
              <textarea
                v-model="revenueStreamForm.description"
                rows="3"
                class="orbitos-input"
                placeholder="How this revenue stream works"
              ></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="orbitos-label">Type</label>
                <select v-model="revenueStreamForm.type" class="orbitos-input">
                  <option v-for="t in revenueStreamTypes" :key="t" :value="t">{{ formatLabel(t) }}</option>
                </select>
              </div>

              <div>
                <label class="orbitos-label">Status</label>
                <select v-model="revenueStreamForm.status" class="orbitos-input">
                  <option v-for="s in revenueStreamStatuses" :key="s" :value="s">{{ s }}</option>
                </select>
              </div>
            </div>
          </div>

          <div v-show="activeTab === 'details'" class="space-y-4">
            <div>
              <label class="orbitos-label">Pricing Mechanism</label>
              <select v-model="revenueStreamForm.pricingMechanism" class="orbitos-input">
                <option v-for="p in pricingMechanisms" :key="p" :value="p">{{ formatLabel(p) }}</option>
              </select>
            </div>
          </div>
        </template>
      </div>

      <!-- Footer Actions -->
      <div class="flex items-center justify-between border-t border-white/10 px-6 py-4 bg-black/20">
        <div>
          <button
            v-if="isEditing"
            type="button"
            class="rounded-lg px-4 py-2 text-sm text-red-400 hover:bg-red-500/10 transition-colors"
            :disabled="isDeleting"
            @click="handleDelete"
          >
            {{ isDeleting ? 'Deleting...' : 'Delete' }}
          </button>
        </div>
        <div class="flex gap-3">
          <button
            type="button"
            class="rounded-lg px-4 py-2 text-sm text-white/70 hover:text-white transition-colors"
            @click="$emit('close')"
          >
            Cancel
          </button>
          <button
            type="button"
            class="orbitos-btn-primary px-6 py-2 text-sm"
            :disabled="isSaving || !currentFormName.trim()"
            @click="handleSave"
          >
            {{ isSaving ? 'Saving...' : (isEditing ? 'Save Changes' : 'Create') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Custom range slider */
input[type="range"] {
  -webkit-appearance: none;
  width: 100%;
  height: 8px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.1);
}

input[type="range"]::-webkit-slider-thumb {
  -webkit-appearance: none;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: linear-gradient(to right, rgb(147 51 234), rgb(37 99 235));
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
}

input[type="range"]::-moz-range-thumb {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: linear-gradient(to right, rgb(147 51 234), rgb(37 99 235));
  cursor: pointer;
  border: none;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
}
</style>
