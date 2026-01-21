import {
  PublicClientApplication,
  type AccountInfo,
  type AuthenticationResult,
  InteractionRequiredAuthError,
} from '@azure/msal-browser'
import { createMsalConfig, createLoginRequest, createSilentRequest } from '~/utils/msalConfig'

let msalInstance: PublicClientApplication | null = null

interface LocalUser {
  email: string
  displayName: string
  token: string
}

export const useAuth = () => {
  const config = useRuntimeConfig()
  const user = useState<AccountInfo | LocalUser | null>('auth-user', () => null)
  const authToken = useState<string | null>('auth-token', () => null)
  const isAuthenticated = computed(() => !!user.value)
  const isLoading = useState<boolean>('auth-loading', () => true)

  const getMsalInstance = async (): Promise<PublicClientApplication | null> => {
    if (msalInstance) return msalInstance

    // Don't initialize MSAL if not configured
    const msalClientId = config.public.msalClientId
    if (!msalClientId || msalClientId === 'your-client-id' || msalClientId === '') {
      return null
    }

    const msalConfig = createMsalConfig(
      msalClientId,
      config.public.msalAuthority,
      config.public.msalRedirectUri
    )

    msalInstance = new PublicClientApplication(msalConfig)
    await msalInstance.initialize()
    return msalInstance
  }

  const initializeAuth = async () => {
    try {
      isLoading.value = true

      // Check for stored local auth token first
      if (import.meta.client) {
        const storedToken = localStorage.getItem('orbitos-token')
        const storedUser = localStorage.getItem('orbitos-user')
        if (storedToken && storedUser) {
          authToken.value = storedToken
          user.value = JSON.parse(storedUser)
          return
        }
      }

      // Only try MSAL if Azure AD is configured (clientId is not empty/placeholder)
      const msalClientId = config.public.msalClientId
      if (!msalClientId || msalClientId === 'your-client-id' || msalClientId === '') {
        // MSAL not configured, skip Azure AD auth
        return
      }

      // Then check MSAL
      const instance = await getMsalInstance()
      if (!instance) {
        return
      }

      // Handle redirect callback
      const response = await instance.handleRedirectPromise()
      if (response?.account) {
        user.value = response.account
        return
      }

      // Check for existing accounts
      const accounts = instance.getAllAccounts()
      if (accounts.length > 0) {
        user.value = accounts[0]
      }
    } catch (error) {
      console.error('Auth initialization error:', error)
    } finally {
      isLoading.value = false
    }
  }

  const loginWithEmail = async (email: string, password: string): Promise<void> => {
    try {
      const response = await $fetch<{ token: string; email: string; displayName: string }>(`${config.public.apiBaseUrl}/api/Auth/login`, {
        method: 'POST',
        body: { email, password }
      })

      const localUser: LocalUser = {
        email: response.email,
        displayName: response.displayName,
        token: response.token
      }

      user.value = localUser
      authToken.value = response.token

      // Store in localStorage
      if (import.meta.client) {
        localStorage.setItem('orbitos-token', response.token)
        localStorage.setItem('orbitos-user', JSON.stringify(localUser))
      }
    } catch (error: unknown) {
      console.error('Login error:', error)
      throw error
    }
  }

  const loginWithGoogle = async (credential: string): Promise<void> => {
    try {
      const response = await $fetch<{ token: string; email: string; displayName: string }>(`${config.public.apiBaseUrl}/api/Auth/google`, {
        method: 'POST',
        body: { credential }
      })

      const localUser: LocalUser = {
        email: response.email,
        displayName: response.displayName,
        token: response.token
      }

      user.value = localUser
      authToken.value = response.token

      // Store in localStorage
      if (import.meta.client) {
        localStorage.setItem('orbitos-token', response.token)
        localStorage.setItem('orbitos-user', JSON.stringify(localUser))
      }
    } catch (error: unknown) {
      console.error('Google login error:', error)
      throw error
    }
  }

  const loginWithGoogleCode = async (code: string): Promise<void> => {
    try {
      const response = await $fetch<{ token: string; email: string; displayName: string }>(`${config.public.apiBaseUrl}/api/Auth/google-code`, {
        method: 'POST',
        body: { code, redirectUri: window.location.origin }
      })

      const localUser: LocalUser = {
        email: response.email,
        displayName: response.displayName,
        token: response.token
      }

      user.value = localUser
      authToken.value = response.token

      // Store in localStorage
      if (import.meta.client) {
        localStorage.setItem('orbitos-token', response.token)
        localStorage.setItem('orbitos-user', JSON.stringify(localUser))
      }
    } catch (error: unknown) {
      console.error('Google code login error:', error)
      throw error
    }
  }

  const login = async (): Promise<void> => {
    try {
      const instance = await getMsalInstance()
      if (!instance) {
        throw new Error('Azure AD authentication is not configured')
      }
      const scopes = config.public.apiScopes ? [config.public.apiScopes] : ['User.Read']
      const loginRequest = createLoginRequest(scopes)

      const response: AuthenticationResult = await instance.loginPopup(loginRequest)
      user.value = response.account
    } catch (error) {
      console.error('Login error:', error)
      throw error
    }
  }

  const logout = async (): Promise<void> => {
    try {
      // Clear local storage
      if (import.meta.client) {
        localStorage.removeItem('orbitos-token')
        localStorage.removeItem('orbitos-user')
        localStorage.removeItem('currentOrganizationId')
      }

      // If using MSAL, logout from there too
      if (user.value && 'homeAccountId' in user.value) {
        const instance = await getMsalInstance()
        if (instance) {
          await instance.logoutPopup({
            account: user.value as AccountInfo,
            postLogoutRedirectUri: config.public.msalRedirectUri,
          })
        }
      }

      user.value = null
      authToken.value = null

      // Redirect to home page after logout
      if (import.meta.client) {
        window.location.href = '/'
      }
    } catch (error) {
      console.error('Logout error:', error)
      throw error
    }
  }

  const getAccessToken = async (): Promise<string | null> => {
    try {
      if (!user.value) {
        return null
      }

      // If using local JWT auth (email/password or Google), return the stored token
      // Local users have 'token' property, MSAL users have 'homeAccountId'
      if ('token' in user.value) {
        return authToken.value
      }

      // Only use MSAL for Azure AD users (they have homeAccountId)
      if (!('homeAccountId' in user.value)) {
        return null
      }

      const instance = await getMsalInstance()
      if (!instance) {
        return null
      }

      const scopes = config.public.apiScopes ? [config.public.apiScopes] : ['User.Read']
      const silentRequest = createSilentRequest(scopes, user.value)

      try {
        const response = await instance.acquireTokenSilent(silentRequest)
        return response.accessToken
      } catch (error) {
        if (error instanceof InteractionRequiredAuthError) {
          // Token expired or needs interaction, trigger popup
          const loginRequest = createLoginRequest(scopes)
          const response = await instance.acquireTokenPopup(loginRequest)
          user.value = response.account
          return response.accessToken
        }
        throw error
      }
    } catch (error) {
      console.error('Get access token error:', error)
      return null
    }
  }

  return {
    user: readonly(user),
    isAuthenticated,
    isLoading: readonly(isLoading),
    authToken: readonly(authToken),
    initializeAuth,
    login,
    loginWithEmail,
    loginWithGoogle,
    loginWithGoogleCode,
    logout,
    getAccessToken,
  }
}
