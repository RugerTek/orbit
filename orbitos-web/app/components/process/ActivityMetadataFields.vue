<script setup lang="ts">
import type { ActivityMetadata } from '~/types/activity-metadata'

const props = defineProps<{
  activityType: string
  modelValue: ActivityMetadata
}>()

const emit = defineEmits<{
  'update:modelValue': [value: ActivityMetadata]
}>()

// Local reactive copy of metadata
const metadata = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

// Update a single field in metadata
const updateField = (field: string, value: unknown) => {
  emit('update:modelValue', { ...props.modelValue, [field]: value })
}

// Check if this is an IE symbol type
const isIESymbol = computed(() => {
  return ['inspection', 'transport', 'delay', 'storage', 'document', 'database', 'manualInput', 'display'].includes(props.activityType)
})
</script>

<template>
  <div v-if="isIESymbol" class="space-y-3 mt-4 pt-4 border-t border-white/10">
    <h4 class="text-sm font-medium text-purple-300">IE Symbol Fields</h4>

    <!-- Inspection Fields -->
    <template v-if="activityType === 'inspection'">
      <div>
        <label class="orbitos-label">Inspection Criteria</label>
        <input
          type="text"
          :value="(metadata as any).inspectionCriteria || ''"
          class="orbitos-input"
          placeholder="e.g., Visual + dimensional check"
          @input="updateField('inspectionCriteria', ($event.target as HTMLInputElement).value)"
        />
      </div>
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="orbitos-label">Pass Rate (%)</label>
          <input
            type="number"
            :value="(metadata as any).passRate || ''"
            class="orbitos-input"
            placeholder="e.g., 98.5"
            min="0"
            max="100"
            step="0.1"
            @input="updateField('passRate', parseFloat(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
        <div>
          <label class="orbitos-label">Sampling Rate (%)</label>
          <input
            type="number"
            :value="(metadata as any).samplingRate || ''"
            class="orbitos-input"
            placeholder="100 = 100%"
            min="0"
            max="100"
            @input="updateField('samplingRate', parseFloat(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
      </div>
    </template>

    <!-- Transport Fields -->
    <template v-else-if="activityType === 'transport'">
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="orbitos-label">Distance</label>
          <input
            type="number"
            :value="(metadata as any).distance || ''"
            class="orbitos-input"
            placeholder="e.g., 50"
            @input="updateField('distance', parseFloat(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
        <div>
          <label class="orbitos-label">Unit</label>
          <select
            :value="(metadata as any).distanceUnit || 'meters'"
            class="orbitos-input"
            @change="updateField('distanceUnit', ($event.target as HTMLSelectElement).value)"
          >
            <option value="meters">Meters</option>
            <option value="feet">Feet</option>
            <option value="kilometers">Kilometers</option>
            <option value="miles">Miles</option>
          </select>
        </div>
      </div>
      <div>
        <label class="orbitos-label">Transport Mode</label>
        <select
          :value="(metadata as any).transportMode || 'manual'"
          class="orbitos-input"
          @change="updateField('transportMode', ($event.target as HTMLSelectElement).value)"
        >
          <option value="manual">Manual (walking)</option>
          <option value="forklift">Forklift</option>
          <option value="conveyor">Conveyor</option>
          <option value="cart">Cart/Trolley</option>
          <option value="truck">Truck</option>
          <option value="agv">AGV (Automated)</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="orbitos-label">Origin</label>
          <input
            type="text"
            :value="(metadata as any).origin || ''"
            class="orbitos-input"
            placeholder="e.g., Station A"
            @input="updateField('origin', ($event.target as HTMLInputElement).value)"
          />
        </div>
        <div>
          <label class="orbitos-label">Destination</label>
          <input
            type="text"
            :value="(metadata as any).destination || ''"
            class="orbitos-input"
            placeholder="e.g., QC Area"
            @input="updateField('destination', ($event.target as HTMLInputElement).value)"
          />
        </div>
      </div>
    </template>

    <!-- Delay Fields -->
    <template v-else-if="activityType === 'delay'">
      <div>
        <label class="orbitos-label">Delay Reason</label>
        <select
          :value="(metadata as any).delayReason || 'queue'"
          class="orbitos-input"
          @change="updateField('delayReason', ($event.target as HTMLSelectElement).value)"
        >
          <option value="queue">Queue/Waiting in line</option>
          <option value="batch">Batching</option>
          <option value="approval">Waiting for approval</option>
          <option value="curing">Curing time</option>
          <option value="drying">Drying time</option>
          <option value="cooling">Cooling time</option>
          <option value="scheduling">Scheduling delay</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="orbitos-label">Avg Wait (min)</label>
          <input
            type="number"
            :value="(metadata as any).averageWaitMinutes || ''"
            class="orbitos-input"
            placeholder="e.g., 15"
            @input="updateField('averageWaitMinutes', parseInt(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
        <div>
          <label class="orbitos-label">Max Wait (min)</label>
          <input
            type="number"
            :value="(metadata as any).maxWaitMinutes || ''"
            class="orbitos-input"
            placeholder="e.g., 45"
            @input="updateField('maxWaitMinutes', parseInt(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
      </div>
    </template>

    <!-- Storage Fields -->
    <template v-else-if="activityType === 'storage'">
      <div>
        <label class="orbitos-label">Storage Type</label>
        <select
          :value="(metadata as any).storageType || 'wip'"
          class="orbitos-input"
          @change="updateField('storageType', ($event.target as HTMLSelectElement).value)"
        >
          <option value="raw">Raw Materials</option>
          <option value="wip">Work in Progress (WIP)</option>
          <option value="finished">Finished Goods</option>
          <option value="tools">Tools & Equipment</option>
          <option value="consumables">Consumables</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="orbitos-label">Capacity</label>
          <input
            type="number"
            :value="(metadata as any).capacity || ''"
            class="orbitos-input"
            placeholder="e.g., 500"
            @input="updateField('capacity', parseInt(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
        <div>
          <label class="orbitos-label">Capacity Unit</label>
          <input
            type="text"
            :value="(metadata as any).capacityUnit || ''"
            class="orbitos-input"
            placeholder="e.g., units, pallets"
            @input="updateField('capacityUnit', ($event.target as HTMLInputElement).value)"
          />
        </div>
      </div>
      <div>
        <label class="orbitos-label">Location</label>
        <input
          type="text"
          :value="(metadata as any).location || ''"
          class="orbitos-input"
          placeholder="e.g., Rack B-12, Warehouse A"
          @input="updateField('location', ($event.target as HTMLInputElement).value)"
        />
      </div>
      <div>
        <label class="orbitos-label">Inventory Method</label>
        <select
          :value="(metadata as any).inventoryMethod || 'fifo'"
          class="orbitos-input"
          @change="updateField('inventoryMethod', ($event.target as HTMLSelectElement).value)"
        >
          <option value="fifo">FIFO (First In, First Out)</option>
          <option value="lifo">LIFO (Last In, First Out)</option>
          <option value="fefo">FEFO (First Expired, First Out)</option>
          <option value="other">Other</option>
        </select>
      </div>
    </template>

    <!-- Document Fields -->
    <template v-else-if="activityType === 'document'">
      <div>
        <label class="orbitos-label">Document Type</label>
        <select
          :value="(metadata as any).documentType || 'form'"
          class="orbitos-input"
          @change="updateField('documentType', ($event.target as HTMLSelectElement).value)"
        >
          <option value="form">Form</option>
          <option value="report">Report</option>
          <option value="checklist">Checklist</option>
          <option value="certificate">Certificate</option>
          <option value="drawing">Drawing/Diagram</option>
          <option value="specification">Specification</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div>
        <label class="orbitos-label">Document Code</label>
        <input
          type="text"
          :value="(metadata as any).documentCode || ''"
          class="orbitos-input"
          placeholder="e.g., QC-FORM-001"
          @input="updateField('documentCode', ($event.target as HTMLInputElement).value)"
        />
      </div>
      <div class="flex items-center gap-2">
        <input
          type="checkbox"
          :checked="(metadata as any).isControlled || false"
          class="rounded border-white/20 bg-white/5 text-purple-500"
          @change="updateField('isControlled', ($event.target as HTMLInputElement).checked)"
        />
        <label class="text-sm text-white/70">Controlled Document</label>
      </div>
    </template>

    <!-- Database Fields -->
    <template v-else-if="activityType === 'database'">
      <div>
        <label class="orbitos-label">System Name</label>
        <input
          type="text"
          :value="(metadata as any).systemName || ''"
          class="orbitos-input"
          placeholder="e.g., SAP, Oracle, PostgreSQL"
          @input="updateField('systemName', ($event.target as HTMLInputElement).value)"
        />
      </div>
      <div>
        <label class="orbitos-label">Operation Type</label>
        <select
          :value="(metadata as any).operation || 'read'"
          class="orbitos-input"
          @change="updateField('operation', ($event.target as HTMLSelectElement).value)"
        >
          <option value="read">Read Only</option>
          <option value="write">Write Only</option>
          <option value="both">Read & Write</option>
        </select>
      </div>
      <div>
        <label class="orbitos-label">Integration Type</label>
        <select
          :value="(metadata as any).integrationType || 'direct'"
          class="orbitos-input"
          @change="updateField('integrationType', ($event.target as HTMLSelectElement).value)"
        >
          <option value="direct">Direct Connection</option>
          <option value="api">API</option>
          <option value="file">File Transfer</option>
          <option value="message">Message Queue</option>
        </select>
      </div>
    </template>

    <!-- Manual Input Fields -->
    <template v-else-if="activityType === 'manualInput'">
      <div>
        <label class="orbitos-label">Input Device</label>
        <select
          :value="(metadata as any).inputDevice || 'keyboard'"
          class="orbitos-input"
          @change="updateField('inputDevice', ($event.target as HTMLSelectElement).value)"
        >
          <option value="keyboard">Keyboard</option>
          <option value="scanner">Barcode/QR Scanner</option>
          <option value="touchscreen">Touchscreen</option>
          <option value="voice">Voice Input</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="orbitos-label">Expected Time (sec)</label>
          <input
            type="number"
            :value="(metadata as any).expectedInputTime || ''"
            class="orbitos-input"
            placeholder="e.g., 30"
            @input="updateField('expectedInputTime', parseInt(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
        <div>
          <label class="orbitos-label">Error Rate (%)</label>
          <input
            type="number"
            :value="(metadata as any).errorRate || ''"
            class="orbitos-input"
            placeholder="e.g., 2"
            step="0.1"
            @input="updateField('errorRate', parseFloat(($event.target as HTMLInputElement).value) || undefined)"
          />
        </div>
      </div>
    </template>

    <!-- Display Fields -->
    <template v-else-if="activityType === 'display'">
      <div>
        <label class="orbitos-label">Display Type</label>
        <select
          :value="(metadata as any).displayType || 'status'"
          class="orbitos-input"
          @change="updateField('displayType', ($event.target as HTMLSelectElement).value)"
        >
          <option value="dashboard">Dashboard</option>
          <option value="report">Report View</option>
          <option value="alert">Alert/Notification</option>
          <option value="status">Status Display</option>
          <option value="instruction">Work Instructions</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div>
        <label class="orbitos-label">Display Device</label>
        <select
          :value="(metadata as any).displayDevice || 'monitor'"
          class="orbitos-input"
          @change="updateField('displayDevice', ($event.target as HTMLSelectElement).value)"
        >
          <option value="monitor">Monitor/Screen</option>
          <option value="andon">Andon Board</option>
          <option value="mobile">Mobile Device</option>
          <option value="projector">Projector</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div>
        <label class="orbitos-label">Refresh Rate (sec)</label>
        <input
          type="number"
          :value="(metadata as any).refreshRateSeconds || ''"
          class="orbitos-input"
          placeholder="e.g., 5 (leave empty for static)"
          @input="updateField('refreshRateSeconds', parseInt(($event.target as HTMLInputElement).value) || undefined)"
        />
      </div>
      <div>
        <label class="orbitos-label">Display Location</label>
        <input
          type="text"
          :value="(metadata as any).displayLocation || ''"
          class="orbitos-input"
          placeholder="e.g., Production Floor, Line 3"
          @input="updateField('displayLocation', ($event.target as HTMLInputElement).value)"
        />
      </div>
    </template>
  </div>
</template>
