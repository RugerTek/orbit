# Engineering Standards

> **Version:** 1.0.0
> **Status:** Canonical
> **Audience:** All engineers, AI coding assistants, contractors

This document defines the **non-negotiable engineering standards** for all development. Every pull request, every code review, every AI-generated implementation MUST comply with these standards.

---

## Table of Contents

1. [Core Principles](#1-core-principles)
2. [Code Organization](#2-code-organization)
3. [Naming Conventions](#3-naming-conventions)
4. [Type Safety Requirements](#4-type-safety-requirements)
5. [Testing Requirements](#5-testing-requirements)
6. [API Contracts](#6-api-contracts)
7. [Database Conventions](#7-database-conventions)
8. [Error Handling](#8-error-handling)
9. [Security Requirements](#9-security-requirements)
10. [Documentation Requirements](#10-documentation-requirements)
11. [Change Management](#11-change-management)
12. [Performance Standards](#12-performance-standards)
13. [Accessibility Standards](#13-accessibility-standards)
14. [Code Review Checklist](#14-code-review-checklist)

---

## 1. Core Principles

### 1.1 Specification-First Development

```
RULE: No code shall be written without a corresponding specification.

Every implementation MUST trace to:
  - An entity definition (ENT-###)
  - A requirement (REQ-{LAYER}-{CAT}-###)
  - A feature (F###)
  - A validation rule (R-###)
```

### 1.2 Contract-Driven Design

```
RULE: Interfaces are contracts. Breaking changes require versioning.

All public interfaces MUST:
  - Be defined with strict types (TypeScript/C#/Dart)
  - Have OpenAPI specs for REST endpoints
  - Have JSON Schema for data structures
  - Version breaking changes (v1 → v2)
```

### 1.3 Test-Driven Verification

```
RULE: Untested code is broken code.

Every feature MUST have:
  - Unit tests (≥80% coverage)
  - Integration tests (all happy paths)
  - Contract tests (API boundaries)
  - E2E tests (critical user journeys)
```

### 1.4 Traceability

```
RULE: Every artifact must be traceable.

Code → Tests → Requirements → Features → Business Value

If you can't trace it, you can't ship it.
```

---

## 2. Code Organization

### 2.1 Directory Structure (Backend - .NET)

```
/src
├── Domain/                     # Business logic (pure, no I/O)
│   ├── Entities/               # Domain models matching ENT-###
│   ├── ValueObjects/           # Immutable value types
│   ├── Services/               # Domain services
│   ├── Events/                 # Domain events
│   └── Errors/                 # Domain-specific errors
│
├── Application/                # Use cases / orchestration
│   ├── Commands/               # Write operations (CQRS)
│   ├── Queries/                # Read operations (CQRS)
│   ├── Handlers/               # Command/Query handlers
│   └── DTOs/                   # Data transfer objects
│
├── Infrastructure/             # External concerns
│   ├── Persistence/            # Database implementations
│   ├── Messaging/              # Event bus, queues
│   ├── External/               # Third-party integrations
│   └── Config/                 # Environment configuration
│
└── Api/                        # HTTP layer
    ├── Controllers/            # REST controllers
    ├── Middleware/             # Auth, validation, logging
    └── Filters/                # Exception filters
```

### 2.2 Directory Structure (Frontend - Vue/Nuxt)

```
/app (or /src)
├── components/                 # Reusable UI components
│   ├── common/                 # Generic components
│   └── {feature}/              # Feature-specific components
│
├── composables/                # Composition API hooks
│   ├── use{Feature}.ts         # Feature-specific logic
│   └── useApi.ts               # API abstraction
│
├── pages/                      # Route components
├── layouts/                    # Page layouts
├── stores/                     # Pinia state stores
├── types/                      # TypeScript types
└── utils/                      # Pure utility functions
```

### 2.3 Directory Structure (Mobile - Flutter)

```
/lib
├── core/                       # Cross-cutting concerns
│   ├── auth/                   # Authentication
│   ├── network/                # HTTP client
│   └── storage/                # Local persistence
│
├── domain/                     # Business logic
│   ├── entities/               # Domain models
│   ├── repositories/           # Repository interfaces
│   └── usecases/               # Use case classes
│
├── data/                       # Data layer
│   ├── repositories/           # Repository implementations
│   ├── datasources/            # Remote/local data sources
│   └── models/                 # DTOs and mappers
│
├── presentation/               # UI layer
│   ├── screens/                # Page widgets
│   ├── widgets/                # Reusable widgets
│   └── blocs/                  # BLoC state management
│
└── main.dart                   # App entry point
```

### 2.4 File Naming

| Type | .NET Convention | TypeScript Convention | Dart Convention |
|------|-----------------|----------------------|-----------------|
| Entity | `Organization.cs` | `organization.entity.ts` | `organization.dart` |
| Repository | `OrganizationRepository.cs` | `organization.repository.ts` | `organization_repository.dart` |
| Service | `RoleService.cs` | `role.service.ts` | `role_service.dart` |
| Controller | `OrganizationsController.cs` | `organizations.controller.ts` | N/A |
| DTO | `CreateOrganizationDto.cs` | `create-organization.dto.ts` | `create_organization_dto.dart` |
| Test | `OrganizationServiceTests.cs` | `organization.service.test.ts` | `organization_service_test.dart` |
| Component | N/A | `RoleCard.vue` | `role_card.dart` |

### 2.5 Import Order

```typescript
// 1. Framework/SDK imports
import { ref, computed } from 'vue';

// 2. External packages
import { z } from 'zod';

// 3. Internal packages (monorepo)
import { Logger } from '@project/logger';

// 4. Relative imports (parent → sibling → child)
import { BaseEntity } from '../base.entity';
import { OrganizationId } from './organization-id';

// 5. Type-only imports (last)
import type { OrganizationDTO } from './organization.dto';
```

---

## 3. Naming Conventions

### 3.1 General Rules

| Element | Convention | Example |
|---------|------------|---------|
| Files (TS/JS) | kebab-case | `role-assignment.service.ts` |
| Files (C#) | PascalCase | `RoleAssignmentService.cs` |
| Files (Dart) | snake_case | `role_assignment_service.dart` |
| Classes | PascalCase | `RoleAssignmentService` |
| Interfaces (C#) | `I` prefix | `IRoleAssignmentService` |
| Types | PascalCase | `OrganizationScope` |
| Functions | camelCase | `assignRoleToResource` |
| Variables | camelCase | `activeRoleAssignments` |
| Constants | SCREAMING_SNAKE_CASE | `MAX_ALLOCATION_PERCENTAGE` |
| Enums (C#) | PascalCase | `ResourceType.Person` |
| Database tables | snake_case, plural | `role_assignments` |
| Database columns | snake_case | `organization_id` |
| API endpoints | kebab-case, plural nouns | `/api/v1/role-assignments` |
| Environment vars | SCREAMING_SNAKE_CASE | `DATABASE_URL` |

### 3.2 Boolean Naming

```typescript
// Use positive, descriptive names
✅ isActive, hasPermission, canEdit, shouldValidate
❌ isNotActive, noPermission, cantEdit

// Prefix with: is, has, can, should, will, did
✅ isArchived, hasMetrics, canAssignRoles
❌ archived, metrics, assignRoles (ambiguous)
```

### 3.3 Function Naming

```typescript
// Commands: verb + noun
createOrganization(), assignRole(), archiveCanvas()

// Queries: get/find/list + noun
getOrganizationById(), findResourcesByType(), listActiveProcesses()

// Predicates: is/has/can + condition
isValidSlug(), hasActiveAssignments(), canDeleteResource()

// Event handlers: on + event
onRoleAssigned(), onProcessCompleted()

// Transformers: to + target
toDTO(), toEntity(), toResponse()
```

---

## 4. Type Safety Requirements

### 4.1 TypeScript Configuration

```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "noPropertyAccessFromIndexSignature": true
  }
}
```

### 4.2 C# Configuration

- Enable nullable reference types: `<Nullable>enable</Nullable>`
- Treat warnings as errors: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- No `dynamic` types allowed
- No `object` without explicit casting with validation

### 4.3 Forbidden Patterns

```typescript
// ❌ NEVER use 'any'
function process(data: any) { } // FORBIDDEN

// ✅ Use 'unknown' with type guards
function process(data: unknown) {
  if (isValidInput(data)) {
    // data is now typed
  }
}

// ❌ NEVER use type assertions without validation
const org = data as Organization; // FORBIDDEN

// ✅ Use runtime validation
const org = OrganizationSchema.parse(data);

// ❌ NEVER ignore null/undefined
const name = user.organization.name; // might crash

// ✅ Handle null cases explicitly
const name = user.organization?.name ?? 'Unknown';
```

### 4.4 Required Type Patterns

```typescript
// 1. Branded types for IDs (prevents ID mixups)
type OrganizationId = string & { readonly brand: unique symbol };
type ResourceId = string & { readonly brand: unique symbol };

// 2. Discriminated unions for variants
type Resource =
  | { type: 'person'; metadata: PersonMetadata }
  | { type: 'tool'; metadata: ToolMetadata }
  | { type: 'partner'; metadata: PartnerMetadata };

// 3. Result types for operations that can fail
type Result<T, E = Error> =
  | { success: true; data: T }
  | { success: false; error: E };

// 4. Zod schemas for runtime validation
const CreateOrganizationSchema = z.object({
  name: z.string().min(1).max(255),
  slug: z.string().regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/),
});
```

---

## 5. Testing Requirements

### 5.1 Test Categories

| Category | Scope | Requirement | Location |
|----------|-------|-------------|----------|
| Unit | Single function/class | ≥80% coverage | Alongside source or `/tests/unit/` |
| Integration | Multiple components | All repository methods | `/tests/integration/` |
| Contract | API boundaries | All endpoints | `/tests/contracts/` |
| E2E | User journeys | Critical paths | `/tests/e2e/` |
| Performance | Response times | NFR compliance | `/tests/performance/` |

### 5.2 Test File Structure

```typescript
describe('OrganizationService', () => {
  // Group by method
  describe('createOrganization', () => {
    // Happy path first
    it('should create organization with valid input', async () => {
      // Arrange
      const input = { name: 'Acme Corp', slug: 'acme-corp' };

      // Act
      const result = await service.createOrganization(input);

      // Assert
      expect(result.success).toBe(true);
      expect(result.data.slug).toBe('acme-corp');
    });

    // Edge cases
    it('should reject duplicate slug', async () => { });
    it('should reject invalid slug characters', async () => { });

    // Error cases
    it('should handle database errors gracefully', async () => { });
  });
});
```

### 5.3 Test Naming Convention

```typescript
// Pattern: should {expected behavior} when {condition}
it('should return 404 when organization not found', ...);
it('should prevent deletion when active assignments exist', ...);

// For validation rules, reference the rule ID
it('should enforce R-001: organization must have at least one member', ...);
```

### 5.4 Required Test Coverage by Entity

Every entity (ENT-###) MUST have:

1. **Schema validation tests** - Valid/invalid data
2. **Repository tests** - CRUD operations
3. **Business rule tests** - All R-### rules
4. **API endpoint tests** - All HTTP methods

---

## 6. API Contracts

### 6.1 REST Endpoint Standards

```yaml
# Every endpoint MUST define:
paths:
  /api/v1/organizations/{id}:
    get:
      operationId: getOrganizationById  # Unique, used for code gen
      tags: [Organizations]
      summary: Get organization by ID
      security:
        - bearerAuth: []
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        200:
          description: Organization found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Organization'
        401:
          $ref: '#/components/responses/Unauthorized'
        403:
          $ref: '#/components/responses/Forbidden'
        404:
          $ref: '#/components/responses/NotFound'
```

### 6.2 Response Envelope

```typescript
// All API responses use consistent envelope
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;        // Machine-readable: 'VALIDATION_ERROR'
    message: string;     // Human-readable: 'Invalid input'
    details?: unknown;   // Field-level errors
  };
  meta?: {
    requestId: string;   // For tracing
    timestamp: string;   // ISO 8601
    pagination?: {
      page: number;
      pageSize: number;
      totalPages: number;
      totalItems: number;
    };
  };
}
```

### 6.3 Standard Error Codes

```typescript
const ERROR_CODES = {
  // 400 Bad Request
  VALIDATION_ERROR: 'Input validation failed',
  INVALID_FORMAT: 'Data format is invalid',
  MISSING_FIELD: 'Required field is missing',

  // 401 Unauthorized
  INVALID_TOKEN: 'Authentication token is invalid',
  TOKEN_EXPIRED: 'Authentication token has expired',

  // 403 Forbidden
  INSUFFICIENT_PERMISSIONS: 'User lacks required permissions',
  ORGANIZATION_ACCESS_DENIED: 'User cannot access this organization',

  // 404 Not Found
  RESOURCE_NOT_FOUND: 'Requested resource does not exist',

  // 409 Conflict
  DUPLICATE_ENTRY: 'Resource already exists',
  CONSTRAINT_VIOLATION: 'Operation violates constraint',

  // 422 Unprocessable Entity
  BUSINESS_RULE_VIOLATION: 'Operation violates business rules',

  // 429 Too Many Requests
  RATE_LIMIT_EXCEEDED: 'Too many requests',

  // 500 Internal Server Error
  INTERNAL_ERROR: 'An unexpected error occurred',
  DATABASE_ERROR: 'Database operation failed',
  EXTERNAL_SERVICE_ERROR: 'External service is unavailable',
} as const;
```

---

## 7. Database Conventions

### 7.1 Table Design

```sql
-- Every table MUST have:
CREATE TABLE organizations (
  -- Primary key (UUID)
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

  -- Multi-tenancy (skip for global entities like User)
  organization_id UUID REFERENCES organizations(id),

  -- Required audit fields
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  created_by UUID REFERENCES users(id),
  updated_by UUID REFERENCES users(id),

  -- Soft delete
  deleted_at TIMESTAMPTZ,

  -- Business fields
  name VARCHAR(255) NOT NULL,
  slug VARCHAR(63) NOT NULL UNIQUE
);

-- Required indexes
CREATE INDEX idx_organizations_org_id ON organizations(organization_id);
CREATE INDEX idx_organizations_deleted ON organizations(deleted_at) WHERE deleted_at IS NULL;
```

### 7.2 Migration Standards

- **Idempotent** - Safe to run multiple times
- **Reversible** - Include up AND down migrations
- **Timestamped** - Format: `YYYYMMDDHHMMSS_description`
- **Atomic** - One logical change per migration

### 7.3 Query Patterns

```csharp
// Always use parameterized queries (prevent SQL injection)
❌ $"SELECT * FROM users WHERE id = '{userId}'"
✅ dbContext.Users.Where(u => u.Id == userId)

// Always scope by organization (multi-tenancy)
❌ dbContext.Resources.ToList()
✅ dbContext.Resources.Where(r => r.OrganizationId == orgId).ToList()

// Always exclude soft-deleted records (unless explicitly requested)
❌ dbContext.Organizations.ToList()
✅ dbContext.Organizations.Where(o => o.DeletedAt == null).ToList()
```

---

## 8. Error Handling

### 8.1 Error Classes

```csharp
// Define domain-specific error hierarchy
public abstract class DomainException : Exception
{
    public abstract string Code { get; }
    public abstract int HttpStatus { get; }
}

public class ValidationException : DomainException
{
    public override string Code => "VALIDATION_ERROR";
    public override int HttpStatus => 400;
    public Dictionary<string, string[]> Errors { get; }
}

public class NotFoundException : DomainException
{
    public override string Code => "RESOURCE_NOT_FOUND";
    public override int HttpStatus => 404;
}

public class BusinessRuleException : DomainException
{
    public override string Code => "BUSINESS_RULE_VIOLATION";
    public override int HttpStatus => 422;
    public string RuleId { get; }
}
```

### 8.2 Error Handling Patterns

```csharp
// ❌ Don't swallow errors
try { await DoSomething(); }
catch { /* silently ignored - FORBIDDEN */ }

// ❌ Don't throw generic errors
throw new Exception("Something went wrong");

// ✅ Throw specific, traceable errors
throw new BusinessRuleException("R-005", "Cannot delete resource with active assignments");

// ✅ Use Result types for expected failures
public Result<Organization> CreateOrganization(CreateOrgInput input)
{
    if (SlugExists(input.Slug))
        return Result<Organization>.Failure(new DuplicateSlugError(input.Slug));

    return Result<Organization>.Success(new Organization(input));
}
```

---

## 9. Security Requirements

### 9.1 Authentication

- Validate JWT token on every protected endpoint
- Check token expiration
- Verify user still exists and is active
- Attach user context to request

### 9.2 Authorization (Multi-Tenancy)

```csharp
// CRITICAL: Every organization-scoped operation MUST verify access
public async Task<Resource> GetResource(string userId, string orgId, string resourceId)
{
    // 1. Verify user has access to organization
    var membership = await GetMembership(userId, orgId);
    if (membership == null)
        throw new ForbiddenException("User has no access to this organization");

    // 2. Verify resource belongs to organization
    var resource = await _resourceRepo.FindById(resourceId);
    if (resource.OrganizationId != orgId)
        throw new NotFoundException("Resource", resourceId); // Don't leak existence

    // 3. Check permission level for operation
    if (!HasPermission(membership.AccessLevel, "resource:read"))
        throw new ForbiddenException("Insufficient permissions");

    return resource;
}
```

### 9.3 Input Validation

- Validate ALL input at API boundary
- Use schema validation (Zod/FluentValidation)
- Sanitize HTML to prevent XSS
- Never trust client-provided IDs for authorization

### 9.4 Secrets Management

- Never hardcode secrets
- Use environment variables
- Use secret managers for production
- Never log secrets (mask in output)

---

## 10. Documentation Requirements

### 10.1 Code Documentation

```csharp
/// <summary>
/// Assigns a person resource to a role within an organization.
/// </summary>
/// <param name="resourceId">The person resource to assign (must be type 'person')</param>
/// <param name="roleId">The role to assign to</param>
/// <param name="allocation">Percentage of time allocated (1-100)</param>
/// <exception cref="BusinessRuleException">R-004 if resource is not a person</exception>
/// <exception cref="BusinessRuleException">R-003 if total allocation exceeds 100%</exception>
/// <remarks>
/// ENTITY: ENT-010 RoleAssignment
/// RULES: R-003, R-004
/// </remarks>
public async Task<RoleAssignment> AssignRole(
    ResourceId resourceId,
    RoleId roleId,
    int allocation)
```

### 10.2 API Documentation

- Complete OpenAPI specification for all endpoints
- Include request/response examples
- Document error responses
- Reference business rules (R-###)

### 10.3 Architecture Decision Records (ADRs)

Major decisions MUST be documented with:
- Context
- Decision
- Consequences (positive and negative)
- Traceability to affected specs

---

## 11. Change Management

### 11.1 Breaking Change Definition

| Change Type | Breaking? | Action Required |
|-------------|-----------|-----------------|
| Remove API endpoint | YES | Major version bump |
| Remove request field | YES | Major version bump |
| Remove response field | YES | Major version bump |
| Change field type | YES | Major version bump |
| Make optional required | YES | Major version bump |
| Add required field | YES | Major version bump |
| Remove enum value | YES | Major version bump |
| Add optional field | NO | Minor version bump |
| Add enum value | NO | Minor version bump |
| Add new endpoint | NO | Minor version bump |
| Bug fix | NO | Patch version bump |

### 11.2 Deprecation Process

1. Mark as deprecated in spec
2. Add deprecation warning in code
3. Log usage warnings
4. Document migration path
5. Remove in next major version

---

## 12. Performance Standards

### 12.1 Response Time SLAs

| Operation Type | P50 | P95 | P99 | Max |
|----------------|-----|-----|-----|-----|
| API Read (single) | <50ms | <100ms | <200ms | 500ms |
| API Read (list) | <100ms | <200ms | <500ms | 1s |
| API Write | <100ms | <300ms | <500ms | 2s |
| Database query | <10ms | <50ms | <100ms | 200ms |
| UI initial load | <1s | <2s | <3s | 5s |
| UI interaction | <100ms | <200ms | <300ms | 500ms |

### 12.2 Query Optimization

```csharp
// ❌ N+1 queries - FORBIDDEN
foreach (var role in roles)
{
    role.Functions = await GetFunctions(role.Id); // Bad!
}

// ✅ Use eager loading or batch queries
var roles = await dbContext.Roles
    .Include(r => r.Functions)
    .ToListAsync();
```

---

## 13. Accessibility Standards

### 13.1 WCAG 2.1 AA Compliance

All UI components MUST meet WCAG 2.1 AA:

- **Keyboard navigation** - All interactive elements focusable
- **Focus indicators** - Visible focus states
- **Color contrast** - 4.5:1 minimum for text, 3:1 for large text
- **Screen reader support** - Proper ARIA labels
- **Reduced motion** - Respect `prefers-reduced-motion`

```vue
<!-- ✅ Proper labeling -->
<label for="org-name">Organization Name</label>
<input id="org-name" aria-describedby="org-name-help" />
<span id="org-name-help">Enter your company's legal name</span>
```

---

## 14. Code Review Checklist

Every PR MUST pass this checklist:

### Traceability
- [ ] Code traces to specification (ENT-###, REQ-###, F###)
- [ ] Tests trace to validation rules (R-###)
- [ ] Breaking changes documented with ADR

### Type Safety
- [ ] No `any` types (TypeScript) or `dynamic` (C#)
- [ ] No type assertions without validation
- [ ] Branded types for IDs
- [ ] Runtime validation for external input

### Testing
- [ ] Unit tests for new code (≥80% coverage)
- [ ] Integration tests for repository methods
- [ ] Contract tests for API changes
- [ ] Validation rule tests (R-### referenced)

### Security
- [ ] No hardcoded secrets
- [ ] Input validated at API boundary
- [ ] Multi-tenancy enforced (organization_id)
- [ ] Authorization checks in place

### Documentation
- [ ] XML/JSDoc comments for public functions
- [ ] OpenAPI updated for API changes
- [ ] README updated if needed

### Performance
- [ ] No N+1 queries
- [ ] Pagination for list endpoints
- [ ] Indexes for new queries

### Accessibility (UI)
- [ ] Keyboard navigable
- [ ] Screen reader labels
- [ ] Color contrast compliant

---

## Appendix: Templates

See `/specs/_meta/templates/` for:
- Entity template
- Feature template
- Business rule template
- ADR template
- Test templates

---

*Last Updated: 2025-01-13*
*Maintainer: Engineering Team*
