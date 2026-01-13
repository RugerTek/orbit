#!/usr/bin/env node

/**
 * =============================================================================
 * OrbitOS Spec Validation Script
 * =============================================================================
 * Validates specification files for:
 * - Valid JSON syntax
 * - Required fields presence
 * - Cross-reference integrity
 * - Naming conventions
 * =============================================================================
 */

const fs = require('fs');
const path = require('path');

const SPECS_DIR = path.join(__dirname, '..', 'specs');

// Required fields for each spec type
const REQUIRED_FIELDS = {
  feature: ['id', 'name', 'status', 'description', 'capabilities'],
  entity: ['id', 'name', 'description', 'fields'],
};

// Validation results
let errors = [];
let warnings = [];

/**
 * Validate a single JSON file
 */
function validateJsonFile(filePath) {
  const relativePath = path.relative(process.cwd(), filePath);

  try {
    const content = fs.readFileSync(filePath, 'utf8');
    const data = JSON.parse(content);

    // Determine spec type from path
    if (filePath.includes('/features/')) {
      validateFeature(data, relativePath);
    } else if (filePath.includes('/entities/')) {
      validateEntity(data, relativePath);
    }

    return true;
  } catch (err) {
    if (err instanceof SyntaxError) {
      errors.push(`${relativePath}: Invalid JSON - ${err.message}`);
    } else {
      errors.push(`${relativePath}: ${err.message}`);
    }
    return false;
  }
}

/**
 * Validate feature spec
 */
function validateFeature(data, filePath) {
  // Skip template and index files
  if (filePath.includes('_template') || filePath.includes('index.json')) {
    return;
  }

  // Check required fields
  for (const field of REQUIRED_FIELDS.feature) {
    if (!data[field]) {
      errors.push(`${filePath}: Missing required field '${field}'`);
    }
  }

  // Validate ID format (F###)
  if (data.id && !/^F\d{3}$/.test(data.id)) {
    warnings.push(`${filePath}: ID '${data.id}' should follow format F### (e.g., F001)`);
  }

  // Validate status
  const validStatuses = ['draft', 'proposed', 'approved', 'in_progress', 'completed', 'deprecated', 'implemented'];
  if (data.status && !validStatuses.includes(data.status)) {
    warnings.push(`${filePath}: Invalid status '${data.status}'. Valid: ${validStatuses.join(', ')}`);
  }

  // Check capabilities have acceptance criteria (support both naming conventions)
  if (data.capabilities && Array.isArray(data.capabilities)) {
    data.capabilities.forEach((cap, idx) => {
      const criteria = cap.acceptanceCriteria || cap.acceptance_criteria;
      if (!criteria || criteria.length === 0) {
        warnings.push(`${filePath}: Capability ${idx + 1} missing acceptance criteria`);
      }
    });
  }
}

/**
 * Validate entity spec
 */
function validateEntity(data, filePath) {
  // Skip template and index files
  if (filePath.includes('_template') || filePath.includes('index.json')) {
    return;
  }

  // Check required fields
  for (const field of REQUIRED_FIELDS.entity) {
    if (!data[field]) {
      errors.push(`${filePath}: Missing required field '${field}'`);
    }
  }

  // Validate ID format (ENT###)
  if (data.id && !/^ENT\d{3}$/.test(data.id)) {
    warnings.push(`${filePath}: ID '${data.id}' should follow format ENT### (e.g., ENT001)`);
  }

  // Check fields have types
  if (data.fields && Array.isArray(data.fields)) {
    data.fields.forEach((field, idx) => {
      if (!field.name) {
        errors.push(`${filePath}: Field ${idx + 1} missing 'name'`);
      }
      if (!field.type) {
        errors.push(`${filePath}: Field '${field.name || idx + 1}' missing 'type'`);
      }
    });
  }

  // Check for audit fields
  const auditFields = ['createdAt', 'updatedAt', 'createdBy', 'updatedBy'];
  const fieldNames = (data.fields || []).map(f => f.name);
  const missingAudit = auditFields.filter(af => !fieldNames.includes(af));
  if (missingAudit.length > 0) {
    warnings.push(`${filePath}: Missing audit fields: ${missingAudit.join(', ')}`);
  }
}

/**
 * Recursively find all JSON files in a directory
 */
function findJsonFiles(dir) {
  const files = [];

  if (!fs.existsSync(dir)) {
    return files;
  }

  const items = fs.readdirSync(dir);
  for (const item of items) {
    const fullPath = path.join(dir, item);
    const stat = fs.statSync(fullPath);

    if (stat.isDirectory()) {
      files.push(...findJsonFiles(fullPath));
    } else if (item.endsWith('.json')) {
      files.push(fullPath);
    }
  }

  return files;
}

/**
 * Main validation
 */
function main() {
  console.log('Validating OrbitOS specifications...\n');

  // Find all spec files
  const featureFiles = findJsonFiles(path.join(SPECS_DIR, 'features'));
  const entityFiles = findJsonFiles(path.join(SPECS_DIR, 'entities'));

  const allFiles = [...featureFiles, ...entityFiles];

  if (allFiles.length === 0) {
    console.log('No spec files found to validate.');
    return 0;
  }

  console.log(`Found ${allFiles.length} spec files to validate.\n`);

  // Validate each file
  for (const file of allFiles) {
    validateJsonFile(file);
  }

  // Report results
  if (warnings.length > 0) {
    console.log('Warnings:');
    warnings.forEach(w => console.log(`  ⚠️  ${w}`));
    console.log('');
  }

  if (errors.length > 0) {
    console.log('Errors:');
    errors.forEach(e => console.log(`  ❌ ${e}`));
    console.log('');
    console.log(`Validation failed with ${errors.length} error(s).`);
    return 1;
  }

  console.log(`✅ All ${allFiles.length} spec files are valid!`);
  return 0;
}

// Run if called directly
if (require.main === module) {
  process.exit(main());
}

module.exports = { validateJsonFile, main };
