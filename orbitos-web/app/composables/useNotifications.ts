import * as signalR from '@microsoft/signalr'
import { useAuth } from '~/composables/useAuth'
import { useOrganizations } from '~/composables/useOrganizations'

export interface NewMessageNotification {
  conversationId: string
  messageId: string
  senderType: 'user' | 'ai'
  senderName: string
  senderAvatarColor: string | null
  contentPreview: string
  sequenceNumber: number
  createdAt: string
}

export interface UnreadCountNotification {
  conversationId: string
  unreadCount: number
}

export interface MessageDto {
  id: string
  conversationId: string
  senderType: 'user' | 'ai'
  content: string
  contentHtml?: string
  tokensUsed?: number
  responseTimeMs?: number
  cost?: number
  modelUsed?: string
  status: string
  sequenceNumber: number
  createdAt: string
  senderUser?: {
    id: string
    displayName: string
    email: string
    avatarUrl?: string
  }
  senderAiAgent?: {
    id: string
    name: string
    roleTitle: string
    avatarColor?: string
    provider: string
    modelDisplayName: string
  }
  mentionedAgentIds?: string[]
}

// Global state (singleton)
const connection = ref<signalR.HubConnection | null>(null)
const isConnected = ref(false)
const isConnecting = ref(false)
const connectionError = ref<string | null>(null)

// Notification state
const notifications = ref<NewMessageNotification[]>([])
const unreadCounts = ref<Map<string, number>>(new Map())
const totalUnreadCount = computed(() => {
  let total = 0
  unreadCounts.value.forEach(count => total += count)
  return total
})

// Event callbacks
const messageHandlers = new Map<string, Set<(message: MessageDto) => void>>()
const notificationHandlers = new Set<(notification: NewMessageNotification) => void>()

export function useNotifications() {
  const config = useRuntimeConfig()
  const { authToken } = useAuth()
  const { currentOrganizationId } = useOrganizations()

  const connect = async () => {
    if (connection.value?.state === signalR.HubConnectionState.Connected) {
      return
    }

    if (isConnecting.value) {
      return
    }

    isConnecting.value = true
    connectionError.value = null

    try {
      const hubUrl = `${config.public.apiBaseUrl}/hubs/conversations`

      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => authToken.value || '',
          withCredentials: true
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build()

      // Handle connection events
      newConnection.onreconnecting((error) => {
        console.log('SignalR reconnecting...', error)
        isConnected.value = false
      })

      newConnection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId)
        isConnected.value = true
        // Re-join notification channel
        if (currentOrganizationId.value) {
          joinNotifications()
        }
      })

      newConnection.onclose((error) => {
        console.log('SignalR connection closed', error)
        isConnected.value = false
        connection.value = null
      })

      // Handle incoming messages
      newConnection.on('NewMessage', (message: MessageDto) => {
        console.log('Received new message:', message)
        const handlers = messageHandlers.get(message.conversationId)
        if (handlers) {
          handlers.forEach(handler => handler(message))
        }
      })

      // Handle new message notifications (for notification bell)
      newConnection.on('NewMessageNotification', (notification: NewMessageNotification) => {
        console.log('Received notification:', notification)
        notifications.value.unshift(notification)
        // Keep only last 50 notifications
        if (notifications.value.length > 50) {
          notifications.value = notifications.value.slice(0, 50)
        }
        // Update unread count for this conversation
        const currentCount = unreadCounts.value.get(notification.conversationId) || 0
        unreadCounts.value.set(notification.conversationId, currentCount + 1)
        // Notify handlers
        notificationHandlers.forEach(handler => handler(notification))
      })

      // Handle unread count updates
      newConnection.on('UnreadCount', (update: UnreadCountNotification) => {
        console.log('Received unread count update:', update)
        const currentCount = unreadCounts.value.get(update.conversationId) || 0
        unreadCounts.value.set(update.conversationId, currentCount + update.unreadCount)
      })

      await newConnection.start()
      console.log('SignalR connected successfully')

      connection.value = newConnection
      isConnected.value = true

      // Auto-join notification channel if org is set
      if (currentOrganizationId.value) {
        await joinNotifications()
      }
    } catch (error) {
      console.error('SignalR connection failed:', error)
      connectionError.value = error instanceof Error ? error.message : 'Connection failed'
    } finally {
      isConnecting.value = false
    }
  }

  const disconnect = async () => {
    if (connection.value) {
      await connection.value.stop()
      connection.value = null
      isConnected.value = false
    }
  }

  const joinNotifications = async () => {
    if (!connection.value || !currentOrganizationId.value) return
    try {
      await connection.value.invoke('JoinNotifications', currentOrganizationId.value)
      console.log('Joined notification channel for org:', currentOrganizationId.value)
    } catch (error) {
      console.error('Failed to join notifications:', error)
    }
  }

  const leaveNotifications = async () => {
    if (!connection.value || !currentOrganizationId.value) return
    try {
      await connection.value.invoke('LeaveNotifications', currentOrganizationId.value)
    } catch (error) {
      console.error('Failed to leave notifications:', error)
    }
  }

  const joinConversation = async (conversationId: string) => {
    if (!connection.value || !currentOrganizationId.value) {
      console.warn('Cannot join conversation: not connected')
      return
    }
    try {
      await connection.value.invoke('JoinConversation', currentOrganizationId.value, conversationId)
      console.log('Joined conversation:', conversationId)
    } catch (error) {
      console.error('Failed to join conversation:', error)
    }
  }

  const leaveConversation = async (conversationId: string) => {
    if (!connection.value || !currentOrganizationId.value) return
    try {
      await connection.value.invoke('LeaveConversation', currentOrganizationId.value, conversationId)
      console.log('Left conversation:', conversationId)
    } catch (error) {
      console.error('Failed to leave conversation:', error)
    }
  }

  const markAsRead = async (conversationId: string, messageId: string) => {
    if (!connection.value) return
    try {
      await connection.value.invoke('MarkAsRead', conversationId, messageId)
      // Clear unread count for this conversation
      unreadCounts.value.set(conversationId, 0)
    } catch (error) {
      console.error('Failed to mark as read:', error)
    }
  }

  const onMessage = (conversationId: string, handler: (message: MessageDto) => void) => {
    if (!messageHandlers.has(conversationId)) {
      messageHandlers.set(conversationId, new Set())
    }
    messageHandlers.get(conversationId)!.add(handler)

    // Return cleanup function
    return () => {
      messageHandlers.get(conversationId)?.delete(handler)
    }
  }

  const onNotification = (handler: (notification: NewMessageNotification) => void) => {
    notificationHandlers.add(handler)
    return () => {
      notificationHandlers.delete(handler)
    }
  }

  const clearNotifications = () => {
    notifications.value = []
  }

  const clearUnreadCount = (conversationId: string) => {
    unreadCounts.value.set(conversationId, 0)
  }

  return {
    // State
    isConnected: readonly(isConnected),
    isConnecting: readonly(isConnecting),
    connectionError: readonly(connectionError),
    notifications: readonly(notifications),
    unreadCounts: readonly(unreadCounts),
    totalUnreadCount,

    // Actions
    connect,
    disconnect,
    joinConversation,
    leaveConversation,
    markAsRead,
    onMessage,
    onNotification,
    clearNotifications,
    clearUnreadCount,
  }
}
