import { useAuth } from '~/composables/useAuth'

export const useApi = () => {
  const config = useRuntimeConfig()
  const { getAccessToken, authToken, isAuthenticated } = useAuth()

  const apiFetch = async <T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> => {
    // Use local auth token if available, otherwise try MSAL token
    const token = authToken.value || await getAccessToken()

    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...(options.headers || {}),
    }

    if (token) {
      (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
    }

    const response = await fetch(`${config.public.apiBaseUrl}${endpoint}`, {
      ...options,
      headers,
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(`API Error: ${response.status} - ${error}`)
    }

    // Handle empty responses (204 No Content)
    if (response.status === 204 || response.headers.get('content-length') === '0') {
      return undefined as T
    }

    const text = await response.text()
    return text ? JSON.parse(text) : undefined as T
  }

  const get = <T>(endpoint: string) => apiFetch<T>(endpoint, { method: 'GET' })

  const post = <T>(endpoint: string, data: unknown) =>
    apiFetch<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    })

  const put = <T>(endpoint: string, data: unknown) =>
    apiFetch<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    })

  const patch = <T>(endpoint: string, data: unknown) =>
    apiFetch<T>(endpoint, {
      method: 'PATCH',
      body: JSON.stringify(data),
    })

  const del = <T>(endpoint: string) => apiFetch<T>(endpoint, { method: 'DELETE' })

  return {
    apiFetch,
    get,
    post,
    put,
    patch,
    delete: del,
    isAuthenticated,
  }
}
