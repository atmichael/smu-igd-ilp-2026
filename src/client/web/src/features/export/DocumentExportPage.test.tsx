import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DocumentExportPage } from './DocumentExportPage'
import { downloadCsv, getDocumentsForExport } from './service/export-service'

vi.mock('./service/export-service', () => ({
  getDocumentsForExport: vi.fn(),
  downloadCsv: vi.fn(),
}))

const record = {
  id: 1,
  refNumber: 'INV-1',
  typeCode: 'Invoice',
  company: { id: 1, uen: 'U', name: 'Acme' },
  sentDate: '2026-10-04T00:00:00',
  totalAmount: 10,
  subtotal: 10,
  subtotalTaxRate: 0,
  subtotalTaxAmount: 0,
  documentItems: [],
}

describe('DocumentExportPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useRealTimers()
  })

  it('defaults the range to the start of the current month through today in local time', () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 9, 31, 23, 30))
    render(<DocumentExportPage />)

    expect(screen.getByLabelText('Created from')).toHaveValue('2026-10-01')
    expect(screen.getByLabelText('Created to')).toHaveValue('2026-10-31')
  })

  it('places the export button in a separate row from the filters', () => {
    render(<DocumentExportPage />)
    const filters = screen.getByLabelText('Created from').closest('.export-form__filters')

    expect(filters).not.toContainElement(screen.getByRole('button', { name: /export to xero/i }))
  })

  it('retrieves documents for the date range and downloads a Xero CSV', async () => {
    vi.mocked(getDocumentsForExport).mockResolvedValue({ items: [record], totalCount: 1 })
    render(<DocumentExportPage />)

    fireEvent.change(screen.getByLabelText('Created from'), { target: { value: '2026-10-01' } })
    fireEvent.change(screen.getByLabelText('Created to'), { target: { value: '2026-10-31' } })
    fireEvent.click(screen.getByRole('button', { name: /export to xero/i }))

    await waitFor(() => expect(downloadCsv).toHaveBeenCalled())
    expect(getDocumentsForExport).toHaveBeenCalledWith({ createdFrom: '2026-10-01', createdTo: '2026-10-31' })
    expect(vi.mocked(downloadCsv).mock.calls[0][0]).toMatch(/^xero-bills-.*\.csv$/)
    expect(vi.mocked(downloadCsv).mock.calls[0][1]).toContain('INV-1')
    expect(await screen.findByText(/exported 1 document/i)).toBeInTheDocument()
  })

  it('reports when nothing matches or the request fails', async () => {
    vi.mocked(getDocumentsForExport).mockResolvedValueOnce({ items: [], totalCount: 0 })
    render(<DocumentExportPage />)
    fireEvent.click(screen.getByRole('button', { name: /export to xero/i }))
    expect(await screen.findByText(/no documents were found/i)).toBeInTheDocument()

    vi.mocked(getDocumentsForExport).mockRejectedValueOnce(new Error('boom'))
    fireEvent.click(screen.getByRole('button', { name: /export to xero/i }))
    expect(await screen.findByText('boom')).toBeInTheDocument()
    expect(downloadCsv).not.toHaveBeenCalled()
  })

  it('blocks export when the range is inverted', () => {
    render(<DocumentExportPage />)
    fireEvent.change(screen.getByLabelText('Created from'), { target: { value: '2026-11-01' } })
    fireEvent.change(screen.getByLabelText('Created to'), { target: { value: '2026-10-01' } })

    expect(screen.getByRole('button', { name: /export to xero/i })).toBeDisabled()
  })
})
