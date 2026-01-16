# OrbitOS AI Assistant Capabilities

This document tracks all AI assistant capabilities in OrbitOS. Update this document whenever new tools or capabilities are added.

## Overview

The AI assistant is powered by Claude (Anthropic) and uses tool/function calling to perform operations on the OrbitOS database. The assistant can help with people management, function management, and organizational analysis.

## Architecture

- **Service**: `AiChatService.cs` in `OrbitOS.Api/Services/`
- **API Endpoint**: `POST /api/organizations/{orgId}/chat`
- **Model**: Claude Sonnet (claude-sonnet-4-20250514)
- **Communication**: Direct HTTP calls to Anthropic API

## Available Tools

### People Management

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| `create_person` | Create a new person in the organization | `name`, `resourceSubtypeId` | `description` |
| `update_person` | Update an existing person's information | `personId` | `name`, `description`, `status` |
| `assign_role` | Assign a role to a person | `personId`, `roleId` | `allocationPercentage`, `isPrimary` |
| `add_capability` | Add a function capability to a person | `personId`, `functionId` | `level` (Learning, Capable, Expert, Master) |

### Function Management

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| `create_function` | Create a new business function | `name` | `description`, `purpose`, `category` |
| `update_function` | Update an existing function | `functionId` | `name`, `description`, `purpose`, `category` |
| `bulk_create_functions` | Create multiple functions at once | `functions` (array) | - |
| `suggest_functions` | Suggest new functions based on context | - | `context`, `count` |
| `suggest_improvements` | Analyze existing functions for improvements | - | - |
| `delete_function` | Delete a function | `functionId` | - |

### Analysis

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| `analyze_coverage` | Analyze organizational coverage gaps and SPOFs | - | - |

## Tool Details

### People Management Tools

#### `create_person`
Creates a new person (resource) in the organization.

**Parameters:**
- `name` (required): The person's full name
- `resourceSubtypeId` (required): The ID of the person subtype (e.g., Employee, Contractor)
- `description` (optional): Job title or description

**Returns:** Person ID, name

---

#### `update_person`
Updates an existing person's information.

**Parameters:**
- `personId` (required): The ID of the person to update
- `name` (optional): New name
- `description` (optional): New description
- `status` (optional): New status (Active, Inactive, Archived)

**Returns:** Updated person details

---

#### `assign_role`
Assigns a role to a person.

**Parameters:**
- `personId` (required): The ID of the person
- `roleId` (required): The ID of the role to assign
- `allocationPercentage` (optional): Percentage of time allocated (0-100), default: 100
- `isPrimary` (optional): Whether this is the primary role, default: false

**Returns:** Assignment ID

---

#### `add_capability`
Adds a function capability to a person.

**Parameters:**
- `personId` (required): The ID of the person
- `functionId` (required): The ID of the function
- `level` (optional): Capability level - Learning, Capable, Expert, Master. Default: Capable

**Returns:** Capability ID

---

### Function Management Tools

#### `create_function`
Creates a new business function (capability/skill) in the organization.

**Parameters:**
- `name` (required): The function name (e.g., "Data Migration", "Requirements Gathering")
- `description` (optional): Detailed description of what this function does
- `purpose` (optional): The business purpose or goal this function serves
- `category` (optional): Category to group related functions (e.g., "Technical", "Sales")

**Returns:** Function ID, name, category

---

#### `update_function`
Updates an existing function's details.

**Parameters:**
- `functionId` (required): The ID of the function to update
- `name` (optional): New name
- `description` (optional): New description
- `purpose` (optional): New purpose
- `category` (optional): New category

**Returns:** Updated function details

---

#### `bulk_create_functions`
Creates multiple functions at once. Useful for quickly setting up many related functions.

**Parameters:**
- `functions` (required): Array of function objects, each with:
  - `name` (required)
  - `description` (optional)
  - `purpose` (optional)
  - `category` (optional)

**Returns:** List of created functions, any errors

---

#### `suggest_functions`
Suggests new functions based on the organization's existing data, industry, or specific context.

**Parameters:**
- `context` (optional): Focus area for suggestions (e.g., "consulting", "technical", "support", "sales")
- `count` (optional): Number of suggestions to generate, default: 5

**Built-in suggestion categories:**
- `technical`: Code Review, Technical Documentation, System Architecture, etc.
- `consulting`: Business Analysis, Solution Design, Change Management, etc.
- `support`: Incident Management, Customer Escalation, SLA Monitoring, etc.
- `sales`: Lead Qualification, Proposal Writing, Contract Negotiation, etc.
- `general`: Project Management, Resource Planning, Quality Assurance, etc.

**Returns:** Table of suggested functions with name, category, and purpose

---

#### `suggest_improvements`
Analyzes existing functions and suggests improvements.

**Checks for:**
- Functions missing descriptions
- Functions missing categories
- Potential duplicate functions (similar names)
- Category distribution
- Uncovered functions (no one assigned)

**Returns:** Improvement suggestions with specific recommendations

---

#### `delete_function`
Deletes a function from the organization.

**Parameters:**
- `functionId` (required): The ID of the function to delete

**Behavior:**
- If the function has capability assignments, returns a warning asking for confirmation
- If no assignments, deletes immediately

**Returns:** Success message or warning

---

### Analysis Tools

#### `analyze_coverage`
Analyzes organizational coverage to identify gaps and single points of failure.

**Checks for:**
- Uncovered roles (no one assigned)
- Single points of failure (only one person in a role)
- Uncovered functions (no one has capability)
- Coverage metrics

**Returns:** Detailed analysis report with summary

---

## Context Provided to AI

The AI receives the following organization context on each request:

1. **Organization Name**
2. **People** (with roles and capabilities)
3. **Roles** (with assignment counts)
4. **Functions** (with capability counts)
5. **Person Subtypes** (available types for creating people)

## Example Conversations

### Creating Functions
```
User: "Add a function for data migration"
AI: [Uses create_function with name="Data Migration"]
    "Successfully created function Data Migration."

User: "Add technical consulting functions"
AI: [Uses bulk_create_functions]
    "Successfully created 5 functions:
    - Business Analysis
    - Requirements Gathering
    - Solution Design
    - Gap Analysis
    - Change Management"
```

### Getting Suggestions
```
User: "What functions should we add for our consulting team?"
AI: [Uses suggest_functions with context="consulting"]
    "Based on your organization, here are suggested functions:
    | Function | Category | Purpose |
    | Business Analysis | Consulting | Analyze client business needs |
    ..."

User: "Are our functions well organized?"
AI: [Uses suggest_improvements]
    "## Function Improvement Suggestions
    ### Functions Missing Descriptions
    - Data Migration
    - Requirements Gathering
    ..."
```

### Analyzing Coverage
```
User: "Do we have any single points of failure?"
AI: [Uses analyze_coverage]
    "## Organization Coverage Analysis
    ### Single Points of Failure
    - Technical Lead - only John Smith
    ..."
```

## Adding New Tools

When adding new tools to the AI assistant:

1. **Define the tool** in `BuildTools()` method of `AiChatService.cs`:
   - Set `Name`, `Description`, and `InputSchema`
   - Define required and optional properties

2. **Add handler** in `ExecuteToolAsync()` switch statement

3. **Implement the tool** method following the pattern:
   - Validate input
   - Perform database operations
   - Return `ToolResult` with action, message, and optional data

4. **Update system prompt** in `BuildSystemPrompt()` to mention the new capability

5. **Update this documentation** with:
   - Tool name and description
   - Parameters (required and optional)
   - Return values
   - Example usage

## Version History

| Date | Changes |
|------|---------|
| 2026-01-15 | Added Function Management tools: create_function, update_function, bulk_create_functions, suggest_functions, suggest_improvements, delete_function |
| 2024-XX-XX | Initial release with People Management and Analysis tools |
