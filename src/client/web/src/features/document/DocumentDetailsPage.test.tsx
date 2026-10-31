import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DocumentDetailsPage } from './DocumentDetailsPage'
import type { DocumentRecord } from './model/document-model'

const serviceMocks = vi.hoisted(() => ({
  getDocumentById: vi.fn(),
  updateDocument: vi.fn(),
  deleteDocument: vi.fn(),
}))

vi.mock('./service/document-service', () => serviceMocks)

vi.mock('./components/DocumentInfo', () => ({
  DocumentInfo: ({
    document,
    mode,
    onSubmit,
    onSaved,
    onCancel,
  }: {
    document: DocumentRecord
    mode?: 'view' | 'edit'
    onSubmit?: () => Promise<void>
    onSaved?: () => void
    onCancel?: () => void
  }) => (
    <section>
      <p>{mode === 'edit' ? 'Edit mode' : 'Read-only mode'}</p>
      <p>Current ref: {document.refNumber}</p>
      {mode === 'edit' && (
        <>
          <button onClick={onCancel}>Cancel edit</button>
          <button
            onClick={async () => {
              await onSubmit?.()
              onSaved?.()
            }}
          >
            Submit edit
          </button>
        </>
      )}
    </section>
  ),
}))

const record = (refNumber = 'INV-001'): DocumentRecord => ({
  id: 17,
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

describe('DocumentDetailsPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('edits in place, reloads the updated record, and returns to read-only mode', async () => {
    serviceMocks.getDocumentById
      .mockResolvedValueOnce(record())
      .mockResolvedValueOnce(record('INV-002'))
    serviceMocks.updateDocument.mockResolvedValue(undefined)
    const onBack = vi.fn()

    render(<DocumentDetailsPage documentId={17} onBack={onBack} onDeleted={onBack} />)

    expect(await screen.findByText('Read-only mode')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /^edit$/i }))

    expect(screen.getByText('Edit mode')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /submit edit/i }))

    expect(await screen.findByText('Current ref: INV-002')).toBeInTheDocument()
    expect(screen.getByText('Read-only mode')).toBeInTheDocument()
    expect(serviceMocks.updateDocument).toHaveBeenCalledWith(17, undefined)
    await waitFor(() => expect(serviceMocks.getDocumentById).toHaveBeenCalledTimes(2))
    expect(onBack).not.toHaveBeenCalled()
  })

  it('cancels editing without changing details or leaving the page', async () => {
    serviceMocks.getDocumentById.mockResolvedValue(record())
    const onBack = vi.fn()

    render(<DocumentDetailsPage documentId={17} onBack={onBack} onDeleted={onBack} />)
    await screen.findByText('Read-only mode')
    fireEvent.click(screen.getByRole('button', { name: /^edit$/i }))
    fireEvent.click(screen.getByRole('button', { name: /cancel edit/i }))

    expect(screen.getByText('Read-only mode')).toBeInTheDocument()
    expect(screen.getByText('Current ref: INV-001')).toBeInTheDocument()
    expect(onBack).not.toHaveBeenCalled()
  })

  it('prompts before deletion and returns to the source page only after confirmation', async () => {
    serviceMocks.getDocumentById.mockResolvedValue(record())
    serviceMocks.deleteDocument.mockResolvedValue(undefined)
    const onDeleted = vi.fn()

    render(<DocumentDetailsPage documentId={17} onBack={vi.fn()} onDeleted={onDeleted} />)
    await screen.findByText('Read-only mode')
    fireEvent.click(screen.getByRole('button', { name: /^delete$/i }))

    expect(screen.getByText(/are you sure you want to delete document inv-001/i)).toBeInTheDocument()
    expect(onDeleted).not.toHaveBeenCalled()
    fireEvent.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => expect(serviceMocks.deleteDocument).toHaveBeenCalledWith(17))
    expect(onDeleted).toHaveBeenCalledTimes(1)
  })
})
