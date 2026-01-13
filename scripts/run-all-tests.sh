#!/bin/bash

# =============================================================================
# OrbitOS Full Test Suite Runner
# =============================================================================
# Runs all tests: Unit, Integration, E2E
# Usage: ./scripts/run-all-tests.sh [--ci]
# =============================================================================

set -e

CI_MODE=${1:-""}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

echo "=============================================="
echo "OrbitOS Full Test Suite"
echo "=============================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track failures
FAILED_TESTS=()

# Function to run a test and track result
run_test() {
  local name=$1
  local command=$2
  local dir=$3

  echo -e "${YELLOW}Running: $name${NC}"
  echo "----------------------------------------"

  if [ -n "$dir" ]; then
    cd "$ROOT_DIR/$dir"
  fi

  if eval "$command"; then
    echo -e "${GREEN}✓ $name passed${NC}"
    echo ""
  else
    echo -e "${RED}✗ $name failed${NC}"
    FAILED_TESTS+=("$name")
    echo ""
  fi

  cd "$ROOT_DIR"
}

# =============================================================================
# 1. API Unit & Integration Tests
# =============================================================================
echo "=============================================="
echo "1. Backend Tests (.NET)"
echo "=============================================="

run_test "API Unit Tests" "dotnet test --configuration Release --verbosity minimal" "orbitos-api"

# =============================================================================
# 2. Frontend Unit Tests
# =============================================================================
echo "=============================================="
echo "2. Frontend Unit Tests (Vitest)"
echo "=============================================="

run_test "Frontend Unit Tests" "npm run test:unit" "orbitos-web"

# =============================================================================
# 3. Spec Validation
# =============================================================================
echo "=============================================="
echo "3. Spec Validation"
echo "=============================================="

run_test "Spec Validation" "node scripts/validate-specs.js" ""

# =============================================================================
# 4. E2E Tests (if not in CI mode without services)
# =============================================================================
if [ "$CI_MODE" != "--ci" ]; then
  echo "=============================================="
  echo "4. E2E Tests (Playwright)"
  echo "=============================================="
  echo "Note: E2E tests require API and Web to be running"
  echo "Skipping in local mode. Run separately with:"
  echo "  cd orbitos-web && npx playwright test"
  echo ""
fi

# =============================================================================
# Summary
# =============================================================================
echo "=============================================="
echo "Test Summary"
echo "=============================================="

if [ ${#FAILED_TESTS[@]} -eq 0 ]; then
  echo -e "${GREEN}All tests passed!${NC}"
  exit 0
else
  echo -e "${RED}Failed tests:${NC}"
  for test in "${FAILED_TESTS[@]}"; do
    echo -e "  ${RED}✗ $test${NC}"
  done
  exit 1
fi
