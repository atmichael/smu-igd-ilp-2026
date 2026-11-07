import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { FileUploadPage } from './FileUploadPage'

const { submitSourceDocument, fetchOriginalFile, getUploadConfig } = vi.hoisted(() => ({
  submitSourceDocument: vi.fn(),
  fetchOriginalFile: vi.fn(),
  getUploadConfig: vi.fn().mockResolvedValue({ attachmentLimitInMB: 2 }),
}))

vi.mock('../source-documents/service/source-document-service', () => ({ submitSourceDocument, fetchOriginalFile }))
vi.mock('./service/upload-config-service', () => ({ getUploadConfig }))

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

    await screen.findByRole('button', { name: /^choose file$/i })
    const input = document.querySelector('input[type="file"]') as HTMLInputElement
    fireEvent.change(input, { target: { files: [new File(['invoice'], 'invoice.pdf', { type: 'application/pdf' })] } })
    fireEvent.click(screen.getByRole('button', { name: /submit document/i }))

    expect(await screen.findByText('Document submitted')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /back to source selection/i }))

    expect(await screen.findByRole('button', { name: /^choose file$/i })).toBeInTheDocument()
    expect(screen.queryByText('Document submitted')).not.toBeInTheDocument()
    expect(onNavigate).not.toHaveBeenCalled()
  })

  it.each([
    [2 * 1024 * 1024 - 1, true],
    [2 * 1024 * 1024, true],
    [2 * 1024 * 1024 + 1, false],
  ])('applies the configured limit to a %i byte file', async (size, accepted) => {
    render(<FileUploadPage onNavigate={vi.fn()} />)
    await screen.findByRole('button', { name: /^choose file$/i })
    const file = new File(['x'], 'invoice.pdf', { type: 'application/pdf' })
    Object.defineProperty(file, 'size', { value: size })
    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files: [file] } })
    if (accepted) expect(screen.getByRole('button', { name: /submit document/i })).toBeEnabled()
    else expect(screen.getByRole('alert')).toHaveTextContent(/limit is 2 MB/i)
  })

  it('uses a non-default server limit', async () => {
    getUploadConfig.mockResolvedValueOnce({ attachmentLimitInMB: 3 })
    render(<FileUploadPage onNavigate={vi.fn()} />)
    await screen.findByRole('button', { name: /^choose file$/i })
    const file = new File(['x'], 'invoice.pdf', { type: 'application/pdf' })
    Object.defineProperty(file, 'size', { value: 3 * 1024 * 1024 })
    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files: [file] } })
    expect(screen.getByRole('button', { name: /submit document/i })).toBeEnabled()
  })
})
