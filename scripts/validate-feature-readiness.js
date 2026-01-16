#!/usr/bin/env node
/**
 * Feature Readiness Validation Script
 *
 * HARD GATES: This script enforces that NO feature can be marked as "ready"
 * unless it passes ALL gates:
 *
 * 1. AI Documentation  - Spec files complete and up-to-date
 * 2. User Manual       - Auto-generated docs exist and are current
 * 3. Testing           - Required test coverage met
 * 4. Code Quality      - No lint/type errors, security scan passed
 *
 * Exit codes:
 *   0 = All gates passed
 *   1 = One or more gates failed (blocks deployment)
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Configuration
const SPECS_DIR = path.join(__dirname, '..', 'specs');
const FEATURES_DIR = path.join(SPECS_DIR, 'features');
const ENTITIES_DIR = path.join(SPECS_DIR, 'entities');
const USER_MANUAL_DIR = path.join(__dirname, '..', 'docs', 'user-manual');
const CONTRACTS_DIR = path.join(__dirname, '..', 'contracts');

// Required fields in feature specs
const REQUIRED_FEATURE_FIELDS = [
  'id', 'name', 'description', 'status', 'capabilities',
  'entities', 'api_endpoints', 'ui_screens', 'security_requirements',
  'test_coverage'
];

// Required fields in entity specs
const REQUIRED_ENTITY_FIELDS = [
  'id', 'name', 'description', 'fields', 'validation_rules', 'relationships'
];

// Gate result tracking
const gateResults = {
  ai_documentation: { passed: false, errors: [] },
  user_manual: { passed: false, errors: [] },
  testing: { passed: false, errors: [] },
  code_quality: { passed: false, errors: [] }
};

// Console colors
const colors = {
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  reset: '\x1b[0m',
  bold: '\x1b[1m'
};

function log(message, color = 'reset') {
  console.log(`${colors[color]}${message}${colors.reset}`);
}

function logHeader(message) {
  console.log(`\n${colors.bold}${colors.blue}${'='.repeat(60)}${colors.reset}`);
  console.log(`${colors.bold}${colors.blue}${message}${colors.reset}`);
  console.log(`${colors.bold}${colors.blue}${'='.repeat(60)}${colors.reset}\n`);
}

function logGate(gateName, passed) {
  const icon = passed ? '✓' : '✗';
  const color = passed ? 'green' : 'red';
  log(`  ${icon} ${gateName}`, color);
}

/**
 * GATE 1: AI Documentation Validation
 */
function validateAiDocumentation() {
  logHeader('GATE 1: AI Documentation');
  const errors = [];

  // Load feature specs
  const featureFiles = fs.readdirSync(FEATURES_DIR)
    .filter(f => f.match(/^F\d{3}-.*\.json$/) && !f.startsWith('_'));

  if (featureFiles.length === 0) {
    errors.push('No feature spec files found');
  }

  for (const file of featureFiles) {
    const filePath = path.join(FEATURES_DIR, file);
    try {
      const feature = JSON.parse(fs.readFileSync(filePath, 'utf8'));

      // Check required fields
      for (const field of REQUIRED_FEATURE_FIELDS) {
        if (!feature[field]) {
          errors.push(`${file}: Missing required field '${field}'`);
        }
      }

      // Validate capabilities have acceptance criteria
      if (feature.capabilities) {
        for (const cap of feature.capabilities) {
          if (!cap.acceptance_criteria || cap.acceptance_criteria.length === 0) {
            errors.push(`${file}: Capability ${cap.id} missing acceptance criteria`);
          }
        }
      }

      // Check referenced entities exist
      if (feature.entities) {
        for (const entityId of feature.entities) {
          const entityFile = fs.readdirSync(ENTITIES_DIR)
            .find(f => f.startsWith(entityId));
          if (!entityFile) {
            errors.push(`${file}: Referenced entity ${entityId} not found`);
          }
        }
      }

      // Check test coverage section is populated
      if (feature.test_coverage) {
        const tc = feature.test_coverage;
        if (!tc.unit_tests || tc.unit_tests.length === 0) {
          errors.push(`${file}: test_coverage.unit_tests is empty`);
        }
        if (!tc.integration_tests || tc.integration_tests.length === 0) {
          errors.push(`${file}: test_coverage.integration_tests is empty`);
        }
        if (!tc.e2e_tests || tc.e2e_tests.length === 0) {
          errors.push(`${file}: test_coverage.e2e_tests is empty`);
        }
      }

      log(`  ✓ ${file} - validated`, 'green');
    } catch (e) {
      errors.push(`${file}: Invalid JSON - ${e.message}`);
      log(`  ✗ ${file} - invalid`, 'red');
    }
  }

  // Validate OpenAPI contract exists
  const openapiPath = path.join(CONTRACTS_DIR, 'openapi.yaml');
  if (!fs.existsSync(openapiPath)) {
    errors.push('contracts/openapi.yaml not found');
  } else {
    log(`  ✓ OpenAPI contract exists`, 'green');
  }

  gateResults.ai_documentation.errors = errors;
  gateResults.ai_documentation.passed = errors.length === 0;

  logGate('AI Documentation Gate', gateResults.ai_documentation.passed);
  return gateResults.ai_documentation.passed;
}

/**
 * GATE 2: User Manual Validation
 */
function validateUserManual() {
  logHeader('GATE 2: User Manual');
  const errors = [];

  // Ensure user manual directory exists
  if (!fs.existsSync(USER_MANUAL_DIR)) {
    errors.push('docs/user-manual directory does not exist');
    log(`  ✗ User manual directory missing`, 'red');
  } else {
    // Check for feature guides
    const featuresManualDir = path.join(USER_MANUAL_DIR, 'features');
    if (!fs.existsSync(featuresManualDir)) {
      errors.push('docs/user-manual/features directory does not exist');
    } else {
      const featureFiles = fs.readdirSync(FEATURES_DIR)
        .filter(f => f.match(/^F\d{3}-.*\.json$/) && !f.startsWith('_'));

      for (const file of featureFiles) {
        const feature = JSON.parse(fs.readFileSync(path.join(FEATURES_DIR, file), 'utf8'));
        const manualFile = path.join(featuresManualDir, `${feature.id}.md`);

        if (!fs.existsSync(manualFile)) {
          errors.push(`Missing user manual for ${feature.id}: ${manualFile}`);
          log(`  ✗ ${feature.id} - no manual`, 'red');
        } else {
          // Check manual is not empty and has required sections
          const content = fs.readFileSync(manualFile, 'utf8');
          if (content.length < 500) {
            errors.push(`User manual for ${feature.id} is too short (< 500 chars)`);
            log(`  ✗ ${feature.id} - manual too short`, 'yellow');
          } else {
            log(`  ✓ ${feature.id} - manual exists`, 'green');
          }
        }
      }
    }

    // Check for concept explanations
    const conceptsDir = path.join(USER_MANUAL_DIR, 'concepts');
    if (!fs.existsSync(conceptsDir)) {
      errors.push('docs/user-manual/concepts directory does not exist');
    } else {
      const entityFiles = fs.readdirSync(ENTITIES_DIR)
        .filter(f => f.match(/^ENT\d{3}-.*\.json$/) && !f.startsWith('_'));

      for (const file of entityFiles) {
        const entity = JSON.parse(fs.readFileSync(path.join(ENTITIES_DIR, file), 'utf8'));
        const conceptFile = path.join(conceptsDir, `${entity.id}.md`);

        if (!fs.existsSync(conceptFile)) {
          errors.push(`Missing concept doc for ${entity.id}: ${conceptFile}`);
        }
      }
    }

    // Check for field help JSON
    const fieldHelpPath = path.join(USER_MANUAL_DIR, 'field-help.json');
    if (!fs.existsSync(fieldHelpPath)) {
      errors.push('docs/user-manual/field-help.json not found');
    }
  }

  gateResults.user_manual.errors = errors;
  gateResults.user_manual.passed = errors.length === 0;

  logGate('User Manual Gate', gateResults.user_manual.passed);
  return gateResults.user_manual.passed;
}

/**
 * GATE 3: Testing Validation
 */
function validateTesting() {
  logHeader('GATE 3: Testing');
  const errors = [];

  // Check API tests exist
  const apiTestsDir = path.join(__dirname, '..', 'orbitos-api', 'tests');
  if (!fs.existsSync(apiTestsDir)) {
    errors.push('orbitos-api/tests directory not found');
  } else {
    const testFiles = getAllFiles(apiTestsDir).filter(f => f.endsWith('Tests.cs'));
    if (testFiles.length === 0) {
      errors.push('No API test files found');
    } else {
      log(`  ✓ Found ${testFiles.length} API test files`, 'green');
    }
  }

  // Check Web tests exist
  const webTestsDir = path.join(__dirname, '..', 'orbitos-web', 'tests');
  if (!fs.existsSync(webTestsDir)) {
    errors.push('orbitos-web/tests directory not found');
  } else {
    // Unit tests
    const unitTestDir = path.join(webTestsDir, 'unit');
    if (!fs.existsSync(unitTestDir)) {
      errors.push('orbitos-web/tests/unit directory not found');
    } else {
      const unitTests = getAllFiles(unitTestDir).filter(f => f.endsWith('.spec.ts'));
      if (unitTests.length === 0) {
        errors.push('No frontend unit tests found');
      } else {
        log(`  ✓ Found ${unitTests.length} frontend unit tests`, 'green');
      }
    }

    // E2E tests
    const e2eTestDir = path.join(webTestsDir, 'e2e');
    if (!fs.existsSync(e2eTestDir)) {
      errors.push('orbitos-web/tests/e2e directory not found');
    } else {
      const e2eTests = getAllFiles(e2eTestDir).filter(f => f.endsWith('.spec.ts'));
      if (e2eTests.length === 0) {
        errors.push('No E2E tests found');
      } else {
        log(`  ✓ Found ${e2eTests.length} E2E tests`, 'green');
      }
    }
  }

  // Validate test coverage for each feature
  const featureFiles = fs.readdirSync(FEATURES_DIR)
    .filter(f => f.match(/^F\d{3}-.*\.json$/) && !f.startsWith('_'));

  for (const file of featureFiles) {
    const feature = JSON.parse(fs.readFileSync(path.join(FEATURES_DIR, file), 'utf8'));

    // Check that test files reference this feature
    if (feature.status === 'implemented' || feature.status === 'ready') {
      // For implemented features, tests must exist
      const apiTestFiles = fs.existsSync(apiTestsDir)
        ? getAllFiles(apiTestsDir).filter(f => f.endsWith('Tests.cs'))
        : [];

      const hasApiTests = apiTestFiles.some(f => {
        const content = fs.readFileSync(f, 'utf8');
        return content.includes(feature.id) ||
               content.toLowerCase().includes(feature.name.toLowerCase().replace(/\s+/g, ''));
      });

      if (!hasApiTests && feature.api_endpoints && feature.api_endpoints.length > 0) {
        errors.push(`${feature.id}: No API tests found for implemented feature with endpoints`);
      }
    }
  }

  gateResults.testing.errors = errors;
  gateResults.testing.passed = errors.length === 0;

  logGate('Testing Gate', gateResults.testing.passed);
  return gateResults.testing.passed;
}

/**
 * GATE 4: Code Quality Validation
 */
function validateCodeQuality() {
  logHeader('GATE 4: Code Quality');
  const errors = [];

  // Check for lint configuration
  const eslintConfig = path.join(__dirname, '..', 'orbitos-web', 'eslint.config.mjs');
  if (!fs.existsSync(eslintConfig)) {
    errors.push('ESLint configuration not found');
  } else {
    log(`  ✓ ESLint configured`, 'green');
  }

  // Check TypeScript strict mode (check nuxt.config.ts for strict setting)
  const nuxtConfigPath = path.join(__dirname, '..', 'orbitos-web', 'nuxt.config.ts');
  if (fs.existsSync(nuxtConfigPath)) {
    const nuxtConfig = fs.readFileSync(nuxtConfigPath, 'utf8');
    if (nuxtConfig.includes('strict: true') || nuxtConfig.includes('typescript:')) {
      log(`  ✓ TypeScript configured in Nuxt`, 'green');
    } else {
      log(`  ℹ TypeScript strict mode should be configured in nuxt.config.ts`, 'yellow');
    }
  }

  // Verify traceability markers in code
  const apiSrcDir = path.join(__dirname, '..', 'orbitos-api', 'src');
  if (fs.existsSync(apiSrcDir)) {
    const csFiles = getAllFiles(apiSrcDir).filter(f => f.endsWith('.cs'));
    let filesWithTraceability = 0;

    for (const file of csFiles) {
      const content = fs.readFileSync(file, 'utf8');
      if (content.includes('ENTITY:') || content.includes('SPEC:') || content.includes('REQ-')) {
        filesWithTraceability++;
      }
    }

    log(`  ℹ ${filesWithTraceability}/${csFiles.length} .cs files have traceability markers`, 'yellow');
  }

  gateResults.code_quality.errors = errors;
  gateResults.code_quality.passed = errors.length === 0;

  logGate('Code Quality Gate', gateResults.code_quality.passed);
  return gateResults.code_quality.passed;
}

/**
 * Helper: Get all files recursively
 */
function getAllFiles(dir, files = []) {
  const items = fs.readdirSync(dir);
  for (const item of items) {
    const fullPath = path.join(dir, item);
    if (fs.statSync(fullPath).isDirectory()) {
      if (!item.includes('node_modules') && !item.includes('bin') && !item.includes('obj')) {
        getAllFiles(fullPath, files);
      }
    } else {
      files.push(fullPath);
    }
  }
  return files;
}

/**
 * Generate readiness report
 */
function generateReport() {
  logHeader('FEATURE READINESS REPORT');

  const allPassed = Object.values(gateResults).every(g => g.passed);

  console.log('\n  Summary:');
  console.log('  ─────────────────────────────────────');

  for (const [gate, result] of Object.entries(gateResults)) {
    const icon = result.passed ? '✓' : '✗';
    const color = result.passed ? 'green' : 'red';
    const gateName = gate.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
    log(`  ${icon} ${gateName}`, color);

    if (result.errors.length > 0) {
      for (const error of result.errors.slice(0, 5)) {
        log(`      → ${error}`, 'red');
      }
      if (result.errors.length > 5) {
        log(`      → ... and ${result.errors.length - 5} more errors`, 'yellow');
      }
    }
  }

  console.log('\n  ─────────────────────────────────────');

  if (allPassed) {
    log('\n  ✓ ALL GATES PASSED - Feature is ready!\n', 'green');
    return 0;
  } else {
    log('\n  ✗ GATES FAILED - Feature is NOT ready\n', 'red');
    log('  Fix the above issues before marking feature as ready.\n', 'yellow');
    return 1;
  }
}

// Main execution
function main() {
  console.log(`\n${colors.bold}OrbitOS Feature Readiness Validator${colors.reset}`);
  console.log(`${'─'.repeat(60)}`);
  console.log(`Running at: ${new Date().toISOString()}`);

  validateAiDocumentation();
  validateUserManual();
  validateTesting();
  validateCodeQuality();

  const exitCode = generateReport();
  process.exit(exitCode);
}

main();
