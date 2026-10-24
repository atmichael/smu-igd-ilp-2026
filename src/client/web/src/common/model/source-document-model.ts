export type SourceDocumentChannel = 'camera-capture' | 'file-upload'

export interface SourceDocumentResponse {
  sourceDocumentId: string
  channel: SourceDocumentChannel
  pageCount: number
  status: 'received'
  mediaType?: string
  origin?: { upload?: { originalFileName: string } }
}

export type SubmitSourceDocumentRequest =
  | { channel: 'camera-capture'; pages: Blob[]; idempotencyKey?: string }
  | { channel: 'file-upload'; file: File; idempotencyKey?: string }

/** A rejected submission; `detail` is the server's user-safe explanation when it sent one. */
export class SourceDocumentRequestError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string

  constructor(status: number, title: string, detail?: string) {
    super(title)
    this.name = 'SourceDocumentRequestError'
    this.status = status
    this.title = title
    this.detail = detail
  }
}
