#!/bin/bash

# =============================================================================
# OrbitOS GitHub Labels Setup
# =============================================================================
# Run this script to configure standardized labels for the repository.
# Usage: ./scripts/setup-github-labels.sh
# Requires: gh CLI authenticated
# =============================================================================

set -e

echo "Setting up OrbitOS GitHub labels..."

# Delete default labels (optional - uncomment if desired)
# gh label delete "bug" --yes 2>/dev/null || true
# gh label delete "enhancement" --yes 2>/dev/null || true
# gh label delete "documentation" --yes 2>/dev/null || true

# =============================================================================
# Type Labels
# =============================================================================

gh label create "type: feature" --description "New feature or enhancement" --color "0E8A16" --force
gh label create "type: bug" --description "Something isn't working" --color "D73A4A" --force
gh label create "type: spec" --description "Specification document" --color "5319E7" --force
gh label create "type: docs" --description "Documentation improvement" --color "0075CA" --force
gh label create "type: refactor" --description "Code restructuring" --color "FEF2C0" --force
gh label create "type: test" --description "Test improvements" --color "BFDADC" --force
gh label create "type: ci" --description "CI/CD changes" --color "D4C5F9" --force
gh label create "type: security" --description "Security related" --color "B60205" --force

# =============================================================================
# Priority Labels
# =============================================================================

gh label create "priority: P0" --description "Critical - Blocks release" --color "B60205" --force
gh label create "priority: P1" --description "High - Core functionality" --color "D93F0B" --force
gh label create "priority: P2" --description "Medium - Important" --color "FBCA04" --force
gh label create "priority: P3" --description "Low - Nice to have" --color "0E8A16" --force

# =============================================================================
# Status Labels
# =============================================================================

gh label create "status: needs-triage" --description "Needs initial review" --color "EDEDED" --force
gh label create "status: needs-spec" --description "Requires spec before work" --color "C2E0C6" --force
gh label create "status: spec-ready" --description "Spec approved, ready for work" --color "0E8A16" --force
gh label create "status: in-progress" --description "Currently being worked on" --color "FBCA04" --force
gh label create "status: blocked" --description "Blocked by dependency" --color "D73A4A" --force
gh label create "status: needs-review" --description "Ready for code review" --color "5319E7" --force
gh label create "status: approved" --description "Approved, ready to merge" --color "0E8A16" --force

# =============================================================================
# Component Labels
# =============================================================================

gh label create "component: api" --description "Backend API (orbitos-api)" --color "1D76DB" --force
gh label create "component: web" --description "Web frontend (orbitos-web)" --color "006B75" --force
gh label create "component: mobile" --description "Mobile app (orbitos-mobile)" --color "5319E7" --force
gh label create "component: contracts" --description "API contracts" --color "D4C5F9" --force
gh label create "component: specs" --description "Specifications" --color "C5DEF5" --force
gh label create "component: infra" --description "Infrastructure/CI" --color "BFD4F2" --force

# =============================================================================
# Spec Labels
# =============================================================================

gh label create "spec: F###" --description "Feature spec reference" --color "7057FF" --force
gh label create "spec: ENT###" --description "Entity spec reference" --color "008672" --force
gh label create "spec: ADR###" --description "ADR reference" --color "D4C5F9" --force

# =============================================================================
# Size Labels (for PRs)
# =============================================================================

gh label create "size: XS" --description "< 50 lines changed" --color "0E8A16" --force
gh label create "size: S" --description "50-200 lines changed" --color "C2E0C6" --force
gh label create "size: M" --description "200-500 lines changed" --color "FBCA04" --force
gh label create "size: L" --description "500-1000 lines changed" --color "D93F0B" --force
gh label create "size: XL" --description "> 1000 lines - needs split" --color "B60205" --force

# =============================================================================
# Special Labels
# =============================================================================

gh label create "breaking-change" --description "Introduces breaking changes" --color "B60205" --force
gh label create "good-first-issue" --description "Good for newcomers" --color "7057FF" --force
gh label create "help-wanted" --description "Extra attention needed" --color "008672" --force
gh label create "wontfix" --description "Will not be worked on" --color "FFFFFF" --force
gh label create "duplicate" --description "Duplicate of another issue" --color "CFD3D7" --force

echo "✅ GitHub labels configured successfully!"
echo ""
echo "Labels created:"
echo "  - Type labels (feature, bug, spec, docs, etc.)"
echo "  - Priority labels (P0-P3)"
echo "  - Status labels (needs-triage, in-progress, etc.)"
echo "  - Component labels (api, web, mobile, etc.)"
echo "  - Spec labels (F###, ENT###, ADR###)"
echo "  - Size labels (XS-XL)"
echo "  - Special labels (breaking-change, good-first-issue, etc.)"
