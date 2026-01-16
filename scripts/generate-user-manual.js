#!/usr/bin/env node
/**
 * User Manual Generator
 *
 * Automatically generates user documentation from spec files.
 * This ensures documentation is always in sync with the AI-maintained specs.
 *
 * Generated outputs:
 * - docs/user-manual/features/F###.md     - Feature guides
 * - docs/user-manual/concepts/ENT###.md   - Entity concept explanations
 * - docs/user-manual/field-help.json      - Field-level help tooltips
 * - docs/user-manual/walkthroughs/F###.json - Interactive walkthrough steps
 */

const fs = require('fs');
const path = require('path');

// Paths
const SPECS_DIR = path.join(__dirname, '..', 'specs');
const FEATURES_DIR = path.join(SPECS_DIR, 'features');
const ENTITIES_DIR = path.join(SPECS_DIR, 'entities');
const OUTPUT_DIR = path.join(__dirname, '..', 'docs', 'user-manual');

// Ensure output directories exist
function ensureDir(dir) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

/**
 * Generate feature guide from feature spec
 */
function generateFeatureGuide(feature) {
  const sections = [];

  // Header
  sections.push(`# ${feature.name}`);
  sections.push('');
  sections.push(`> ${feature.description}`);
  sections.push('');
  sections.push(`**Feature ID:** ${feature.id}  `);
  sections.push(`**Status:** ${feature.status}  `);
  sections.push(`**Last Updated:** ${feature.updated_at || 'N/A'}`);
  sections.push('');

  // Overview
  sections.push('## Overview');
  sections.push('');
  sections.push(`This guide explains how to use the **${feature.name}** feature in OrbitOS.`);
  sections.push('');

  // What you can do (capabilities)
  if (feature.capabilities && feature.capabilities.length > 0) {
    sections.push('## What You Can Do');
    sections.push('');

    for (const cap of feature.capabilities) {
      sections.push(`### ${cap.description.replace(/^As a user, I want to\s*/i, '').replace(/^As a .+?, I want to\s*/i, '')}`);
      sections.push('');

      if (cap.acceptance_criteria && cap.acceptance_criteria.length > 0) {
        sections.push('**Expected behavior:**');
        sections.push('');
        for (const criteria of cap.acceptance_criteria) {
          // Convert Given/When/Then to readable format
          const readable = criteria
            .replace(/^Given\s+/i, '- **Starting point:** ')
            .replace(/,?\s*when\s+/i, '\n- **Action:** ')
            .replace(/,?\s*then\s+/i, '\n- **Result:** ');
          sections.push(readable);
          sections.push('');
        }
      }
    }
  }

  // Related screens
  if (feature.ui_screens && feature.ui_screens.length > 0) {
    sections.push('## Where to Find It');
    sections.push('');
    sections.push('| Screen | Location | Description |');
    sections.push('|--------|----------|-------------|');

    for (const screen of feature.ui_screens) {
      sections.push(`| ${screen.name} | \`${screen.path}\` | ${screen.description} |`);
    }
    sections.push('');
  }

  // Step-by-step guide
  sections.push('## Step-by-Step Guide');
  sections.push('');

  if (feature.capabilities && feature.capabilities.length > 0) {
    let stepNum = 1;
    for (const cap of feature.capabilities) {
      const action = cap.description
        .replace(/^As a .+?, I want to\s*/i, '')
        .replace(/\s+so that.*$/i, '');

      sections.push(`### Step ${stepNum}: ${action}`);
      sections.push('');

      // Generate steps from acceptance criteria
      if (cap.acceptance_criteria && cap.acceptance_criteria.length > 0) {
        const successCriteria = cap.acceptance_criteria.find(c =>
          c.toLowerCase().includes('given valid') || c.toLowerCase().includes('success')
        );

        if (successCriteria) {
          sections.push('1. Navigate to the appropriate screen');
          sections.push('2. Enter the required information');
          sections.push('3. Click the action button');
          sections.push('4. Verify the expected result');
          sections.push('');
        }
      }

      stepNum++;
    }
  }

  // Security notes
  if (feature.security_requirements && feature.security_requirements.length > 0) {
    sections.push('## Security Information');
    sections.push('');
    sections.push('This feature implements the following security measures:');
    sections.push('');

    for (const req of feature.security_requirements) {
      sections.push(`- ${req}`);
    }
    sections.push('');
  }

  // Troubleshooting
  sections.push('## Troubleshooting');
  sections.push('');
  sections.push('If you encounter issues:');
  sections.push('');
  sections.push('1. Ensure you have the correct permissions for this feature');
  sections.push('2. Check that all required fields are filled in correctly');
  sections.push('3. Try refreshing the page');
  sections.push('4. Contact support if the issue persists');
  sections.push('');

  // Related concepts
  if (feature.entities && feature.entities.length > 0) {
    sections.push('## Related Concepts');
    sections.push('');
    sections.push('Learn more about the concepts used in this feature:');
    sections.push('');

    for (const entityId of feature.entities) {
      sections.push(`- [${entityId}](../concepts/${entityId}.md)`);
    }
    sections.push('');
  }

  // Footer
  sections.push('---');
  sections.push('');
  sections.push(`*This documentation was auto-generated from [${feature.id} spec](../../specs/features/${feature.id.toLowerCase()}-${feature.name.toLowerCase().replace(/\s+/g, '-')}.json).*`);
  sections.push('');

  return sections.join('\n');
}

/**
 * Generate concept explanation from entity spec
 */
function generateConceptDoc(entity) {
  const sections = [];

  // Header
  sections.push(`# ${entity.name}`);
  sections.push('');
  sections.push(`> ${entity.description}`);
  sections.push('');
  sections.push(`**Concept ID:** ${entity.id}`);
  sections.push('');

  // What is it
  sections.push('## What is it?');
  sections.push('');
  sections.push(`A **${entity.name}** in OrbitOS represents ${entity.description.toLowerCase()}.`);
  sections.push('');

  // Fields explanation
  if (entity.fields && entity.fields.length > 0) {
    sections.push('## Properties');
    sections.push('');
    sections.push('| Property | Description | Required |');
    sections.push('|----------|-------------|----------|');

    for (const field of entity.fields) {
      const required = field.required ? 'Yes' : 'No';
      const desc = field.description || field.name;
      sections.push(`| ${field.name} | ${desc} | ${required} |`);
    }
    sections.push('');
  }

  // Validation rules
  if (entity.validation_rules && entity.validation_rules.length > 0) {
    sections.push('## Business Rules');
    sections.push('');
    sections.push('The following rules apply to this concept:');
    sections.push('');

    for (const rule of entity.validation_rules) {
      sections.push(`- **${rule.id}**: ${rule.description}`);
    }
    sections.push('');
  }

  // Relationships
  if (entity.relationships && entity.relationships.length > 0) {
    sections.push('## Relationships');
    sections.push('');
    sections.push('This concept connects to other parts of OrbitOS:');
    sections.push('');

    for (const rel of entity.relationships) {
      sections.push(`- **${rel.type}** → ${rel.target}: ${rel.description || ''}`);
    }
    sections.push('');
  }

  // Footer
  sections.push('---');
  sections.push('');
  sections.push(`*This documentation was auto-generated from [${entity.id} spec](../../specs/entities/${entity.id.toLowerCase()}-${entity.name.toLowerCase().replace(/\s+/g, '-')}.json).*`);
  sections.push('');

  return sections.join('\n');
}

/**
 * Generate field help JSON for tooltips
 */
function generateFieldHelp(entities) {
  const fieldHelp = {};

  for (const entity of entities) {
    if (entity.fields) {
      for (const field of entity.fields) {
        const key = `${entity.id}.${field.name}`;
        fieldHelp[key] = {
          entity: entity.id,
          entityName: entity.name,
          field: field.name,
          type: field.type,
          description: field.description || `The ${field.name} of the ${entity.name}`,
          required: field.required || false,
          validation: field.validation || null,
          examples: field.examples || [],
          helpText: generateFieldHelpText(entity, field)
        };
      }
    }
  }

  return fieldHelp;
}

/**
 * Generate helpful text for a field
 */
function generateFieldHelpText(entity, field) {
  const tips = [];

  if (field.required) {
    tips.push('This field is required.');
  }

  if (field.validation) {
    if (field.validation.minLength) {
      tips.push(`Minimum ${field.validation.minLength} characters.`);
    }
    if (field.validation.maxLength) {
      tips.push(`Maximum ${field.validation.maxLength} characters.`);
    }
    if (field.validation.pattern) {
      tips.push('Must match a specific format.');
    }
  }

  if (tips.length === 0) {
    tips.push(`Enter the ${field.name.replace(/_/g, ' ')} for this ${entity.name}.`);
  }

  return tips.join(' ');
}

/**
 * Generate walkthrough steps from feature
 */
function generateWalkthrough(feature) {
  const steps = [];

  if (feature.capabilities) {
    let stepNum = 1;

    for (const cap of feature.capabilities) {
      // Generate intro step
      steps.push({
        step: stepNum++,
        title: cap.description.replace(/^As a .+?, I want to\s*/i, ''),
        type: 'highlight',
        target: cap.id,
        content: cap.description,
        position: 'bottom'
      });

      // Generate action steps based on acceptance criteria
      if (cap.acceptance_criteria) {
        for (const criteria of cap.acceptance_criteria) {
          if (criteria.toLowerCase().includes('when user')) {
            const action = criteria
              .replace(/.*when user\s*/i, '')
              .replace(/,\s*then.*/i, '');

            steps.push({
              step: stepNum++,
              title: action,
              type: 'action',
              content: `Complete this action: ${action}`,
              position: 'right'
            });
          }
        }
      }
    }

    // Add completion step
    steps.push({
      step: stepNum,
      title: 'Complete!',
      type: 'completion',
      content: `You've learned how to use ${feature.name}. Great job!`,
      position: 'center'
    });
  }

  return {
    featureId: feature.id,
    featureName: feature.name,
    totalSteps: steps.length,
    estimatedMinutes: Math.ceil(steps.length * 0.5),
    steps
  };
}

/**
 * Main generation function
 */
function generateAll() {
  console.log('User Manual Generator');
  console.log('═'.repeat(50));
  console.log('');

  // Ensure directories
  ensureDir(OUTPUT_DIR);
  ensureDir(path.join(OUTPUT_DIR, 'features'));
  ensureDir(path.join(OUTPUT_DIR, 'concepts'));
  ensureDir(path.join(OUTPUT_DIR, 'walkthroughs'));

  // Load all features
  const featureFiles = fs.readdirSync(FEATURES_DIR)
    .filter(f => f.match(/^F\d{3}-.*\.json$/) && !f.startsWith('_'));

  console.log(`Found ${featureFiles.length} feature specs`);

  const features = [];
  for (const file of featureFiles) {
    try {
      const feature = JSON.parse(fs.readFileSync(path.join(FEATURES_DIR, file), 'utf8'));
      features.push(feature);

      // Generate feature guide
      const guide = generateFeatureGuide(feature);
      const outputPath = path.join(OUTPUT_DIR, 'features', `${feature.id}.md`);
      fs.writeFileSync(outputPath, guide);
      console.log(`  ✓ Generated: features/${feature.id}.md`);

      // Generate walkthrough
      const walkthrough = generateWalkthrough(feature);
      const walkthroughPath = path.join(OUTPUT_DIR, 'walkthroughs', `${feature.id}.json`);
      fs.writeFileSync(walkthroughPath, JSON.stringify(walkthrough, null, 2));
      console.log(`  ✓ Generated: walkthroughs/${feature.id}.json`);

    } catch (e) {
      console.log(`  ✗ Error processing ${file}: ${e.message}`);
    }
  }

  // Load all entities
  const entityFiles = fs.readdirSync(ENTITIES_DIR)
    .filter(f => f.match(/^ENT\d{3}-.*\.json$/) && !f.startsWith('_'));

  console.log(`\nFound ${entityFiles.length} entity specs`);

  const entities = [];
  for (const file of entityFiles) {
    try {
      const entity = JSON.parse(fs.readFileSync(path.join(ENTITIES_DIR, file), 'utf8'));
      entities.push(entity);

      // Generate concept doc
      const conceptDoc = generateConceptDoc(entity);
      const outputPath = path.join(OUTPUT_DIR, 'concepts', `${entity.id}.md`);
      fs.writeFileSync(outputPath, conceptDoc);
      console.log(`  ✓ Generated: concepts/${entity.id}.md`);

    } catch (e) {
      console.log(`  ✗ Error processing ${file}: ${e.message}`);
    }
  }

  // Generate field help
  console.log('\nGenerating field help...');
  const fieldHelp = generateFieldHelp(entities);
  fs.writeFileSync(
    path.join(OUTPUT_DIR, 'field-help.json'),
    JSON.stringify(fieldHelp, null, 2)
  );
  console.log(`  ✓ Generated: field-help.json (${Object.keys(fieldHelp).length} fields)`);

  // Generate index
  console.log('\nGenerating index...');
  const index = {
    generated_at: new Date().toISOString(),
    features: features.map(f => ({
      id: f.id,
      name: f.name,
      status: f.status,
      guide: `features/${f.id}.md`,
      walkthrough: `walkthroughs/${f.id}.json`
    })),
    concepts: entities.map(e => ({
      id: e.id,
      name: e.name,
      doc: `concepts/${e.id}.md`
    }))
  };

  fs.writeFileSync(
    path.join(OUTPUT_DIR, 'index.json'),
    JSON.stringify(index, null, 2)
  );
  console.log(`  ✓ Generated: index.json`);

  // Summary
  console.log('\n' + '═'.repeat(50));
  console.log('Generation complete!');
  console.log(`  Features:     ${features.length}`);
  console.log(`  Concepts:     ${entities.length}`);
  console.log(`  Field help:   ${Object.keys(fieldHelp).length} fields`);
  console.log(`  Output dir:   ${OUTPUT_DIR}`);
  console.log('');
}

// Run
generateAll();
