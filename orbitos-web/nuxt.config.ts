// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss'],
  devServer: {
    host: '0.0.0.0',
    port: 3000,
  },

  runtimeConfig: {
    public: {
      msalClientId: process.env.NUXT_PUBLIC_MSAL_CLIENT_ID || '',
      msalAuthority: process.env.NUXT_PUBLIC_MSAL_AUTHORITY || '',
      msalRedirectUri: process.env.NUXT_PUBLIC_MSAL_REDIRECT_URI || 'http://localhost:3000',
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'http://localhost:5027',
      apiScopes: process.env.NUXT_PUBLIC_API_SCOPES || '',
      googleClientId: process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID || '',  // Required: Set NUXT_PUBLIC_GOOGLE_CLIENT_ID env var
    }
  },

  app: {
    head: {
      title: 'OrbitOS',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        { name: 'description', content: 'OrbitOS - Operations Management Platform' }
      ]
    }
  },

  typescript: {
    strict: true
  }
})
