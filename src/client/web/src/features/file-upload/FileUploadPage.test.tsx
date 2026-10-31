import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { FileUploadPage } from './FileUploadPage'

const { submitSourceDocument, fetchOriginalFile } = vi.hoisted(() => ({
  submitSourceDocument: vi.fn(),
  fetchOriginalFile: vi.fn(),
}))

vi.mock('../source-documents/service/source-document-service', () => ({ submitSourceDocument, fetchOriginalFile }))

describe('FileUploadPage', () => {
  it('returns to file selection after the user leaves the submission confirmation', async () => {
    submitSourceDocument.mockResolvedValue({
      sourceDocumentId: 'source-doc-1',
      channel: 'file-upload',
      pageCount: 1,
      status: 'received',
    })
    const onNavigate = vi.fn()
    render(<FileUploadPage onNavigate={onNavigate} />)

    const input = document.querySelector('input[type="file"]') as HTMLInputElement
    fireEvent.change(input, { target: { files: [new File(['invoice'], 'invoice.pdf', { type: 'application/pdf' })] } })
    fireEvent.click(screen.getByRole('button', { name: /submit document/i }))

    expect(await screen.findByText('Document submitted')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /back to source selection/i }))

    expect(await screen.findByRole('button', { name: /^choose file$/i })).toBeInTheDocument()
    expect(screen.queryByText('Document submitted')).not.toBeInTheDocument()
    expect(onNavigate).not.toHaveBeenCalled()
  })
})
