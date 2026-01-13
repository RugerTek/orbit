# OrbitOS Git Workflow Standards

## Branch Strategy: GitHub Flow + Feature Flags

```
main (protected)
  │
  ├── feature/F001-user-authentication
  ├── feature/F002-super-admin-panel
  ├── fix/issue-123-login-redirect
  ├── refactor/auth-service-cleanup
  ├── docs/api-documentation
  └── chore/update-dependencies
```

## Branch Naming Convention

### Format
```
<type>/<scope>-<short-description>
```

### Types

| Type | Description | Example |
|------|-------------|---------|
| `feature/` | New feature from spec | `feature/F001-user-authentication` |
| `fix/` | Bug fix | `fix/issue-42-null-pointer` |
| `hotfix/` | Critical production fix | `hotfix/security-patch-xss` |
| `refactor/` | Code restructuring | `refactor/auth-service-cleanup` |
| `docs/` | Documentation only | `docs/api-endpoints` |
| `chore/` | Maintenance tasks | `chore/update-dependencies` |
| `test/` | Test additions | `test/auth-integration-tests` |
| `ci/` | CI/CD changes | `ci/add-security-scanning` |

### Rules

1. **Always lowercase** - No capitals in branch names
2. **Use hyphens** - Not underscores or camelCase
3. **Reference spec ID** - Features MUST include F### (e.g., `feature/F001-canvas-crud`)
4. **Reference issue** - Fixes SHOULD include issue number (e.g., `fix/issue-42-description`)
5. **Keep short** - Max 50 characters for description part
6. **No slashes in description** - Use hyphens instead

### Examples

```bash
# Good
feature/F001-user-authentication
feature/F002-super-admin-panel
fix/issue-123-login-redirect-loop
hotfix/CVE-2024-1234-sql-injection
refactor/consolidate-auth-handlers
docs/add-api-documentation
chore/bump-nuxt-to-4.3

# Bad
Feature/UserAuth              # Capitals
feature/user_auth             # Underscores
feature/add-new-feature       # No spec reference
fix/bug                       # Too vague
feature/F001/sub/feature      # Nested slashes
```

## Commit Message Convention

We use [Conventional Commits](https://www.conventionalcommits.org/) enforced by commitlint.

### Format

```
<type>(<scope>): <subject>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description | Triggers |
|------|-------------|----------|
| `feat` | New feature | Minor version bump |
| `fix` | Bug fix | Patch version bump |
| `docs` | Documentation | No release |
| `style` | Formatting only | No release |
| `refactor` | Code restructuring | No release |
| `test` | Adding tests | No release |
| `chore` | Maintenance | No release |
| `ci` | CI/CD changes | No release |
| `perf` | Performance | Patch version bump |
| `build` | Build system | No release |
| `revert` | Revert commit | Depends on reverted |
| `spec` | Specification changes | No release |

### Scopes

| Scope | Description |
|-------|-------------|
| `api` | Backend API changes |
| `web` | Frontend changes |
| `mobile` | Mobile app changes |
| `contracts` | API contract changes |
| `specs` | Specification files |
| `ci` | CI/CD configuration |
| `auth` | Authentication/authorization |
| `admin` | Admin panel |
| `deps` | Dependencies |

### Subject Rules

1. **Lowercase** - Start with lowercase letter
2. **No period** - Don't end with a period
3. **Imperative mood** - "add feature" not "added feature"
4. **Max 72 characters** - Keep it concise
5. **What, not how** - Describe the change, not implementation

### Examples

```bash
# Good
feat(api): add user registration endpoint
fix(web): resolve login redirect loop
docs(specs): add F003 organization management spec
refactor(auth): consolidate token validation logic
test(api): add integration tests for user CRUD
chore(deps): bump nuxt from 4.2 to 4.3
ci: add security scanning workflow

# With body and footer
feat(api): add multi-tenant organization support

Implements organization isolation for all queries.
All database queries now automatically filter by org_id.

BREAKING CHANGE: API responses now require org context
Closes #42
Refs: F002

# Bad
Added new feature                    # No type, vague
feat: Fix bug                        # Wrong type
feat(api): Added user registration.  # Past tense, period
FEAT(API): Add feature               # Capitals
feat(api): add a really long commit message that goes on and on  # Too long
```

### Breaking Changes

For breaking changes, add `BREAKING CHANGE:` in the footer or `!` after type:

```bash
feat(api)!: change authentication to JWT

BREAKING CHANGE: Session-based auth is no longer supported.
Migrate by updating all auth headers to use Bearer tokens.
```

## Pull Request Standards

### PR Title Format

Same as commit messages:
```
<type>(<scope>): <description>
```

### PR Description Template

Use the template at `.github/PULL_REQUEST_TEMPLATE.md`

### PR Checklist

Before requesting review:

- [ ] Branch is up to date with main
- [ ] All CI checks pass
- [ ] Self-review completed
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] Spec compliance verified

### PR Size Guidelines

| Size | Lines Changed | Review Time |
|------|---------------|-------------|
| XS | < 50 | 15 min |
| S | 50-200 | 30 min |
| M | 200-500 | 1 hour |
| L | 500-1000 | 2 hours |
| XL | > 1000 | Split required |

**Rule**: PRs over 500 lines should be split unless atomic.

## Workflow

### Starting New Work

```bash
# 1. Ensure main is current
git checkout main
git pull origin main

# 2. Create feature branch
git checkout -b feature/F001-user-authentication

# 3. Make changes and commit
git add .
git commit -m "feat(auth): add login endpoint"

# 4. Push and create PR
git push -u origin feature/F001-user-authentication
gh pr create --fill
```

### Keeping Branch Updated

```bash
# Rebase on main (preferred)
git fetch origin
git rebase origin/main

# Or merge (if rebase causes issues)
git merge origin/main
```

### After PR Approval

```bash
# Squash and merge (default)
# Done via GitHub UI or:
gh pr merge --squash
```

## Branch Protection Rules

### `main` Branch

Configure in GitHub Settings > Branches > Branch protection rules:

| Rule | Setting |
|------|---------|
| Require PR before merging | ✅ Enabled |
| Required approvals | 1 |
| Dismiss stale reviews | ✅ Enabled |
| Require status checks | ✅ Enabled |
| Required checks | `CI`, `Security Scan` |
| Require branches up to date | ✅ Enabled |
| Require signed commits | ⚡ Recommended |
| Include administrators | ✅ Enabled |
| Allow force pushes | ❌ Disabled |
| Allow deletions | ❌ Disabled |

### Setting Up (via CLI)

```bash
gh api repos/{owner}/{repo}/branches/main/protection -X PUT -f '{
  "required_status_checks": {
    "strict": true,
    "contexts": ["CI", "Security Scan"]
  },
  "required_pull_request_reviews": {
    "required_approving_review_count": 1,
    "dismiss_stale_reviews": true
  },
  "enforce_admins": true,
  "restrictions": null
}'
```

## Release Process

### Version Tagging

```bash
# Semantic versioning: MAJOR.MINOR.PATCH
git tag -a v1.0.0 -m "Release v1.0.0: Initial production release"
git push origin v1.0.0
```

### Release Notes

Use GitHub Releases:
```bash
gh release create v1.0.0 --generate-notes
```

## Quick Reference

```bash
# Create feature branch
git checkout -b feature/F###-description

# Create fix branch
git checkout -b fix/issue-###-description

# Good commit
git commit -m "feat(scope): description"

# Create PR
gh pr create --title "feat(scope): description" --body "..."

# View PR status
gh pr checks

# Merge PR
gh pr merge --squash
```
