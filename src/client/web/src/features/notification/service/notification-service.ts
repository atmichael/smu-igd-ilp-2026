import { apiFetch } from '../../../common/service/api-client'

export interface DocumentNotification {
  documentType?: string | null
  documentReferenceNumber?: string | null
  companyUen?: string | null
}

export function formatDocumentNotification(notification: DocumentNotification): string {
  const value = (text?: string | null) => (text && text.trim() ? text.trim() : '-')
  return `Received new ${value(notification.documentType)}: ${value(notification.documentReferenceNumber)} from ${value(notification.companyUen)}`
}

export function parseNotificationEvent(block: string): DocumentNotification | null {
  const data = block
    .split('\n')
    .filter((line) => line.startsWith('data:'))
    .map((line) => line.slice(5).trim())
    .join('\n')
  if (!data) return null

  try {
    return JSON.parse(data) as DocumentNotification
  } catch {
    return null
  }
}

/** Streams server-sent events until the signal aborts, reconnecting after failures. */
export async function streamDocumentNotifications(
  onNotification: (notification: DocumentNotification) => void,
  signal: AbortSignal,
  retryDelayMs = 5000,
): Promise<void> {
  while (!signal.aborted) {
    try {
      const response = await apiFetch('/api/notifications/stream', {
        headers: { Accept: 'text/event-stream' },
        signal,
      })
      if (!response.ok || !response.body) throw new Error('Notification stream unavailable.')

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      for (;;) {
        const { done, value } = await reader.read()
        if (done) break
        buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n')
        let separator = buffer.indexOf('\n\n')
        while (separator >= 0) {
          const notification = parseNotificationEvent(buffer.slice(0, separator))
          buffer = buffer.slice(separator + 2)
          if (notification) onNotification(notification)
          separator = buffer.indexOf('\n\n')
        }
      }
    } catch {
      if (signal.aborted) return
    }

    await new Promise<void>((resolve) => {
      const timer = setTimeout(resolve, retryDelayMs)
      signal.addEventListener('abort', () => { clearTimeout(timer); resolve() }, { once: true })
    })
  }
}
