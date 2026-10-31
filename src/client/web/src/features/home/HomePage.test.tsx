import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { HomePage } from './HomePage'
import type { DocumentRecord } from '../document/model/document-model'

const pageMocks = vi.hoisted(() => ({
  getDocumentsBelowThreshold: vi.fn(),
}))

vi.mock('../document/service/document-service', () => ({
  deleteDocument: vi.fn(),
  getDocumentsBelowThreshold: pageMocks.getDocumentsBelowThreshold,
}))

vi.mock('../document/components/DocumentDataGrid', () => ({
  DocumentDataGrid: ({
    rows,
    onEdit,
  }: {
    rows: DocumentRecord[]
    onEdit: (document: DocumentRecord) => void
  }) => (
    <div>
      {rows.map((document) => (
        <div key={document.id}>
          <span>{document.refNumber}</span>
          <button onClick={() => onEdit(document)}>Edit {document.refNumber}</button>
        </div>
      ))}
    </div>
  ),
}))

vi.mock('../document/components/DocumentEditDialog', () => ({
  DocumentEditDialog: ({
    document,
    onSaved,
  }: {
    document: DocumentRecord | null
    onSaved: () => void
  }) =>
    document ? (
      <div role="dialog">
        <span>Editing {document.refNumber}</span>
        <button onClick={onSaved}>Save edits</button>
      </div>
    ) : null,
}))

const documentRow = (refNumber: string): DocumentRecord => ({
  id: 1,
  refNumber,
  typeCode: 'INV',
  company: { id: 1, uen: '111A', name: 'Alpha' },
  sentDate: '2026-10-01',
  totalAmount: 10,
  subtotal: 10,
  subtotalTaxRate: 0,
  subtotalTaxAmount: 0,
  documentItems: [],
})

describe('HomePage document editing', () => {
  it('stays on the landing page and refreshes the list after saving', async () => {
    pageMocks.getDocumentsBelowThreshold
      .mockResolvedValueOnce({ items: [documentRow('INV-001')], totalCount: 1 })
      .mockResolvedValueOnce({ items: [documentRow('INV-002')], totalCount: 1 })
    const onNavigate = vi.fn()

    render(<HomePage onNavigate={onNavigate} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Edit INV-001' }))
    fireEvent.click(screen.getByRole('button', { name: 'Save edits' }))

    expect(await screen.findByText('INV-002')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Manual rectification' })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(onNavigate).not.toHaveBeenCalled()
    await waitFor(() => expect(pageMocks.getDocumentsBelowThreshold).toHaveBeenCalledTimes(2))
  })
})
