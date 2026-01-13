# OrbitOS Specifications

## Overview

This directory contains the single source of truth for all OrbitOS features, entities, APIs, and architectural decisions. **All code must trace back to specifications in this directory.**

## Directory Structure

```
specs/
├── features/       # F###: Feature specifications (what to build)
├── entities/       # ENT###: Data model specifications (domain objects)
├── api/            # API contracts and endpoint definitions
│   └── endpoints/  # Individual API endpoint specs
└── decisions/      # ADR###: Architecture Decision Records (why)
```

## For AI Assistants

### Before Writing Any Code

1. **Find the relevant spec file(s)** for your task
2. **Read the spec completely** before generating code
3. **Include traceability markers** in all generated code:
   - `/// <remarks>FEATURE: F###</remarks>` for handlers/services
   - `/// <remarks>ENTITY: ENT###</remarks>` for domain entities
   - `// SPEC: F### - description` for frontend components

### Spec File Formats

**Features (features/F###-*.json)**:
```json
{
  "id": "F001",
  "name": "Feature Name",
  "status": "draft|approved|implemented",
  "capabilities": [...],
  "acceptance_criteria": [...],
  "entities": ["ENT001", "ENT002"],
  "api_endpoints": ["POST /endpoint", "GET /endpoint"]
}
```

**Entities (entities/ENT###-*.json)**:
```json
{
  "id": "ENT001",
  "name": "EntityName",
  "description": "...",
  "fields": [...],
  "validation_rules": [...],
  "relationships": [...],
  "api_representation": {...}
}
```

**Decisions (decisions/ADR###-*.md)**:
- Use ADR template format
- Must be approved before implementation

### Validation Requirements

Before any PR can be merged:
- [ ] All new code references a spec (F### or ENT###)
- [ ] All spec acceptance criteria have tests
- [ ] API endpoints match OpenAPI contract
- [ ] Entity fields match database schema

## For Humans

### Creating a New Feature

1. Copy `features/_template.json` to `features/F{NNN}-{name}.json`
2. Fill in all required fields
3. Get approval (change status to "approved")
4. Only then begin implementation

### Creating a New Entity

1. Copy `entities/_template.json` to `entities/ENT{NNN}-{name}.json`
2. Define all fields with types and validation rules
3. Update related feature specs to reference the entity
4. Update OpenAPI contract if entity is exposed via API

### Making Architectural Decisions

1. Copy `decisions/_template.md` to `decisions/ADR{NNN}-{title}.md`
2. Document context, decision, and consequences
3. Get team review and approval
4. Update status to "accepted"

## Spec Registry

| Type | Count | Index File |
|------|-------|------------|
| Features | See index | [features/index.json](features/index.json) |
| Entities | See index | [entities/index.json](entities/index.json) |
| Decisions | See docs | Listed in decisions/ |

## Enforcement

These specs are enforced through:
- **Pre-commit hooks**: Validate spec references in code
- **CI pipeline**: Contract tests verify API matches specs
- **Architecture tests**: Verify entity fields match specs
- **Code review**: Traceability markers required
