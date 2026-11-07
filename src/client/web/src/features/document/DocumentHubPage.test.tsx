import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { DocumentHubPage } from './DocumentHubPage'
import type { DocumentRecord } from './model/document-model'

const pageMocks = vi.hoisted(() => ({
  searchDocuments: vi.fn(),
}))

vi.mock('./service/document-service', () => ({
  deleteDocument: vi.fn(),
  searchDocuments: pageMocks.searchDocuments,
}))

vi.mock('./components/DocumentDataGrid', () => ({
  DocumentDataGrid: ({
    rows,
    onEdit,
    onView,
    serverPaging,
  }: {
    serverPaging: {
      onPageChange: (page: number) => void
      onRowsPerPageChange: (rowsPerPage: number) => void
      onUenFilterChange: (uen: string) => void
    }
    rows: DocumentRecord[]
    onEdit: (document: DocumentRecord) => void
    onView: (document: DocumentRecord) => void
  }) => (
    <div>
      <button onClick={() => serverPaging.onPageChange(1)}>Next page</button>
      <button onClick={() => serverPaging.onRowsPerPageChange(50)}>Show 50</button>
      <button onClick={() => serverPaging.onUenFilterChange('201912345A')}>Filter UEN</button>
      {rows.map((document) => (
        <div key={document.id}>
          <span>{document.refNumber}</span>
          <button onClick={() => onView(document)}>View {document.refNumber}</button>
          <button onClick={() => onEdit(document)}>Edit {document.refNumber}</button>
        </div>
      ))}
    </div>
  ),
}))

vi.mock('./components/DocumentEditDialog', () => ({
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

describe('DocumentHubPage document editing', () => {
  it('stays in the document hub and refreshes the current search after saving', async () => {
    pageMocks.searchDocuments
      .mockResolvedValueOnce({ items: [documentRow('INV-001')], totalCount: 1 })
      .mockResolvedValueOnce({ items: [documentRow('INV-002')], totalCount: 1 })
    const onNavigate = vi.fn()

    render(<DocumentHubPage onNavigate={onNavigate} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Edit INV-001' }))
    fireEvent.click(screen.getByRole('button', { name: 'Save edits' }))

    expect(await screen.findByText('INV-002')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Document hub' })).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: /search by document number/i })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(onNavigate).not.toHaveBeenCalled()
    await waitFor(() => expect(pageMocks.searchDocuments).toHaveBeenCalledTimes(2))
  })

  it('marks the Document Hub as the source when opening document details', async () => {
    pageMocks.searchDocuments.mockResolvedValueOnce({ items: [documentRow('INV-001')], totalCount: 1 })
    const onNavigate = vi.fn()

    render(<DocumentHubPage onNavigate={onNavigate} />)

    fireEvent.click(await screen.findByRole('button', { name: 'View INV-001' }))

    expect(onNavigate).toHaveBeenCalledWith('/document/1?returnTo=%2Fdocument')
  })

  it('requests server-side pages, page sizes and UEN filters', async () => {
    pageMocks.searchDocuments.mockReset()
    pageMocks.searchDocuments.mockResolvedValue({ items: [documentRow('INV-001')], totalCount: 60 })

    render(<DocumentHubPage onNavigate={vi.fn()} />)
    await screen.findByText('INV-001')
    expect(pageMocks.searchDocuments).toHaveBeenLastCalledWith('', { page: 1, pageSize: 25 })

    fireEvent.click(screen.getByRole('button', { name: 'Next page' }))
    await waitFor(() =>
      expect(pageMocks.searchDocuments).toHaveBeenLastCalledWith('', { page: 2, pageSize: 25 }))

    fireEvent.click(screen.getByRole('button', { name: 'Show 50' }))
    await waitFor(() =>
      expect(pageMocks.searchDocuments).toHaveBeenLastCalledWith('', { page: 2, pageSize: 50 }))

    fireEvent.click(screen.getByRole('button', { name: 'Filter UEN' }))
    await waitFor(() =>
      expect(pageMocks.searchDocuments).toHaveBeenLastCalledWith('201912345A', { page: 1, pageSize: 50 }))
  })
})
