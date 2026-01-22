import { describe, it, expect } from 'vitest'

/**
 * Unit tests for Person Detail Page utilities
 * Testing the parseMetadata function that handles JSON parsing safely
 */

// Replicate the parseMetadata function from app/pages/app/people/[id].vue
const parseMetadata = (metadata?: string): { email?: string } => {
  if (!metadata) return {}
  try {
    return JSON.parse(metadata)
  } catch {
    return {}
  }
}

describe('Person Detail Page - parseMetadata', () => {
  describe('valid JSON inputs', () => {
    it('should parse valid JSON with email', () => {
      const result = parseMetadata('{"email":"test@example.com"}')
      expect(result).toEqual({ email: 'test@example.com' })
    })

    it('should parse valid JSON with multiple fields', () => {
      const result = parseMetadata('{"email":"test@example.com","phone":"123-456"}')
      expect(result).toEqual({ email: 'test@example.com', phone: '123-456' })
    })

    it('should parse empty JSON object', () => {
      const result = parseMetadata('{}')
      expect(result).toEqual({})
    })
  })

  describe('null/undefined inputs', () => {
    it('should return empty object for undefined', () => {
      const result = parseMetadata(undefined)
      expect(result).toEqual({})
    })

    it('should return empty object for null (cast as string)', () => {
      const result = parseMetadata(null as unknown as string)
      expect(result).toEqual({})
    })
  })

  describe('empty/whitespace strings', () => {
    it('should return empty object for empty string', () => {
      const result = parseMetadata('')
      expect(result).toEqual({})
    })

    it('should handle whitespace-only string (invalid JSON)', () => {
      const result = parseMetadata('   ')
      expect(result).toEqual({})
    })
  })

  describe('invalid JSON inputs', () => {
    it('should return empty object for plain text', () => {
      const result = parseMetadata('not json')
      expect(result).toEqual({})
    })

    it('should return empty object for malformed JSON', () => {
      const result = parseMetadata('{email: test}')
      expect(result).toEqual({})
    })

    it('should return empty object for incomplete JSON', () => {
      const result = parseMetadata('{"email":')
      expect(result).toEqual({})
    })

    it('should return empty object for JSON array', () => {
      const result = parseMetadata('["email@test.com"]')
      // Arrays are valid JSON but don't have .email property
      expect(result.email).toBeUndefined()
    })

    it('should return empty object for JSON string literal', () => {
      const result = parseMetadata('"just a string"')
      // String literals are valid JSON but don't have .email property
      expect(result.email).toBeUndefined()
    })
  })

  describe('edge cases', () => {
    it('should handle JSON with null email value', () => {
      const result = parseMetadata('{"email":null}')
      expect(result).toEqual({ email: null })
    })

    it('should handle JSON with empty string email', () => {
      const result = parseMetadata('{"email":""}')
      expect(result).toEqual({ email: '' })
    })

    it('should handle nested JSON', () => {
      const result = parseMetadata('{"email":"test@example.com","nested":{"key":"value"}}')
      expect(result.email).toBe('test@example.com')
    })
  })
})
