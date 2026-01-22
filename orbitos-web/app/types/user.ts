// User Profile Types

export interface UserProfile {
  id: string
  email: string
  displayName: string
  firstName?: string
  lastName?: string
  avatarUrl?: string
  hasPassword: boolean
  hasGoogleId: boolean
  hasAzureAdId: boolean
  lastLoginAt?: string
  createdAt: string
  updatedAt: string
}

export interface UpdateProfileRequest {
  displayName: string
  firstName?: string
  lastName?: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export interface LinkGoogleRequest {
  credential: string
}

// Password validation
export type PasswordStrength = 'weak' | 'medium' | 'strong'

export interface PasswordValidation {
  isValid: boolean
  strength: PasswordStrength
  errors: string[]
  checks: {
    minLength: boolean
    hasUppercase: boolean
    hasLowercase: boolean
    hasNumber: boolean
    hasSpecial: boolean
  }
}
