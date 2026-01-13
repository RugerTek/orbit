// =============================================================================
// OrbitOS Commitlint Configuration
// =============================================================================
// Enforces conventional commit message format.
// See: https://www.conventionalcommits.org/
// =============================================================================

module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    // Type must be one of the allowed values
    'type-enum': [
      2,
      'always',
      [
        'feat',     // New feature
        'fix',      // Bug fix
        'docs',     // Documentation only
        'style',    // Formatting, no code change
        'refactor', // Code change that neither fixes nor adds
        'test',     // Adding or updating tests
        'chore',    // Maintenance tasks
        'ci',       // CI/CD changes
        'perf',     // Performance improvement
        'build',    // Build system changes
        'revert',   // Revert a previous commit
        'spec',     // Specification changes
      ],
    ],
    // Scope is optional but encouraged
    'scope-enum': [
      1, // Warning, not error
      'always',
      [
        'api',
        'web',
        'mobile',
        'contracts',
        'specs',
        'ci',
        'auth',
        'admin',
        'deps',
      ],
    ],
    // Subject requirements
    'subject-case': [2, 'always', 'lower-case'],
    'subject-empty': [2, 'never'],
    'subject-full-stop': [2, 'never', '.'],
    // Header length
    'header-max-length': [2, 'always', 100],
    // Body requirements
    'body-leading-blank': [2, 'always'],
    'body-max-line-length': [1, 'always', 100],
    // Footer requirements
    'footer-leading-blank': [2, 'always'],
  },
  helpUrl: 'https://www.conventionalcommits.org/',
};
