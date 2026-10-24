import { act, createEvent, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { FileUpload } from './FileUpload'
import {
  SourceDocumentRequestError,
  type SourceDocumentResponse,
} from '../../../common/model/source-document-model'

type SubmitFn = NonNullable<React.ComponentProps<typeof FileUpload>['onSubmit']>

const MAX_BYTES = 20 * 1024 * 1024

const received: SourceDocumentResponse = {
  sourceDocumentId: '5a14d651-c487-4e8b-babc-a81051053d7b',
  channel: 'file-upload',
  pageCount: 2,
  status: 'received',
  mediaType: 'application/pdf',
  origin: { upload: { originalFileName: 'invoice-0042.pdf' } },
}

function makeFile(name: string, type = 'application/pdf', size?: number): File {
  const file = new File(['%PDF-1.7 content'], name, { type })
  if (size !== undefined) {
    Object.defineProperty(file, 'size', { value: size, configurable: true })
  }
  return file
}

function renderUpload(overrides: Partial<React.ComponentProps<typeof FileUpload>> = {}) {
  const onSubmit = vi.fn<SubmitFn>().mockResolvedValue(received)
  const onExit = vi.fn()
  const view = render(<FileUpload onExit={onExit} onSubmit={onSubmit} {...overrides} />)
  const input = () => view.container.querySelector('input[type="file"]') as HTMLInputElement
  const choose = (file: File) => fireEvent.change(input(), { target: { files: [file] } })
  return { onSubmit, onExit, input, choose, ...view }
}

const submitButton = () => screen.getByRole('button', { name: /submit document/i })

describe('FileUpload', () => {
  beforeEach(() => {
    Object.defineProperty(URL, 'createObjectURL', { value: vi.fn(() => 'blob:original'), configurable: true })
    Object.defineProperty(URL, 'revokeObjectURL', { value: vi.fn(), configurable: true })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('selecting and submitting a file', () => {
    it('shows the file name and passing quick checks before anything is sent', () => {
      const { choose, onSubmit } = renderUpload()

      choose(makeFile('invoice-0042.pdf'))

      expect(screen.getByText('Selected file: invoice-0042.pdf')).toBeInTheDocument()
      expect(screen.getByText(/ready to submit/i)).toBeInTheDocument()
      expect(onSubmit).not.toHaveBeenCalled()
    })

    it('shows the file size beside the name', () => {
      const { choose } = renderUpload()

      choose(makeFile('invoice.pdf', 'application/pdf', 2 * 1024 * 1024))

      expect(screen.getByText('2.0 MB')).toBeInTheDocument()
    })

    it('puts Back first and the primary action last so tab order follows the layout', () => {
      const { choose } = renderUpload()

      choose(makeFile('invoice.pdf'))

      const names = screen.getAllByRole('button').map((button) => button.textContent?.replace('←', '').trim())
      expect(names[0]).toBe('Back to source selection')
      expect(names.at(-1)).toBe('Submit document')
    })

    it('accepts one file at a time and does not force the camera', () => {
      const { input } = renderUpload()

      expect(input()).not.toHaveAttribute('multiple')
      expect(input()).not.toHaveAttribute('capture')
      expect(input()).toHaveAttribute('accept', 'application/pdf,image/jpeg,image/png')
    })

    it('sends only the replacement when the selection is replaced', async () => {
      const { choose, onSubmit } = renderUpload()

      choose(makeFile('wrong.pdf'))
      choose(makeFile('right.pdf'))
      fireEvent.click(submitButton())

      await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
      expect(onSubmit.mock.calls[0][0].file.name).toBe('right.pdf')
    })

    it('clears the selection', () => {
      const { choose } = renderUpload()

      choose(makeFile('invoice.pdf'))
      fireEvent.click(screen.getByRole('button', { name: /clear selection/i }))

      expect(screen.queryByText(/selected file/i)).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /submit document/i })).not.toBeInTheDocument()
    })

    it('submits one file-upload request with an idempotency key and shows progress', async () => {
      let resolve!: (value: SourceDocumentResponse) => void
      const onSubmit = vi.fn<SubmitFn>(() => new Promise((done) => { resolve = done }))
      const { choose } = renderUpload({ onSubmit })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())

      expect(await screen.findByText(/submitting the document/i)).toBeInTheDocument()
      expect(submitButton()).toBeDisabled()
      const request = onSubmit.mock.calls[0][0]
      expect(request.channel).toBe('file-upload')
      expect(request.file.name).toBe('invoice.pdf')
      expect(request.idempotencyKey).toMatch(/^[0-9a-f-]{36}$/)

      await act(async () => resolve(received))
      expect(await screen.findByText('Document received')).toBeInTheDocument()
    })

    it('confirms with the source document ID and offers the next actions', async () => {
      const { choose } = renderUpload()

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())

      expect(await screen.findByText(`Source document ID: ${received.sourceDocumentId}`)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /upload another/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /view original/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /download original/i })).toBeInTheDocument()
    })

    it('returns to an empty selection with Upload another', async () => {
      const { choose } = renderUpload()

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())
      fireEvent.click(await screen.findByRole('button', { name: /upload another/i }))

      expect(screen.getByRole('button', { name: /^choose file$/i })).toBeInTheDocument()
      expect(screen.queryByText(/selected file/i)).not.toBeInTheDocument()
    })

    it('views and downloads the original through an authenticated fetch and releases the URLs', async () => {
      const open = vi.spyOn(window, 'open').mockReturnValue(null)
      const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined)
      const onFetchOriginal = vi.fn(async () => new Blob(['pdf'], { type: 'application/pdf' }))
      const { choose, unmount } = renderUpload({ onFetchOriginal })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())
      fireEvent.click(await screen.findByRole('button', { name: /view original/i }))
      await waitFor(() => expect(open).toHaveBeenCalledWith('blob:original', '_blank', 'noopener'))
      fireEvent.click(screen.getByRole('button', { name: /download original/i }))
      await waitFor(() => expect(click).toHaveBeenCalled())

      expect(onFetchOriginal).toHaveBeenCalledWith(received.sourceDocumentId)
      unmount()
      expect(URL.revokeObjectURL).toHaveBeenCalledTimes(2)
    })

    it('shows an error when the original cannot be retrieved', async () => {
      const onFetchOriginal = vi.fn(async () => { throw new SourceDocumentRequestError(404, 'x') })
      const { choose } = renderUpload({ onFetchOriginal })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())
      fireEvent.click(await screen.findByRole('button', { name: /view original/i }))

      expect(await screen.findByText(/original file could not be retrieved/i)).toBeInTheDocument()
    })

    it('sends nothing when the user leaves before confirming', () => {
      const { choose, onSubmit, onExit } = renderUpload()

      choose(makeFile('invoice.pdf'))
      fireEvent.click(screen.getByRole('button', { name: /back to source selection/i }))

      expect(onExit).toHaveBeenCalledTimes(1)
      expect(onSubmit).not.toHaveBeenCalled()
    })
  })

  describe('dragging and dropping a file', () => {
    const dropTarget = () => screen.getByRole('heading', { name: /upload a file/i })
    const dropFiles = (files: File[], types: string[] = ['Files']) =>
      fireEvent.drop(dropTarget(), { dataTransfer: { types, files } })

    it('selects a dropped file the same way as a chosen one', () => {
      renderUpload()

      dropFiles([makeFile('dropped.pdf')])

      expect(screen.getByText('Selected file: dropped.pdf')).toBeInTheDocument()
      expect(screen.getByText(/ready to submit/i)).toBeInTheDocument()
    })

    it('applies the same quick checks to a dropped file', () => {
      renderUpload()

      dropFiles([makeFile('notes.txt', 'text/plain')])

      expect(screen.getByText(/not supported/i)).toHaveTextContent('PDF, JPEG, or PNG')
      expect(screen.queryByRole('button', { name: /submit document/i })).not.toBeInTheDocument()
    })

    it('tells the user only one file is allowed and keeps the current selection', () => {
      const { choose } = renderUpload()

      choose(makeFile('first.pdf'))
      dropFiles([makeFile('a.pdf'), makeFile('b.pdf')])

      expect(screen.getByRole('alert')).toHaveTextContent('Only one file can be uploaded at a time')
      expect(screen.getByText('Selected file: first.pdf')).toBeInTheDocument()
    })

    it('replaces the current selection and sends only the dropped file', async () => {
      const { choose, onSubmit } = renderUpload()

      choose(makeFile('first.pdf'))
      dropFiles([makeFile('second.pdf')])
      fireEvent.click(submitButton())

      await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
      expect(onSubmit.mock.calls[0][0].file.name).toBe('second.pdf')
    })

    it('allows a file drop and ignores drags that are not files', () => {
      renderUpload()

      const fileDrag = createEvent.dragOver(dropTarget(), { dataTransfer: { types: ['Files'] } })
      const textDrag = createEvent.dragOver(dropTarget(), { dataTransfer: { types: ['text/plain'] } })
      fireEvent(dropTarget(), fileDrag)
      fireEvent(dropTarget(), textDrag)
      dropFiles([makeFile('text.pdf')], ['text/plain'])

      expect(fileDrag.defaultPrevented).toBe(true)
      expect(textDrag.defaultPrevented).toBe(false)
      expect(screen.queryByText(/selected file/i)).not.toBeInTheDocument()
    })

    it('ignores a drop after the document was received without leaving the page', async () => {
      const { choose, onSubmit } = renderUpload()

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())
      await screen.findByText('Document received')
      const drop = createEvent.drop(dropTarget(), { dataTransfer: { types: ['Files'], files: [makeFile('late.pdf')] } })
      fireEvent(dropTarget(), drop)

      expect(drop.defaultPrevented).toBe(true)
      expect(screen.getByText('Document received')).toBeInTheDocument()
      expect(onSubmit).toHaveBeenCalledTimes(1)
    })

    it('shows the drag hint only on devices with a mouse or trackpad and always keeps the button', () => {
      const original = window.matchMedia
      Object.defineProperty(window, 'matchMedia', {
        configurable: true,
        value: (query: string) => ({
          matches: true,
          media: query,
          addEventListener: () => undefined,
          removeEventListener: () => undefined,
          addListener: () => undefined,
          removeListener: () => undefined,
          onchange: null,
          dispatchEvent: () => false,
        }),
      })

      try {
        renderUpload()
        expect(screen.getByText(/drag a pdf, jpeg, or png file here, or choose one/i)).toBeInTheDocument()
        expect(screen.getByRole('button', { name: /^choose file$/i })).toBeInTheDocument()
      } finally {
        Object.defineProperty(window, 'matchMedia', { configurable: true, value: original })
      }
    })

    it('does not mention dragging when the device cannot drag files', () => {
      renderUpload()

      expect(screen.getByText(/choose one pdf, jpeg, or png file, up to 20 mb/i)).toBeInTheDocument()
      expect(screen.queryByText(/drag/i)).not.toBeInTheDocument()
    })
  })

  describe('quick checks and rejections', () => {
    it('blocks an unsupported extension and lists the accepted types', () => {
      const { choose } = renderUpload()

      choose(makeFile('notes.txt', 'text/plain'))

      expect(screen.getByText(/not supported/i)).toHaveTextContent('PDF, JPEG, or PNG')
      expect(screen.queryByRole('button', { name: /submit document/i })).not.toBeInTheDocument()
    })

    it('blocks an empty file', () => {
      const { choose } = renderUpload()

      choose(makeFile('empty.pdf', 'application/pdf', 0))

      expect(screen.getByText(/file is empty/i)).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /submit document/i })).not.toBeInTheDocument()
    })

    it('blocks a file one byte over 20 MB and names the limit', () => {
      const { choose } = renderUpload()

      choose(makeFile('big.pdf', 'application/pdf', MAX_BYTES + 1))

      expect(screen.getByText(/limit is 20 MB/i)).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /submit document/i })).not.toBeInTheDocument()
    })

    it('accepts a file of exactly 20 MB', () => {
      const { choose } = renderUpload()

      choose(makeFile('exact.pdf', 'application/pdf', MAX_BYTES))

      expect(submitButton()).toBeEnabled()
    })

    it.each([
      [415, 'Unsupported media type.', 'The PDF could not be read. Upload an unencrypted, undamaged PDF.'],
      [413, 'Payload too large.', 'The file is too large. The limit is 20 MB.'],
      [400, 'Empty file.', 'Choose a file that is not empty.'],
    ])('shows the server explanation for %i and lets the user choose another file', async (status, title, detail) => {
      const onSubmit = vi.fn<SubmitFn>().mockRejectedValue(new SourceDocumentRequestError(status, title, detail))
      const { choose } = renderUpload({ onSubmit })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())

      expect(await screen.findByText(detail)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /choose another file/i })).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /try again/i })).not.toBeInTheDocument()
    })

    it('uses fixed wording when the server sends no detail and never shows a storage path', async () => {
      const onSubmit = vi.fn<SubmitFn>().mockRejectedValue(new SourceDocumentRequestError(415, 'Unsupported media type.'))
      const { choose, container } = renderUpload({ onSubmit })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())

      expect(await screen.findByText(/could not be read/i)).toBeInTheDocument()
      expect(container.textContent).not.toMatch(/protected:\/\//)
    })

    it('explains a 401 and keeps the selection for a retry', async () => {
      const onSubmit = vi.fn<SubmitFn>().mockRejectedValue(new SourceDocumentRequestError(401, 'Unauthorized'))
      const { choose } = renderUpload({ onSubmit })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())

      expect(await screen.findByText(/upload is unavailable/i)).toBeInTheDocument()
      expect(screen.getByText('Selected file: invoice.pdf')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument()
    })
  })

  describe('retrying safely', () => {
    it('creates a key on selection and reuses it after a network error and after a server error', async () => {
      const onSubmit = vi
        .fn<SubmitFn>()
        .mockRejectedValueOnce(new TypeError('Failed to fetch'))
        .mockRejectedValueOnce(new SourceDocumentRequestError(500, 'Source document could not be stored.'))
        .mockResolvedValueOnce(received)
      const { choose } = renderUpload({ onSubmit })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())
      expect(await screen.findByText(/connection problem/i)).toBeInTheDocument()
      fireEvent.click(screen.getByRole('button', { name: /try again/i }))
      expect(await screen.findByText(/could not be stored/i)).toBeInTheDocument()
      expect(screen.getByText('Selected file: invoice.pdf')).toBeInTheDocument()
      fireEvent.click(screen.getByRole('button', { name: /try again/i }))
      expect(await screen.findByText('Document received')).toBeInTheDocument()

      const keys = onSubmit.mock.calls.map(([request]) => request.idempotencyKey)
      expect(keys).toHaveLength(3)
      expect(new Set(keys).size).toBe(1)
    })

    it('uses a new key after choosing a different file and after a successful submission', async () => {
      const { choose, onSubmit } = renderUpload()

      choose(makeFile('first.pdf'))
      fireEvent.click(submitButton())
      await screen.findByText('Document received')
      fireEvent.click(screen.getByRole('button', { name: /upload another/i }))
      choose(makeFile('first.pdf'))
      fireEvent.click(submitButton())
      await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(2))
      await screen.findByText('Document received')

      const [firstKey, secondKey] = onSubmit.mock.calls.map(([request]) => request.idempotencyKey)
      expect(secondKey).not.toBe(firstKey)
    })

    it('uses a new key when a different file replaces the selection', async () => {
      const onSubmit = vi.fn<SubmitFn>().mockRejectedValueOnce(new TypeError('Failed to fetch')).mockResolvedValue(received)
      const { choose } = renderUpload({ onSubmit })

      choose(makeFile('first.pdf'))
      fireEvent.click(submitButton())
      await screen.findByText(/connection problem/i)
      choose(makeFile('second.pdf'))
      fireEvent.click(submitButton())
      await screen.findByText('Document received')

      const [firstKey, secondKey] = onSubmit.mock.calls.map(([request]) => request.idempotencyKey)
      expect(secondKey).not.toBe(firstKey)
    })

    it('reports a key conflict without retrying automatically', async () => {
      const onSubmit = vi.fn<SubmitFn>().mockRejectedValue(new SourceDocumentRequestError(409, 'Idempotency key conflict.'))
      const { choose } = renderUpload({ onSubmit })

      choose(makeFile('invoice.pdf'))
      fireEvent.click(submitButton())

      expect(await screen.findByText(/could not be reconciled/i)).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /try again/i })).not.toBeInTheDocument()
      expect(screen.getByRole('button', { name: /choose another file/i })).toBeInTheDocument()
      expect(onSubmit).toHaveBeenCalledTimes(1)
    })
  })
})
