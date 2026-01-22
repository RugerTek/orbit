# OrbitOS Workspace - AI Development Guide

## Overview

This is the **OrbitOS monorepo workspace** containing all repositories for the AI-native business operating system. This file provides AI assistants with the context needed to develop, test, and validate across all projects.

## Workspace Structure

```
/OrbitOS-Workspace/
├── CLAUDE.md                    # This file - workspace-level AI instructions
├── .ai/                         # AI orchestration and context
│   ├── commands/                # AI-executable scripts
│   ├── context/                 # Generated context files
│   └── prompts/                 # Reusable prompt templates
├── contracts/                   # Shared API contracts (OpenAPI, Protobuf)
│   ├── openapi.yaml            # OpenAPI 3.1 specification
│   └── schemas/                # JSON schemas for validation
├── specs/                       # Symlink to Operations-Tool/docs/srs
├── orbitos-api/                 # .NET 8 Backend API
├── orbitos-web/                 # Nuxt 4 + Vuetify Web Frontend
└── orbitos-mobile/              # Flutter Mobile App
```

## Quick Reference

| Repo | Tech Stack | Port | Command to Run |
|------|------------|------|----------------|
| orbitos-api | .NET 8, EF Core, PostgreSQL | 5000 | `dotnet run` |
| orbitos-web | Nuxt 4, Vuetify 3, Pinia | 3000 | `npm run dev` |
| orbitos-mobile | Flutter 3.x, BLoC | N/A | `flutter run` |

## AI Development Protocol

### Before Writing Any Code

1. **Read the specification first**:
   ```
   specs/manifest.yaml           # AI navigation guide
   specs/L4-data/entities/       # Entity definitions
   specs/L3-domain/              # Business rules
   specs/nfr/                    # Non-functional requirements
   ```

2. **Check the API contract**:
   ```
   contracts/openapi.yaml        # All endpoints defined here
   ```

3. **Understand traceability**:
   - Every entity maps to: ENT-###
   - Every feature maps to: F#
   - Every requirement maps to: REQ-{LAYER}-{CATEGORY}-###

### Code Standards (Enforced Across All Repos)

| Standard | Requirement |
|----------|-------------|
| **Type Safety** | No `any` types (TypeScript), no `dynamic` (C#/Dart) |
| **Validation** | All inputs validated at boundaries |
| **Multi-Tenancy** | All queries MUST be scoped by `organization_id` |
| **Soft Delete** | Never hard delete - set `deleted_at` timestamp |
| **Audit Fields** | All entities: `created_at`, `updated_at`, `created_by`, `updated_by` |
| **Error Handling** | Result types for business logic, exceptions for infrastructure |

### Testing Requirements (ISO 29119 Aligned)

| Test Type | Coverage | Framework |
|-----------|----------|-----------|
| Unit Tests | 80% minimum | xUnit (.NET), Vitest (Nuxt), flutter_test |
| Integration Tests | 100% of APIs | TestContainers + actual DB |
| Contract Tests | 100% of endpoints | Pact / OpenAPI validation |
| E2E Tests | Critical paths | Playwright (Web), integration_test (Flutter) |
| Security Tests | OWASP Top 10 | OWASP ZAP, dependency scanning |
| Performance Tests | NFR compliance | k6, Lighthouse |

## AI Commands

The AI can execute these commands to run, test, and validate:

### Run All Services
```bash
# Terminal 1: Start API
cd orbitos-api && dotnet run

# Terminal 2: Start Web
cd orbitos-web && npm run dev

# Terminal 3: Start Mobile (iOS Simulator)
cd orbitos-mobile && flutter run
```

### Run All Tests
```bash
# API Tests
cd orbitos-api && dotnet test --collect:"XPlat Code Coverage"

# Web Tests
cd orbitos-web && npm run test:unit && npm run test:e2e

# Mobile Tests
cd orbitos-mobile && flutter test

# Contract Tests (validates all repos against OpenAPI)
npm run test:contracts
```

### Validate Against Specs
```bash
# Validate OpenAPI spec is complete
npm run validate:openapi

# Validate entities match database schema
npm run validate:entities

# Validate all traceability links
npm run validate:traceability
```

## Entity to Code Mapping

When implementing an entity from the specs:

| Spec Location | .NET Location | Nuxt Location | Flutter Location |
|--------------|---------------|---------------|------------------|
| `specs/L4-data/entities/ENT-###.json` | `src/OrbitOS.Domain/Entities/` | `app/types/entities/` | `lib/domain/entities/` |
| `specs/L3-domain/business-rules/` | `src/OrbitOS.Domain/Rules/` | `app/composables/rules/` | `lib/domain/rules/` |
| `specs/L3-domain/authorization/` | `src/OrbitOS.Domain/Authorization/` | `app/middleware/` | `lib/core/auth/` |
| `specs/nfr/NFR-SEC-security.json` | Cross-cutting | Cross-cutting | Cross-cutting |

## Feature Implementation Checklist

When implementing a feature (e.g., F1 - Canvas Management):

- [ ] Read `specs/features/F1-canvas-management.json`
- [ ] Identify required entities from feature spec
- [ ] Read each entity definition from `specs/L4-data/entities/`
- [ ] Identify business rules from `specs/L3-domain/`
- [ ] Check validation rules in `specs/L4-data/constraints/`
- [ ] Implement API endpoints per `contracts/openapi.yaml`
- [ ] Implement domain logic with business rules
- [ ] Write unit tests (80% coverage)
- [ ] Write integration tests (API contract)
- [ ] Write E2E tests (critical user flows)
- [ ] Update traceability in spec files

## Multi-Tenancy Implementation

**CRITICAL**: Every database query MUST be scoped by `organization_id`.

### .NET (EF Core)
```csharp
// Global query filter in DbContext
modelBuilder.Entity<Canvas>().HasQueryFilter(c => c.OrganizationId == _tenantId);
```

### Nuxt (API calls)
```typescript
// All API calls include org context from auth
const { data } = await $fetch(`/api/organizations/${orgId}/canvases`);
```

### Flutter (Repository)
```dart
// Organization context from auth state
final canvases = await canvasRepository.getAll(organizationId: authState.orgId);
```

## SOC 2 Compliance Checklist

Reference: `specs/L5-infrastructure/security/SEC-001-soc2-compliance.json`

Before any PR:
- [ ] No secrets in code (check SEC-CC8-01)
- [ ] Input validation on all endpoints (check SEC-PI1-02)
- [ ] Audit logging for security events (check SEC-CC4-01)
- [ ] Proper error handling (no stack traces to client)
- [ ] Rate limiting on public endpoints (check NFR-SEC-008)

## Getting Started for AI

1. **First time setup**:
   ```bash
   cd OrbitOS-Workspace
   ./scripts/setup-workspace.sh
   ```

2. **Start development**:
   ```bash
   ./scripts/start-all.sh
   ```

3. **Run full validation**:
   ```bash
   ./scripts/validate-all.sh
   ```

## Key Files to Read

| Purpose | File |
|---------|------|
| AI Navigation | `specs/manifest.yaml` |
| All Entities | `specs/L4-data/index.json` |
| Business Rules | `specs/L3-domain/index.json` |
| Security Requirements | `specs/nfr/NFR-SEC-security.json` |
| SOC 2 Controls | `specs/L5-infrastructure/security/SEC-002-controls-matrix.json` |
| API Contract | `contracts/openapi.yaml` |

## Version Compatibility

| Component | Version | Notes |
|-----------|---------|-------|
| .NET | 8.0 LTS | Long-term support |
| Node.js | 20 LTS | For Nuxt build |
| Flutter | 3.x stable | Latest stable |
| PostgreSQL | 15+ | JSON support required |
| Docker | 24+ | For TestContainers |

---

## Vue/Nuxt UI Implementation Rules

### Dialog/Modal Implementation

**ALWAYS use the `BaseDialog` component** for any modal/dialog UI:

```vue
<BaseDialog
  v-model="showDialog"
  size="lg"
  title="My Dialog Title"
  subtitle="Optional description"
  @submit="handleSubmit"
>
  <!-- Form content here -->

  <template #footer="{ close }">
    <button @click="close" class="orbitos-btn-secondary">Cancel</button>
    <button @click="handleSubmit" class="orbitos-btn-primary">Submit</button>
  </template>
</BaseDialog>
```

**CRITICAL: Component Naming**
- Use `<BaseDialog>`, **NOT** `<UiBaseDialog>`
- Nuxt auto-imports from `components/` directory
- `UiBaseDialog` would look for `components/ui/BaseDialog.vue` which doesn't exist
- Using wrong component name causes form content to render at page bottom instead of in dialog!

**If you must create a custom dialog, follow these rules:**

1. **NEVER use `@click.self` on modal backdrops** - it's unreliable and causes dialogs to close when clicking form fields
2. **ALWAYS use separate backdrop div with `@click` + content div with `@click.stop`**:
   ```vue
   <Teleport to="body">
     <div v-if="showDialog" class="fixed inset-0 z-50 ...">
       <!-- Backdrop - closes on click -->
       <div class="absolute inset-0 bg-black/60" @click="showDialog = false" />
       <!-- Content - stops propagation -->
       <div class="relative ..." @click.stop>
         <!-- Your dialog content -->
       </div>
     </div>
   </Teleport>
   ```
3. **ALWAYS wrap modals in `<Teleport to="body">`** to avoid z-index issues
4. **ALWAYS support Escape key** to close the dialog

### Before Marking UI Work Complete

When creating or modifying dialog/modal UIs, verify:

- [ ] Clicking between form fields does NOT close the dialog
- [ ] Clicking empty space inside the dialog does NOT close it
- [ ] Clicking color pickers, dropdowns, or other controls does NOT close it
- [ ] Escape key closes the dialog
- [ ] Clicking the dark backdrop DOES close the dialog
- [ ] Cancel button closes the dialog
- [ ] No console errors during interaction

### Form Best Practices

- Use proper `<label>` elements with `for` attributes or wrap inputs
- Add `autofocus` to the first field in dialogs
- Disable submit buttons when required fields are empty
- Show loading state during async operations
- Clear form state when dialog closes

### Dialog Troubleshooting

| Symptom | Cause | Solution |
|---------|-------|----------|
| Form content appears at page bottom | Using `<UiBaseDialog>` instead of `<BaseDialog>` | Change to `<BaseDialog>` |
| Dialog closes when clicking form fields | Custom dialog using `@click.self` on container | Use BaseDialog or separate backdrop/content divs |
| Dialog doesn't close on Escape | Missing keyboard handler | Use BaseDialog (has built-in support) |
| z-index issues with nested dialogs | Not using Teleport | Use BaseDialog (teleports to body) |

---

## Implemented Features Reference

### F001 - Authentication (Status: Implemented)
**Spec:** `specs/features/F001-authentication.json`

**Capabilities:**
- Email/password login with JWT tokens
- Google OAuth2 SSO (popup and One Tap)
- Microsoft Azure AD authentication (optional)
- Session persistence with localStorage
- Multi-provider authentication support

**Key Files:**
| Component | Location |
|-----------|----------|
| Login Page | `orbitos-web/app/pages/index.vue` |
| Auth Composable | `orbitos-web/app/composables/useAuth.ts` |
| Auth Controller | `orbitos-api/src/OrbitOS.Api/Controllers/AuthController.cs` |

**E2E Tests:** `orbitos-web/tests/e2e/login.spec.ts`

---

### F002 - Super Admin Panel (Status: Implemented)
**Spec:** `specs/features/F002-super-admin.json`

**Capabilities:**
- Dashboard with system statistics
- User CRUD (create, read, update, soft-delete)
- Organization CRUD with slug generation
- Global roles management
- Global functions management
- Password reset functionality

**Key Files:**
| Component | Location |
|-----------|----------|
| Admin Dashboard | `orbitos-web/app/pages/admin/index.vue` |
| Users Page | `orbitos-web/app/pages/admin/users.vue` |
| Organizations Page | `orbitos-web/app/pages/admin/organizations.vue` |
| Super Admin Composable | `orbitos-web/app/composables/useSuperAdmin.ts` |
| Super Admin Controller | `orbitos-api/src/OrbitOS.Api/Controllers/SuperAdminController.cs` |

**E2E Tests:** `orbitos-web/tests/e2e/super-admin.spec.ts`, `orbitos-web/tests/e2e/super-admin-crud.spec.ts`

---

### F003 - Multi-Agent AI Chat (Status: In Progress)
**Spec:** `specs/features/F003-multi-agent-chat.json`

**Phase 1 - AI Agent Management (Complete):**
- Create, edit, delete AI agents
- Configure model (Claude Sonnet/Opus/Haiku)
- System prompt templates (CFO, Operations, Strategy, HR)
- Avatar color customization
- Max tokens and temperature settings
- Toggle agent active/inactive

**Phase 2 - Group Chat (Implemented):**
- Create conversations with multiple participants
- Send messages and invoke AI agents
- @mention detection for agent invocation
- Conversation modes: OnDemand, Moderated, RoundRobin, Free
- Pause/resume conversations
- Session stats (messages, tokens, cost, duration)

**Key Files:**
| Component | Location |
|-----------|----------|
| AI Agents Page | `orbitos-web/app/pages/app/ai-agents/index.vue` |
| Conversation Page | `orbitos-web/app/pages/app/ai-agents/conversations/[id].vue` |
| AI Agents Composable | `orbitos-web/app/composables/useAiAgents.ts` |
| Conversations Composable | `orbitos-web/app/composables/useConversations.ts` |
| AI Agents Controller | `orbitos-api/src/OrbitOS.Api/Controllers/AiAgentsController.cs` |
| Conversations Controller | `orbitos-api/src/OrbitOS.Api/Controllers/ConversationsController.cs` |
| Multi-Provider AI Service | `orbitos-api/src/OrbitOS.Api/Services/MultiProviderAiService.cs` |

**E2E Tests:** `orbitos-web/tests/e2e/ai-agents.spec.ts`, `orbitos-web/tests/e2e/conversations.spec.ts`

---

### F004 - Business Model Canvas (Status: Draft/Partial)
**Spec:** `specs/features/F004-business-canvas.json`

**Capabilities:**
- 9-block Business Model Canvas view
- Three view modes: Canvas, Kanban, List
- Canvas scopes: Organization, Product, Segment, Initiative
- Partners, Channels, Value Propositions, Customer Relationships, Revenue Streams
- Reusable entity references across canvases

**Key Files:**
| Component | Location |
|-----------|----------|
| Business Canvas Page | `orbitos-web/app/pages/app/business-canvas.vue` |
| Canvases List | `orbitos-web/app/pages/app/canvases/index.vue` |
| Entity Modal | `orbitos-web/app/components/business-canvas/EntityModal.vue` |
| Canvas Controller | `orbitos-api/src/OrbitOS.Api/Controllers/Operations/CanvasesController.cs` |

**E2E Tests:** `orbitos-web/tests/e2e/canvas-*.spec.ts`

---

### F005 - AI Data Access & CRUD (Status: Draft/Partial)
**Spec:** `specs/features/F005-ai-data-access.json`

**Capabilities:**
- AI agents have read access to all organization data
- Organization context injection into AI prompts
- AI tool calling for entity operations (create, update, delete)
- Pending action workflow for user confirmation
- Audit trail for AI-proposed changes

**Key Files:**
| Component | Location |
|-----------|----------|
| Pending Actions Page | `orbitos-web/app/pages/app/assignments.vue` |
| Pending Action Card | `orbitos-web/app/components/ai/PendingActionCard.vue` |
| Pending Actions Composable | `orbitos-web/app/composables/usePendingActions.ts` |
| AI Chat Service | `orbitos-api/src/OrbitOS.Api/Services/AiChatService.cs` |
| Organization Context Service | `orbitos-api/src/OrbitOS.Api/Services/OrganizationContextService.cs` |

---

### F007 - Contextual Help System (Status: Implemented)
**Spec:** `specs/features/F007-help-system.json`

**Capabilities:**
- Persistent help sidebar on right edge with "?" toggle button
- Context-aware content that updates based on current page
- Four tabs: Overview, Fields, Actions, Tips
- Spotlight search with Cmd+K / Ctrl+K
- Interactive walkthroughs with step-by-step guidance
- Field-level tooltips
- Related pages navigation

**Key Files:**
| Component | Location |
|-----------|----------|
| Help Sidebar | `orbitos-web/app/components/help/HelpSidebar.vue` |
| Help Spotlight | `orbitos-web/app/components/help/HelpSpotlight.vue` |
| Help Panel | `orbitos-web/app/components/help/HelpPanel.vue` |
| Help Button | `orbitos-web/app/components/help/HelpButton.vue` |
| Walkthrough Overlay | `orbitos-web/app/components/help/WalkthroughOverlay.vue` |
| Help Composable | `orbitos-web/app/composables/useHelp.ts` |

**Keyboard Shortcuts:**
| Shortcut | Action |
|----------|--------|
| `Cmd+K` / `Ctrl+K` | Open spotlight search |
| `Escape` | Close sidebar, panel, or walkthrough |
| `Arrow Keys` | Navigate walkthrough steps |

**Help Content Coverage:**
- Dashboard (`/app`)
- People Management (`/app/people`)
- Org Chart (`/app/people/org-chart`)
- Roles (`/app/roles`)
- Functions (`/app/functions`)
- Processes (`/app/processes`)
- Resources (`/app/resources`)
- Goals (`/app/goals`)
- AI Agents (`/app/ai-agents`)
- Business Canvas (`/app/canvases`)
- Pending Actions (`/app/assignments`)
- User Profile (`/app/profile`)
- Settings (`/app/settings`)

---

### F008 - User Profile & Settings (Status: Implemented)
**Spec:** `specs/features/F008-user-profile.json`

**Capabilities:**
- View and edit user profile (display name, first name, last name)
- Change password with current password verification
- Password strength meter with requirements checklist
- Security section showing linked authentication methods
- Link Google account to existing user
- Settings page with preferences and account management
- Delete account with confirmation flow

**Key Files:**
| Component | Location |
|-----------|----------|
| Profile Page | `orbitos-web/app/pages/app/profile.vue` |
| Settings Page | `orbitos-web/app/pages/app/settings.vue` |
| Profile Avatar | `orbitos-web/app/components/profile/ProfileAvatar.vue` |
| Profile Edit Dialog | `orbitos-web/app/components/profile/ProfileEditDialog.vue` |
| Change Password Dialog | `orbitos-web/app/components/profile/ChangePasswordDialog.vue` |
| Security Section | `orbitos-web/app/components/profile/SecuritySection.vue` |
| User Profile Composable | `orbitos-web/app/composables/useUserProfile.ts` |
| User Types | `orbitos-web/app/types/user.ts` |
| Auth Controller (Profile endpoints) | `orbitos-api/src/OrbitOS.Api/Controllers/AuthController.cs` |

**API Endpoints:**
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/auth/profile` | Get current user's full profile |
| PUT | `/api/auth/profile` | Update profile (display name, names) |
| PUT | `/api/auth/change-password` | Change password with verification |
| POST | `/api/auth/link-google` | Link Google account to user |

**E2E Tests:** `orbitos-web/tests/e2e/profile.spec.ts`, `orbitos-web/tests/e2e/profile-interactions.spec.ts`

---

## Operations Domain Features

### People Management
**Route:** `/app/people`

**Capabilities:**
- Full CRUD for team members (resources of type Person)
- Role assignment with capacity allocation
- Status tracking: Available, Stable, Near Capacity, Overloaded
- Quick stats: Total, Overloaded, Near Capacity, Available

**Key Files:**
| Component | Location |
|-----------|----------|
| People Page | `orbitos-web/app/pages/app/people/index.vue` |
| Person Detail | `orbitos-web/app/pages/app/people/[id].vue` |
| Operations Composable | `orbitos-web/app/composables/useOperations.ts` |

**E2E Tests:** `orbitos-web/tests/e2e/people-crud.spec.ts`, `orbitos-web/tests/e2e/operations-people.spec.ts`

---

### Organizational Chart
**Route:** `/app/people/org-chart`

**Capabilities:**
- Three view modes: Tree (Vue Flow), List (table), Cards (grid)
- Reporting relationship management
- Vacancy creation and filling
- Metrics: Total People, Open Vacancies, Org Depth, Avg Span of Control
- Click to select and edit reporting structure

**Key Files:**
| Component | Location |
|-----------|----------|
| Org Chart Page | `orbitos-web/app/pages/app/people/org-chart.vue` |
| Org Chart Tree | `orbitos-web/app/components/org-chart/OrgChartTree.vue` |
| Org Chart Node | `orbitos-web/app/components/org-chart/OrgChartNode.vue` |
| Org Chart Cards | `orbitos-web/app/components/org-chart/OrgChartCards.vue` |
| Org Chart List | `orbitos-web/app/components/org-chart/OrgChartList.vue` |
| Reporting Dialog | `orbitos-web/app/components/org-chart/OrgChartReportingDialog.vue` |
| Vacancy Dialog | `orbitos-web/app/components/org-chart/OrgChartVacancyDialog.vue` |

**E2E Tests:** `orbitos-web/tests/e2e/org-chart.spec.ts` (100+ comprehensive tests)

---

### Roles Management
**Route:** `/app/roles`

**Capabilities:**
- Role CRUD with department categorization
- Function assignment to roles (many-to-many)
- Coverage status: Covered, At Risk, Uncovered
- SearchableAssigner for function assignment

**Key Files:**
| Component | Location |
|-----------|----------|
| Roles Page | `orbitos-web/app/pages/app/roles.vue` |
| Role Detail | `orbitos-web/app/pages/app/roles/[id].vue` |
| Searchable Assigner | `orbitos-web/app/components/SearchableAssigner.vue` |

**E2E Tests:** `orbitos-web/tests/e2e/operations-roles.spec.ts`, `orbitos-web/tests/e2e/role-detail.spec.ts`

---

### Functions Catalog
**Route:** `/app/functions`

**Capabilities:**
- Function CRUD with categories/departments
- Role assignment (which roles can perform this function)
- Coverage tracking: Fully Covered, At Risk, SPOF (Single Point of Failure)
- Dual view: Table and Grid modes
- Function capabilities management

**Key Files:**
| Component | Location |
|-----------|----------|
| Functions Page | `orbitos-web/app/pages/app/functions.vue` |
| Function Detail | `orbitos-web/app/pages/app/functions/[id].vue` |

**E2E Tests:** `orbitos-web/tests/e2e/people-functions-comprehensive.spec.ts`

---

### Process Management
**Route:** `/app/processes`

**Capabilities:**
- Process CRUD with purpose, trigger, output fields
- Process status: Active, Draft, Deprecated
- State type: Current or Target state
- Linked function tracking

**Key Files:**
| Component | Location |
|-----------|----------|
| Processes List | `orbitos-web/app/pages/app/processes/index.vue` |
| Process Editor | `orbitos-web/app/pages/app/processes/[id].vue` |

---

### Process Flow Editor
**Route:** `/app/processes/[id]`

**Capabilities:**
- Vue Flow-based process diagram editor
- Activity types: Manual, Automated, Hybrid, Decision, Handoff
- Custom nodes: Start, End, Activity, Decision
- Drag-to-reposition and connect nodes
- Implicit (sequential) vs Explicit (user-defined) flow modes
- Edge/connection management
- Minimap and zoom controls

**Key Files:**
| Component | Location |
|-----------|----------|
| Process Flow Canvas | `orbitos-web/app/components/process/ProcessFlowCanvas.vue` |
| Activity Node | `orbitos-web/app/components/process/nodes/ActivityNode.vue` |
| Decision Node | `orbitos-web/app/components/process/nodes/DecisionNode.vue` |
| Start Node | `orbitos-web/app/components/process/nodes/StartNode.vue` |
| End Node | `orbitos-web/app/components/process/nodes/EndNode.vue` |
| Process Flow Composable | `orbitos-web/app/composables/useProcessFlow.ts` |

**E2E Tests:** `orbitos-web/tests/e2e/process-editor.spec.ts` (40+ tests)

---

### Resources Registry
**Route:** `/app/resources`

**Capabilities:**
- Resource CRUD with types: Person, Team, Tool, Automation, Partner, Asset
- Status tracking: Active, Inactive, Archived
- Subtype management
- Stats by resource type

**Key Files:**
| Component | Location |
|-----------|----------|
| Resources Page | `orbitos-web/app/pages/app/resources.vue` |
| Resources Controller | `orbitos-api/src/OrbitOS.Api/Controllers/Operations/ResourcesController.cs` |

---

## Reusable Components Reference

### BaseDialog
**Location:** `orbitos-web/app/components/BaseDialog.vue`

The standard dialog component for all modals. Features:
- Proper backdrop/content separation (no @click.self)
- Escape key support
- Enter key submission
- Multiple sizes: sm, md, lg, xl, 2xl
- Teleports to body
- v-model binding

### SearchableAssigner
**Location:** `orbitos-web/app/components/SearchableAssigner.vue`

Multi-select assignment component for entity relationships. Props:
- `assigned: AssignableItem[]`
- `available: AssignableItem[]`
- `label: string`
- Events: `@add(id)`, `@remove(id)`

### OrganizationSwitcher
**Location:** `orbitos-web/app/components/OrganizationSwitcher.vue`

Multi-organization dropdown in sidebar. Features:
- Current org display with avatar
- Dropdown list of user's organizations
- "Create new organization" trigger

### AiAssistant
**Location:** `orbitos-web/app/components/AiAssistant.vue`

Floating AI chat widget (bottom-right). Features:
- Minimizable chat panel
- Quick action buttons
- Chat history with timestamps
- Real-time typing indicator

### HelpSidebar
**Location:** `orbitos-web/app/components/help/HelpSidebar.vue`

Persistent right-edge help sidebar with context-aware documentation. Features:
- Purple "?" toggle button on right edge (always visible)
- Auto-updates content based on current route
- Four tabs: Overview, Fields, Actions, Tips
- Related pages navigation links
- Collapse state persisted in localStorage
- Teleported to body for proper z-index handling

### HelpSpotlight
**Location:** `orbitos-web/app/components/help/HelpSpotlight.vue`

Cmd+K spotlight search for help content. Features:
- Search across features, concepts, and field help
- Keyboard navigation
- Results grouped by type

### ProfileAvatar
**Location:** `orbitos-web/app/components/profile/ProfileAvatar.vue`

User avatar component with initials fallback. Features:
- Displays user avatar image if available
- Falls back to initials (from first/last name or display name)
- Multiple sizes: sm, md, lg, xl
- Gradient background for initials

### ProfileEditDialog
**Location:** `orbitos-web/app/components/profile/ProfileEditDialog.vue`

Dialog for editing user profile. Features:
- Edit display name, first name, last name
- Real-time validation
- Uses BaseDialog for proper modal behavior
- Loading states during save

### ChangePasswordDialog
**Location:** `orbitos-web/app/components/profile/ChangePasswordDialog.vue`

Dialog for changing user password. Features:
- Current password verification
- Password strength meter (weak/medium/strong)
- Requirements checklist (length, uppercase, lowercase, number)
- Password visibility toggles
- Confirm password matching

### SecuritySection
**Location:** `orbitos-web/app/components/profile/SecuritySection.vue`

Security information display. Features:
- Shows linked auth methods (Password, Google, Microsoft)
- Change password button
- Link Google account button
- Last login and account creation timestamps

---

## Composables Reference

| Composable | Purpose | Key Methods |
|------------|---------|-------------|
| `useApi` | HTTP client wrapper | `get()`, `post()`, `put()`, `delete()` |
| `useAuth` | Authentication state | `loginWithEmail()`, `loginWithGoogle()`, `logout()` |
| `useOrganizations` | Org context management | `fetchOrganizations()`, `createOrganization()` |
| `useOperations` | Core operations data | Full CRUD for all entities, stats |
| `useProcessFlow` | Vue Flow integration | Converts processes to nodes/edges |
| `useAiAgents` | AI agent management | Agent CRUD, model listing |
| `useConversations` | Multi-agent chat | Conversation CRUD, messaging, invocation |
| `usePendingActions` | AI action approval | Approve/reject workflow |
| `useSuperAdmin` | Admin operations | User/Org/Role/Function CRUD |
| `useHelp` | Contextual help system | `openSpotlight()`, `toggleHelpPanel()`, `searchHelp()`, `startWalkthrough()` |
| `useUserProfile` | User profile management | `fetchProfile()`, `updateProfile()`, `changePassword()`, `linkGoogleAccount()` |

---

## E2E Test Suite Summary

| Test File | Coverage | Tests |
|-----------|----------|-------|
| `login.spec.ts` | Authentication flows | Login, logout, session |
| `super-admin.spec.ts` | Admin dashboard | Stats, navigation |
| `super-admin-crud.spec.ts` | Admin CRUD | Users, orgs, roles, functions |
| `ai-agents.spec.ts` | AI agent management | Full CRUD, models, templates |
| `conversations.spec.ts` | Multi-agent chat | Messaging, modes, participants |
| `org-chart.spec.ts` | Organizational chart | All views, dialogs, reporting |
| `process-editor.spec.ts` | Process flow design | Canvas, activities, edges |
| `canvas-*.spec.ts` | Business canvas | Views, entities, modals |
| `people-crud.spec.ts` | People management | Full CRUD lifecycle |
| `operations-roles.spec.ts` | Roles management | CRUD, function assignment |
| `comprehensive-user-journeys.spec.ts` | 10 user personas | Full application coverage |
| `profile.spec.ts` | User profile | View, edit, password change, settings |
| `profile-interactions.spec.ts` | Profile UX/UI | Button clicks, dialog behavior, forms |

**Run all tests:**
```bash
cd orbitos-web && npx playwright test
```

**Run specific test file:**
```bash
cd orbitos-web && npx playwright test tests/e2e/ai-agents.spec.ts
```

---

## Entity Registry Quick Reference

| Entity ID | Name | Table | Status |
|-----------|------|-------|--------|
| ENT001 | User | Users | Implemented |
| ENT002 | Organization | Organizations | Implemented |
| ENT003 | Role | Roles | Implemented |
| ENT004 | Function | Functions | Implemented |
| ENT014 | Resource | Resources | Implemented |
| ENT015 | Process | Processes | Implemented |
| ENT016 | Activity | Activities | Implemented |
| ENT017 | AiAgent | AiAgents | Implemented |
| ENT018 | Conversation | Conversations | Implemented |
| ENT019 | Message | Messages | Implemented |
| ENT020 | ConversationParticipant | ConversationParticipants | Implemented |
| ENT021 | Partner | Partners | Draft |
| ENT022 | Channel | Channels | Draft |
| ENT023 | ValueProposition | ValuePropositions | Draft |
| ENT024 | CustomerRelationship | CustomerRelationships | Draft |
| ENT025 | RevenueStream | RevenueStreams | Draft |
| ENT011 | Canvas | Canvases | Draft |
| ENT012 | CanvasBlock | CanvasBlocks | Draft |
| ENT013 | BlockReference | BlockReferences | Draft |

**Full entity definitions:** `specs/entities/index.json`
