# Contributing to OrbitOS

Thank you for your interest in contributing to OrbitOS! This document provides guidelines and standards for contributing.

## Code of Conduct

Be respectful, inclusive, and professional. We're all here to build great software.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Set up the development environment (see [README.md](README.md))
4. Create a feature branch from `develop`

## Development Workflow

### Branch Naming

```
feature/F###-short-description    # New features
bugfix/issue-###-short-description # Bug fixes
hotfix/critical-issue-description  # Production fixes
refactor/component-name            # Code refactoring
docs/topic-name                    # Documentation
test/test-description              # Test additions
```

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, semicolons)
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(auth): add Google OAuth integration
fix(api): resolve null reference in user lookup
docs(readme): update installation instructions
test(users): add unit tests for user service
```

## Code Standards

### General

- Write self-documenting code with clear naming
- Keep functions small and focused
- Add comments only for complex logic
- No TODOs in production code (use GitHub issues)

### TypeScript/Vue (Frontend)

```typescript
// Use strict types - no 'any'
interface User {
  id: string;
  email: string;
  displayName: string;
}

// Use composables for shared logic
export function useUser() {
  const user = ref<User | null>(null);
  // ...
}
```

### C# (.NET API)

```csharp
// Use nullable reference types
public class UserService
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        // Always scope by organization
        return await _context.Users
            .Where(u => u.OrganizationId == _currentOrg.Id)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
```

### Multi-Tenancy

**CRITICAL**: All database queries MUST be scoped by `organization_id`.

```csharp
// Good
var users = await _context.Users
    .Where(u => u.OrganizationId == currentOrgId)
    .ToListAsync();

// Bad - data leak risk!
var users = await _context.Users.ToListAsync();
```

## Testing Requirements

### Coverage Thresholds

- **Minimum**: 60% line coverage
- **Target**: 80% line coverage

### Test Types

1. **Unit Tests** - Test individual functions/methods
2. **Integration Tests** - Test API endpoints with real database
3. **E2E Tests** - Test complete user flows

### Running Tests

```bash
# API
cd orbitos-api && dotnet test

# Web Unit
cd orbitos-web && npm run test:unit

# Web E2E
cd orbitos-web && npx playwright test
```

## Pull Request Process

### Before Submitting

1. **Update from develop**: `git pull origin develop`
2. **Run all tests**: Ensure they pass
3. **Check linting**: `npm run lint` / build warnings
4. **Self-review**: Check your own code first

### PR Checklist

- [ ] Code follows project style guidelines
- [ ] Tests added for new functionality
- [ ] All tests pass locally
- [ ] No secrets or credentials in code
- [ ] Documentation updated if needed
- [ ] Multi-tenancy rules followed

### PR Title Format

Same as commit messages:
```
feat(auth): add password reset functionality
fix(api): handle null organization gracefully
```

### PR Description

```markdown
## Summary
Brief description of changes

## Changes
- Added X
- Fixed Y
- Updated Z

## Test Plan
- [ ] Unit tests added
- [ ] Manual testing completed
- [ ] E2E tests pass

## Screenshots (if UI changes)
[Add screenshots]
```

## Review Process

1. All PRs require at least 1 approval
2. CI checks must pass
3. Address all review comments
4. Squash commits on merge

## Security

- Never commit secrets or credentials
- Use environment variables for configuration
- Follow OWASP guidelines
- Report security issues privately (see [SECURITY.md](SECURITY.md))

## Spec-Driven Development

When adding new features:

1. Check if a spec exists in `specs/features/`
2. If not, create one following the template
3. Reference spec IDs in code comments
4. Update spec status when complete

```csharp
/// <summary>
/// User management service
/// </summary>
/// <remarks>FEATURE: F002-user-management</remarks>
public class UserService { }
```

## Questions?

- Check existing documentation
- Search closed issues/PRs
- Open a discussion for general questions
- Open an issue for bugs/features

---

Thank you for contributing to OrbitOS!
