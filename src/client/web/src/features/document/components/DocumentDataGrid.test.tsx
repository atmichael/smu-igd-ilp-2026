import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { DocumentDataGrid } from './DocumentDataGrid'
import type { DocumentRecord } from '../model/document-model'

const documents: DocumentRecord[] = [
  {
    id: 1,
    refNumber: 'INV-001',
    typeCode: 'INV',
    company: { id: 1, uen: '111111111A', name: 'Alpha Ltd' },
    sentDate: '2026-10-01',
    dueDate: '2026-10-31',
    totalAmount: 100,
    confidence: 0.42,
    subtotal: 100,
    subtotalTaxRate: 0,
    subtotalTaxAmount: 0,
    documentItems: [],
    apJournal: {
      confidence: 0.85,
      gstCode: 'SG_STD_9',
      coaSuggestions: [],
      verificationNotes: [],
    },
  },
  {
    id: 2,
    refNumber: 'INV-002',
    typeCode: 'INV',
    company: { id: 2, uen: '222222222B', name: 'Beta Ltd' },
    sentDate: '2026-10-02',
    dueDate: '2026-11-01',
    totalAmount: 200,
    confidence: 0.9,
    subtotal: 200,
    subtotalTaxRate: 0,
    subtotalTaxAmount: 0,
    documentItems: [],
  },
]

describe('DocumentDataGrid', () => {
  it('filters by company UEN and exposes recognizable row actions', () => {
    render(
      <DocumentDataGrid rows={documents} onView={vi.fn()} onEdit={vi.fn()} onDelete={vi.fn()} />,
    )

    fireEvent.change(screen.getByRole('textbox', { name: /filter by company uen/i }), {
      target: { value: '2222' },
    })

    expect(screen.getByText('INV-002')).toBeInTheDocument()
    expect(screen.queryByText('INV-001')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /view inv-002/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /edit inv-002/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /delete inv-002/i })).toBeInTheDocument()
  })

  it('labels every document field for the mobile card layout', () => {
    render(<DocumentDataGrid rows={documents} onView={vi.fn()} onEdit={vi.fn()} onDelete={vi.fn()} />)

    const row = screen.getByText('INV-001').closest('tr')
    expect(Array.from(row?.querySelectorAll('[data-label]') ?? []).map((cell) => cell.getAttribute('data-label'))).toEqual([
      'Document Number',
      'Company UEN & Name',
      'Document Type',
      'Posting Date',
      'Due Date',
      'Total Amount',
      'Confidence Level',
      'AP Confidence',
      'Actions',
    ])
  })

  it('displays document type and AP confidence when available', () => {
    render(<DocumentDataGrid rows={documents} onView={vi.fn()} onEdit={vi.fn()} onDelete={vi.fn()} />)

    const firstRow = screen.getByText('INV-001').closest('tr')
    const secondRow = screen.getByText('INV-002').closest('tr')
    expect(firstRow).toHaveTextContent('INV')
    expect(firstRow).toHaveTextContent('85%')
    expect(secondRow).toHaveTextContent('INV')
    expect(secondRow).toHaveTextContent('—')
  })

  it('only deletes after confirmation and closes the dialog on success', async () => {
    const onDelete = vi.fn().mockResolvedValue(undefined)
    render(<DocumentDataGrid rows={documents} onView={vi.fn()} onEdit={vi.fn()} onDelete={onDelete} />)

    fireEvent.click(screen.getByRole('button', { name: /delete inv-001/i }))
    fireEvent.click(screen.getByRole('button', { name: /cancel/i }))
    expect(onDelete).not.toHaveBeenCalled()
    await waitFor(() => expect(screen.queryByText(/are you sure/i)).not.toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: /delete inv-001/i }))
    fireEvent.click(screen.getByRole('button', { name: /confirm/i }))
    await waitFor(() => expect(onDelete).toHaveBeenCalledWith(documents[0]))
    await waitFor(() => expect(screen.queryByText(/are you sure/i)).not.toBeInTheDocument())
  })

  it('keeps the dialog open and shows the error when deletion fails', async () => {
    const onDelete = vi.fn().mockRejectedValue(new Error('boom'))
    render(<DocumentDataGrid rows={documents} onView={vi.fn()} onEdit={vi.fn()} onDelete={onDelete} />)

    fireEvent.click(screen.getByRole('button', { name: /delete inv-001/i }))
    fireEvent.click(screen.getByRole('button', { name: /confirm/i }))
    expect(await screen.findByText('boom')).toBeInTheDocument()
  })
})
