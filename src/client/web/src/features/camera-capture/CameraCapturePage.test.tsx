import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { CameraCapturePage } from './CameraCapturePage'

const { submitSourceDocument } = vi.hoisted(() => ({
  submitSourceDocument: vi.fn(),
}))

vi.mock('../source-documents/service/source-document-service', () => ({ submitSourceDocument }))
vi.mock('./components/CameraCapture', () => ({
  CameraCapture: ({
    onSubmit,
  }: {
    onSubmit: (request: { channel: 'camera-capture'; pages: Blob[]; idempotencyKey: string }) => Promise<unknown>
  }) => (
    <div>
      <p>Camera capture initial state</p>
      <button
        onClick={() => void onSubmit({ channel: 'camera-capture', pages: [], idempotencyKey: 'key' })}
      >
        Submit test capture
      </button>
    </div>
  ),
}))

describe('CameraCapturePage', () => {
  it('returns to the initial capture state after the user leaves the submission confirmation', async () => {
    submitSourceDocument.mockResolvedValue({
      sourceDocumentId: 'source-doc-1',
      channel: 'camera-capture',
      pageCount: 1,
      status: 'received',
    })
    const onNavigate = vi.fn()
    render(<CameraCapturePage onNavigate={onNavigate} />)

    fireEvent.click(screen.getByRole('button', { name: /submit test capture/i }))
    expect(await screen.findByText('Document submitted')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /back to source selection/i }))

    expect(await screen.findByText('Camera capture initial state')).toBeInTheDocument()
    expect(onNavigate).not.toHaveBeenCalled()
  })
})
