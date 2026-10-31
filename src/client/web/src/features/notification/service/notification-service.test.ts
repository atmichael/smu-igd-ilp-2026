import { describe, expect, it, vi } from 'vitest'
import {
  formatDocumentNotification,
  parseNotificationEvent,
  streamDocumentNotifications,
} from './notification-service'

describe('notification-service', () => {
  it('formats the notification message', () => {
    expect(
      formatDocumentNotification({ documentType: 'Invoice', documentReferenceNumber: 'INV-1', companyUen: 'UEN-1' }),
    ).toBe('Received new Invoice: INV-1 from UEN-1')
  })

  it('defaults missing parts to "-"', () => {
    expect(formatDocumentNotification({ documentType: 'Invoice', documentReferenceNumber: ' ', companyUen: null })).toBe(
      'Received new Invoice: - from -',
    )
    expect(formatDocumentNotification({})).toBe('Received new -: - from -')
  })

  it('parses data events and ignores comments', () => {
    expect(parseNotificationEvent(': connected')).toBeNull()
    expect(parseNotificationEvent('data: {"documentType":"Bill"}')).toEqual({ documentType: 'Bill' })
    expect(parseNotificationEvent('data: not-json')).toBeNull()
  })

  it('streams notifications from the server-sent events endpoint', async () => {
    const encoder = new TextEncoder()
    const body = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(encoder.encode(': connected\n\ndata: {"documentType":"Invoice",'))
        controller.enqueue(encoder.encode('"documentReferenceNumber":"INV-1","companyUen":"U1"}\n\n'))
        controller.close()
      },
    })
    const abort = new AbortController()
    vi.stubGlobal('fetch', vi.fn(async () => new Response(body, { status: 200 })))
    const received: unknown[] = []

    await streamDocumentNotifications(
      (notification) => {
        received.push(notification)
        abort.abort()
      },
      abort.signal,
      1,
    )

    expect(received).toEqual([{ documentType: 'Invoice', documentReferenceNumber: 'INV-1', companyUen: 'U1' }])
    vi.unstubAllGlobals()
  })
})
