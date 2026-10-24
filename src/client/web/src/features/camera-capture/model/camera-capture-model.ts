export type CameraCaptureState =
  | 'idle'
  | 'requestingPermission'
  | 'liveCapture'
  | 'pageReview'
  | 'documentReview'
  | 'submitting'
  | 'submitted'
  | 'recoverableError'
  | 'cancelled'

export interface CameraCapturePage {
  id: string
  file: Blob
  previewUrl: string
  position: number
}

export interface CameraCaptureSession {
  state: CameraCaptureState
  acceptedPages: CameraCapturePage[]
  pendingPage: CameraCapturePage | null
  errorMessage: string | null
}

export interface SourceDocumentSubmissionRequest {
  channel: 'camera-capture'
  pages: Blob[]
  idempotencyKey: string
}

export function createInitialSession(): CameraCaptureSession {
  return {
    state: 'idle',
    acceptedPages: [],
    pendingPage: null,
    errorMessage: null,
  }
}

export function canAddPage(session: CameraCaptureSession): boolean {
  return session.acceptedPages.length < 3 && !session.pendingPage
}

export function buildSubmissionRequest(
  pages: CameraCapturePage[],
  idempotencyKey = crypto.randomUUID(),
): SourceDocumentSubmissionRequest {
  return {
    channel: 'camera-capture',
    pages: pages.map((page) => page.file),
    idempotencyKey,
  }
}

export function normalisePagePositions(pages: CameraCapturePage[]): CameraCapturePage[] {
  return pages.map((page, index) => ({
    ...page,
    position: index + 1,
  }))
}

export function addPendingPage(
  session: CameraCaptureSession,
  file: Blob,
  previewUrl: string,
): CameraCaptureSession {
  if (!canAddPage(session)) {
    URL.revokeObjectURL(previewUrl)
    return session
  }

  return {
    ...session,
    pendingPage: {
      id: crypto.randomUUID(),
      file,
      previewUrl,
      position: session.acceptedPages.length + 1,
    },
    state: 'pageReview',
    errorMessage: null,
  }
}

export function acceptPendingPage(session: CameraCaptureSession): CameraCaptureSession {
  if (!session.pendingPage || session.acceptedPages.length >= 3) {
    return session
  }

  const accepted = normalisePagePositions([
    ...session.acceptedPages,
    {
      ...session.pendingPage,
      position: session.acceptedPages.length + 1,
    },
  ])

  return {
    ...session,
    acceptedPages: accepted,
    pendingPage: null,
    state: 'documentReview',
    errorMessage: null,
  }
}

export function removeAcceptedPage(
  session: CameraCaptureSession,
  pageId: string,
): CameraCaptureSession {
  const pageToRemove = session.acceptedPages.find((page) => page.id === pageId)
  if (!pageToRemove) {
    return session
  }

  URL.revokeObjectURL(pageToRemove.previewUrl)
  const nextPages = normalisePagePositions(
    session.acceptedPages.filter((page) => page.id !== pageId),
  )

  return {
    ...session,
    acceptedPages: nextPages,
    state: nextPages.length > 0 ? 'documentReview' : 'idle',
    errorMessage: null,
  }
}
