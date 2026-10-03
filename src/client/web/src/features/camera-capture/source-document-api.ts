export interface SourceDocumentResponse {
  sourceDocumentId: string
  source: 'camera-capture'
  pageCount: number
  status: 'received'
}

export interface SubmitSourceDocumentRequest {
  source: 'camera-capture'
  pages: Blob[]
  idempotencyKey?: string
}

export async function submitSourceDocument(
  request: SubmitSourceDocumentRequest,
): Promise<SourceDocumentResponse> {
  if (!request.source || request.source !== 'camera-capture') {
    throw new Error('Camera capture source is required.')
  }

  if (request.pages.length === 0 || request.pages.length > 3) {
    throw new Error('A camera capture must contain between one and three pages.')
  }

  const formData = new FormData()
  formData.append('source', request.source)

  request.pages.forEach((page, index) => {
    formData.append('pages', page, `page-${index + 1}.jpg`)
  })

  const idempotencyKey = request.idempotencyKey ?? crypto.randomUUID()

  const response = await fetch('/api/source-documents', {
    method: 'POST',
    headers: {
      'Idempotency-Key': idempotencyKey,
      ...(import.meta.env.DEV ? { Authorization: 'Test camera-capture' } : {}),
    },
    body: formData,
  })

  if (!response.ok) {
    const problem = await response.json().catch(() => ({}))
    throw new Error(
      typeof problem?.title === 'string'
        ? problem.title
        : 'The camera capture could not be submitted.',
    )
  }

  return (await response.json()) as SourceDocumentResponse
}
