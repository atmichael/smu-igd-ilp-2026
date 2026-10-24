import { apiFetch } from '../../../common/service/api-client'
import {
  SourceDocumentRequestError,
  type SourceDocumentResponse,
  type SubmitSourceDocumentRequest,
} from '../../../common/model/source-document-model'

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

  const response = await apiFetch('/api/source-documents', {
    method: 'POST',
    headers: {
      'Idempotency-Key': idempotencyKey,
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
  const response = await apiFetch(`/api/source-documents/${encodeURIComponent(sourceDocumentId)}/file`)

  if (!response.ok) {
    throw new SourceDocumentRequestError(response.status, 'The original file could not be retrieved.')
  }

  return await response.blob()
}
