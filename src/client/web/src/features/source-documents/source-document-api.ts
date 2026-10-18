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

function authorizationHeaders(): Record<string, string> {
  return import.meta.env.DEV ? { Authorization: 'Test camera-capture' } : {}
}

async function readProblem(response: Response): Promise<{ title?: string; detail?: string }> {
  const problem = await response.json().catch(() => ({}))
  return {
    title: typeof problem?.title === 'string' ? problem.title : undefined,
    detail: typeof problem?.detail === 'string' ? problem.detail : undefined,
  }
}

export async function submitSourceDocument(
  request: SubmitSourceDocumentRequest,
): Promise<SourceDocumentResponse> {
  const formData = new FormData()

  if (request.channel === 'camera-capture') {
    if (request.pages.length === 0 || request.pages.length > 3) {
      throw new Error('A camera capture must contain between one and three pages.')
    }

    formData.append('channel', request.channel)
    request.pages.forEach((page, index) => {
      formData.append('pages', page, `page-${index + 1}.jpg`)
    })
  } else if (request.channel === 'file-upload') {
    formData.append('channel', request.channel)
    formData.append('file', request.file, request.file.name)
  } else {
    throw new Error('A supported source-document channel is required.')
  }

  const idempotencyKey = request.idempotencyKey ?? crypto.randomUUID()

  const response = await fetch('/api/source-documents', {
    method: 'POST',
    headers: {
      'Idempotency-Key': idempotencyKey,
      ...authorizationHeaders(),
    },
    body: formData,
  })

  if (!response.ok) {
    const problem = await readProblem(response)
    throw new SourceDocumentRequestError(
      response.status,
      problem.title ??
        (request.channel === 'file-upload'
          ? 'The file could not be submitted.'
          : 'The camera capture could not be submitted.'),
      problem.detail,
    )
  }

  return (await response.json()) as SourceDocumentResponse
}

/** Fetches the original with the same credentials as intake so a view or download link works once sign-in is enforced. */
export async function fetchOriginalFile(sourceDocumentId: string): Promise<Blob> {
  const response = await fetch(`/api/source-documents/${encodeURIComponent(sourceDocumentId)}/file`, {
    headers: authorizationHeaders(),
  })

  if (!response.ok) {
    throw new SourceDocumentRequestError(response.status, 'The original file could not be retrieved.')
  }

  return await response.blob()
}
