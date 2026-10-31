import { act, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DocumentNotifications } from './DocumentNotifications'

const notificationTest = vi.hoisted(() => ({
  onNotification: null as null | ((notification: { documentType?: string; documentReferenceNumber?: string; companyUen?: string }) => void),
}))

vi.mock('../service/notification-service', () => ({
  formatDocumentNotification: (notification: { documentType?: string; documentReferenceNumber?: string; companyUen?: string }) =>
    `Received new ${notification.documentType}: ${notification.documentReferenceNumber} from ${notification.companyUen}`,
  streamDocumentNotifications: (onNotification: (notification: { documentType?: string; documentReferenceNumber?: string; companyUen?: string }) => void) => {
    notificationTest.onNotification = onNotification
    return Promise.resolve()
  },
}))

describe('DocumentNotifications', () => {
  beforeEach(() => {
    notificationTest.onNotification = null
  })

  it('shows new-document notifications at the bottom left with a bounded width', () => {
    render(<DocumentNotifications />)

    act(() => {
      notificationTest.onNotification?.({
        documentType: 'invoice',
        documentReferenceNumber: 'INV-42',
        companyUen: '12345678A',
      })
    })

    const alert = screen.getByRole('alert')
    const snackbar = alert.closest('.MuiSnackbar-root')

    expect(alert).toHaveTextContent('Received new invoice: INV-42 from 12345678A')
    expect(snackbar).toHaveClass('MuiSnackbar-anchorOriginBottomLeft')
    expect(alert).toHaveStyle({ width: 'min(60vw, 400px)', minWidth: 'min(250px, calc(100vw - 32px))' })
  })
})
