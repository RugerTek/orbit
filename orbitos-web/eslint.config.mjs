// =============================================================================
// OrbitOS Web - ESLint Configuration
// =============================================================================
// Enforces strict TypeScript and Vue best practices.
// No 'any' types allowed. Enterprise-grade code quality.
// =============================================================================

import withNuxt from './.nuxt/eslint.config.mjs'

export default withNuxt({
  rules: {
    // =======================================================================
    // TypeScript Strict Rules
    // =======================================================================

    // CRITICAL: No 'any' types allowed
    '@typescript-eslint/no-explicit-any': 'error',
    '@typescript-eslint/no-implicit-any-catch': 'off', // Deprecated, handled by noImplicitAny

    // Enforce type safety
    '@typescript-eslint/explicit-function-return-type': 'off', // Nuxt infers types
    '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    '@typescript-eslint/no-non-null-assertion': 'warn',

    // =======================================================================
    // Vue Rules
    // =======================================================================

    // Component best practices
    'vue/multi-word-component-names': 'off', // Nuxt page naming
    'vue/require-default-prop': 'error',
    'vue/no-v-html': 'warn', // XSS prevention

    // Template style
    'vue/html-self-closing': ['error', {
      html: { void: 'always', normal: 'never', component: 'always' },
      svg: 'always',
      math: 'always',
    }],

    // =======================================================================
    // General Code Quality
    // =======================================================================

    // No console in production
    'no-console': process.env.NODE_ENV === 'production' ? 'error' : 'warn',
    'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'warn',

    // Best practices
    'no-var': 'error',
    'prefer-const': 'error',
    'eqeqeq': ['error', 'always'],
    'no-eval': 'error',
    'no-implied-eval': 'error',

    // Security
    'no-new-func': 'error',
  },
})
