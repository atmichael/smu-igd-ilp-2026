import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { CameraCapture } from './CameraCapture'

describe('CameraCapture', () => {
  it('hides the capture action until the camera is live', () => {
    render(
      <CameraCapture
        onExit={() => undefined}
        onSubmit={() =>
          Promise.resolve({
            sourceDocumentId: 'doc-1',
            channel: 'camera-capture',
            pageCount: 1,
            status: 'received',
          })
        }
        startCamera={async () => ({ getTracks: () => [] } as unknown as MediaStream)}
      />,
    )

    expect(screen.getByRole('button', { name: /start camera capture/i })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /capture page/i })).not.toBeInTheDocument()
  })

  it('requests camera access only after explicit user action', async () => {
    const startCapture = vi.fn()
    render(
      <CameraCapture
        onExit={() => undefined}
        onSubmit={() =>
          Promise.resolve({
            sourceDocumentId: 'doc-1',
            channel: 'camera-capture',
            pageCount: 1,
            status: 'received',
          })
        }
        startCamera={startCapture}
      />,
    )

    expect(screen.getByRole('button', { name: /start camera capture/i })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /start camera capture/i }))

    await waitFor(() => expect(startCapture).toHaveBeenCalledTimes(1))
  })

  it('starts the live preview immediately when autoStart is enabled', async () => {
    const startCapture = vi.fn(async () => ({
      getTracks: () => [{ readyState: 'live', stop: vi.fn() }],
      active: true,
    } as unknown as MediaStream))

    render(
      <CameraCapture
        autoStart
        onExit={() => undefined}
        onSubmit={() =>
          Promise.resolve({
            sourceDocumentId: 'doc-1',
            channel: 'camera-capture',
            pageCount: 1,
            status: 'received',
          })
        }
        startCamera={startCapture}
      />,
    )

    await waitFor(() => expect(startCapture).toHaveBeenCalledTimes(1))
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /capture page/i })).toBeInTheDocument()
    })
  })

  it('captures, reviews, and accepts up to three pages', async () => {
    const mockBlob = new Blob(['fake-image'], { type: 'image/jpeg' })
    const previewUrl = 'blob:uploaded-preview'
    const originalCreateObjectURL = URL.createObjectURL
    const originalRevokeObjectURL = URL.revokeObjectURL

    Object.defineProperty(URL, 'createObjectURL', {
      value: vi.fn(() => previewUrl),
      configurable: true,
    })
    Object.defineProperty(URL, 'revokeObjectURL', {
      value: vi.fn(),
      configurable: true,
    })

    const originalGetContext = HTMLCanvasElement.prototype.getContext
    HTMLCanvasElement.prototype.getContext = vi.fn(() => ({
      drawImage: vi.fn(),
    } as any)) as typeof HTMLCanvasElement.prototype.getContext
    HTMLCanvasElement.prototype.toBlob = vi.fn((callback) => callback?.(mockBlob)) as typeof HTMLCanvasElement.prototype.toBlob

    render(
      <CameraCapture
        onExit={() => undefined}
        onSubmit={() =>
          Promise.resolve({
            sourceDocumentId: 'doc-1',
            channel: 'camera-capture',
            pageCount: 1,
            status: 'received',
          })
        }
        startCamera={async () => ({
          getTracks: () => [{ readyState: 'live', stop: vi.fn() }],
          active: true,
        } as unknown as MediaStream)}
      />,
    )

    const video = document.querySelector('video')
    expect(video).not.toBeNull()
    Object.defineProperty(video, 'videoWidth', { value: 1200, configurable: true })
    Object.defineProperty(video, 'videoHeight', { value: 1600, configurable: true })

    fireEvent.click(screen.getByRole('button', { name: /start camera capture/i }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /capture page/i })).toBeInTheDocument()
    })

    fireEvent.click(screen.getByRole('button', { name: /capture page/i }))

    await waitFor(() => {
      expect(screen.getByAltText(/captured page preview/i)).toHaveAttribute('src', previewUrl)
    })

    fireEvent.click(screen.getByRole('button', { name: /accept page/i }))

    expect(screen.getByAltText(/accepted page 1 preview/i)).toHaveAttribute('src', previewUrl)
    expect(screen.getByAltText(/accepted page 1 thumbnail/i)).toHaveAttribute('src', previewUrl)
    expect(URL.revokeObjectURL).not.toHaveBeenCalledWith(previewUrl)

    fireEvent.click(screen.getByRole('button', { name: /capture another page/i }))
    expect(screen.getByRole('button', { name: /capture page/i })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /capture page/i }))
    expect(await screen.findByAltText(/captured page preview/i)).toHaveAttribute('src', previewUrl)
    fireEvent.click(screen.getByRole('button', { name: /accept page/i }))
    expect(screen.getByAltText(/accepted page 2 preview/i)).toHaveAttribute('src', previewUrl)
    expect(screen.getAllByAltText(/accepted page \d thumbnail/i)).toHaveLength(2)

    fireEvent.click(screen.getByRole('button', { name: /capture another page/i }))
    fireEvent.click(screen.getByRole('button', { name: /capture page/i }))
    expect(await screen.findByAltText(/captured page preview/i)).toHaveAttribute('src', previewUrl)
    fireEvent.click(screen.getByRole('button', { name: /accept page/i }))
    expect(screen.getAllByAltText(/accepted page \d thumbnail/i)).toHaveLength(3)
    expect(screen.queryByRole('button', { name: /capture another page/i })).not.toBeInTheDocument()

    Object.defineProperty(URL, 'createObjectURL', {
      value: originalCreateObjectURL,
      configurable: true,
    })
    Object.defineProperty(URL, 'revokeObjectURL', {
      value: originalRevokeObjectURL,
      configurable: true,
    })
    HTMLCanvasElement.prototype.getContext = originalGetContext
    delete (HTMLCanvasElement.prototype as { toBlob?: unknown }).toBlob
  })

  it('keeps pages in accepted order and blocks a fourth page', async () => {
    render(
      <CameraCapture
        onExit={() => undefined}
        onSubmit={() =>
          Promise.resolve({
            sourceDocumentId: 'doc-1',
            channel: 'camera-capture',
            pageCount: 1,
            status: 'received',
          })
        }
        startCamera={async () => ({ getTracks: () => [] } as unknown as MediaStream)}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /start camera capture/i }))

    expect(screen.getByText(/capture one to three pages/i)).toBeInTheDocument()
  })
})
