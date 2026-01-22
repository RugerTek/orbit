import type {
  UserProfile,
  UpdateProfileRequest,
  ChangePasswordRequest,
  PasswordValidation,
  PasswordStrength
} from '~/types/user'

export const useUserProfile = () => {
  const config = useRuntimeConfig()
  const { authToken } = useAuth()

  const profile = useState<UserProfile | null>('user-profile', () => null)
  const isLoading = useState<boolean>('user-profile-loading', () => false)
  const error = useState<string | null>('user-profile-error', () => null)

  const baseUrl = `${config.public.apiBaseUrl}/api/Auth`

  const getAuthHeaders = () => {
    if (!authToken.value) {
      throw new Error('Not authenticated')
    }
    return {
      Authorization: `Bearer ${authToken.value}`
    }
  }

  const fetchProfile = async (): Promise<UserProfile | null> => {
    isLoading.value = true
    error.value = null

    try {
      const data = await $fetch<UserProfile>(`${baseUrl}/profile`, {
        headers: getAuthHeaders()
      })
      profile.value = data
      return data
    } catch (e: unknown) {
      const errorMessage = extractErrorMessage(e)
      error.value = errorMessage
      console.error('Failed to fetch profile:', e)
      return null
    } finally {
      isLoading.value = false
    }
  }

  const updateProfile = async (request: UpdateProfileRequest): Promise<UserProfile | null> => {
    isLoading.value = true
    error.value = null

    try {
      const data = await $fetch<UserProfile>(`${baseUrl}/profile`, {
        method: 'PUT',
        headers: getAuthHeaders(),
        body: request
      })
      profile.value = data

      // Update localStorage user data to reflect changes
      if (import.meta.client) {
        const storedUser = localStorage.getItem('orbitos-user')
        if (storedUser) {
          const user = JSON.parse(storedUser)
          user.displayName = data.displayName
          localStorage.setItem('orbitos-user', JSON.stringify(user))
        }
      }

      return data
    } catch (e: unknown) {
      const errorMessage = extractErrorMessage(e)
      error.value = errorMessage
      throw new Error(errorMessage)
    } finally {
      isLoading.value = false
    }
  }

  const changePassword = async (request: ChangePasswordRequest): Promise<void> => {
    isLoading.value = true
    error.value = null

    try {
      await $fetch(`${baseUrl}/change-password`, {
        method: 'PUT',
        headers: getAuthHeaders(),
        body: request
      })
    } catch (e: unknown) {
      const errorMessage = extractErrorMessage(e)
      error.value = errorMessage
      throw new Error(errorMessage)
    } finally {
      isLoading.value = false
    }
  }

  const linkGoogleAccount = async (credential: string): Promise<UserProfile | null> => {
    isLoading.value = true
    error.value = null

    try {
      const data = await $fetch<UserProfile>(`${baseUrl}/link-google`, {
        method: 'POST',
        headers: getAuthHeaders(),
        body: { credential }
      })
      profile.value = data
      return data
    } catch (e: unknown) {
      const errorMessage = extractErrorMessage(e)
      error.value = errorMessage
      throw new Error(errorMessage)
    } finally {
      isLoading.value = false
    }
  }

  const clearError = () => {
    error.value = null
  }

  // Computed helpers
  const fullName = computed(() => {
    if (!profile.value) return ''
    if (profile.value.firstName && profile.value.lastName) {
      return `${profile.value.firstName} ${profile.value.lastName}`
    }
    return profile.value.displayName
  })

  const initials = computed(() => {
    if (!profile.value) return '?'
    if (profile.value.firstName && profile.value.lastName) {
      return `${profile.value.firstName[0]}${profile.value.lastName[0]}`.toUpperCase()
    }
    return profile.value.displayName.charAt(0).toUpperCase()
  })

  const authMethods = computed(() => {
    if (!profile.value) return []
    const methods: string[] = []
    if (profile.value.hasPassword) methods.push('password')
    if (profile.value.hasGoogleId) methods.push('google')
    if (profile.value.hasAzureAdId) methods.push('microsoft')
    return methods
  })

  return {
    profile: readonly(profile),
    isLoading: readonly(isLoading),
    error: readonly(error),
    fullName,
    initials,
    authMethods,
    fetchProfile,
    updateProfile,
    changePassword,
    linkGoogleAccount,
    clearError
  }
}

// Password validation utility
export const validatePassword = (password: string): PasswordValidation => {
  const checks = {
    minLength: password.length >= 8,
    hasUppercase: /[A-Z]/.test(password),
    hasLowercase: /[a-z]/.test(password),
    hasNumber: /[0-9]/.test(password),
    hasSpecial: /[!@#$%^&*(),.?":{}|<>]/.test(password)
  }

  const errors: string[] = []
  if (!checks.minLength) errors.push('At least 8 characters')
  if (!checks.hasUppercase) errors.push('At least one uppercase letter')
  if (!checks.hasLowercase) errors.push('At least one lowercase letter')
  if (!checks.hasNumber) errors.push('At least one number')

  const passedChecks = Object.values(checks).filter(Boolean).length
  let strength: PasswordStrength = 'weak'
  if (passedChecks >= 4) strength = 'strong'
  else if (passedChecks >= 3) strength = 'medium'

  const isValid = checks.minLength && checks.hasUppercase && checks.hasLowercase && checks.hasNumber

  return {
    isValid,
    strength,
    errors,
    checks
  }
}

// Helper to extract error message from API response
function extractErrorMessage(e: unknown): string {
  if (e && typeof e === 'object') {
    if ('data' in e) {
      const data = (e as { data?: { Message?: string; message?: string } }).data
      if (data?.Message) return data.Message
      if (data?.message) return data.message
    }
    if ('message' in e && typeof (e as { message: unknown }).message === 'string') {
      return (e as { message: string }).message
    }
  }
  return 'An error occurred'
}
