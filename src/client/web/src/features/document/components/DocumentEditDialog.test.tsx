import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DocumentEditDialog } from './DocumentEditDialog'
import { getDocumentById } from '../service/document-service'
import type { DocumentRecord } from '../model/document-model'

vi.mock('../service/document-service', () => ({ getDocumentById: vi.fn(), updateDocument: vi.fn() }))
vi.mock('../../company/service/company-service', () => ({ findCompaniesByUen: vi.fn().mockResolvedValue([]) }))

const row: DocumentRecord = {
  id: 1, refNumber: 'INV-001', typeCode: 'INV', company: { id: 1, uen: '111A', name: 'Alpha' },
  sentDate: '2026-10-01', totalAmount: 0, subtotal: 0, subtotalTaxRate: 0, subtotalTaxAmount: 0, documentItems: [],
}

describe('DocumentEditDialog', () => {
  beforeEach(() => vi.clearAllMocks())

  it('loads the full record so the original attachment is previewed, and closes only via Cancel', async () => {
    vi.mocked(getDocumentById).mockResolvedValue({
      ...row, originalFileUrl: 'https://example.com/a.png', originalFileContentType: 'image/png',
    })
    const onClose = vi.fn()
    render(<DocumentEditDialog document={row} onClose={onClose} onSaved={vi.fn()} />)

    expect(await screen.findByAltText('Original document attachment')).toBeInTheDocument()
    expect(getDocumentById).toHaveBeenCalledWith(1)

    const backdrop = document.querySelector('.MuiBackdrop-root') as HTMLElement
    fireEvent.click(backdrop)
    expect(onClose).not.toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: /^cancel$/i }))
    await waitFor(() => expect(onClose).toHaveBeenCalled())
  })
})
