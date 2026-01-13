## Summary

<!-- Brief description of what this PR does (1-3 sentences) -->

## Related Specs

<!-- Link to specification files this PR implements or modifies -->

- Feature: `specs/features/F###-name.json`
- Entities: `specs/entities/ENT###-name.json`
- ADR: `specs/decisions/ADR###-name.md` (if applicable)

## Type of Change

- [ ] Feature implementation (F###)
- [ ] Bug fix
- [ ] Refactoring (no functional changes)
- [ ] Documentation update
- [ ] Infrastructure/CI change
- [ ] Security fix

## Checklist

### Before Submitting

- [ ] I have read the [CLAUDE.md](../CLAUDE.md) development guidelines
- [ ] My code follows the project's coding standards
- [ ] I have performed a self-review of my code
- [ ] My changes do not introduce any new warnings or errors

### Specification Compliance

- [ ] Spec file exists for this feature/entity (or N/A)
- [ ] Implementation matches spec requirements
- [ ] All acceptance criteria are met (or documented exceptions)

### Code Quality

- [ ] No `any` types in TypeScript
- [ ] No `dynamic` types in C#
- [ ] All inputs are validated at boundaries
- [ ] Error handling follows project patterns

### Multi-Tenancy (if applicable)

- [ ] All database queries are scoped by `organization_id`
- [ ] No cross-tenant data leakage possible
- [ ] Soft delete used (no hard deletes)

### Testing

- [ ] Unit tests added/updated (80% coverage maintained)
- [ ] Integration tests added/updated (if API changes)
- [ ] All existing tests pass
- [ ] E2E tests updated (if UI changes)

### Security

- [ ] No secrets or credentials in code
- [ ] Input validation prevents injection attacks
- [ ] Authentication/authorization properly checked
- [ ] Audit logging added for security-relevant actions

### Documentation

- [ ] Code comments added where logic is complex
- [ ] API documentation updated (if endpoints changed)
- [ ] Spec files updated to reflect implementation

## Test Plan

<!-- How to test this PR manually -->

```bash
# Steps to test
```

## Screenshots (if applicable)

<!-- Add screenshots for UI changes -->

## Breaking Changes

<!-- List any breaking changes and migration steps -->

- [ ] No breaking changes
- [ ] Breaking changes documented below:

## Additional Notes

<!-- Any additional context or notes for reviewers -->

---

**Reviewer Checklist:**

- [ ] Code review completed
- [ ] Tests reviewed and adequate
- [ ] Spec compliance verified
- [ ] Security considerations reviewed
