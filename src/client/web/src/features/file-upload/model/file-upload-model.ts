import { SourceDocumentRequestError, type SourceDocumentResponse } from '../../../common/model/source-document-model'

/** Mirrors `SourceDocumentLimits.FileUploadMaxBytes` on the server (20 MB, file-upload only). */
export const FILE_UPLOAD_MAX_BYTES = 20 * 1024 * 1024
export const FILE_UPLOAD_MAX_LABEL = '20 MB'
export const FILE_INPUT_ACCEPT = 'application/pdf,image/jpeg,image/png'
export const ACCEPTED_TYPES_TEXT = 'PDF, JPEG, or PNG'

const ACCEPTED_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png']

export type QuickCheck = { ok: true } | { ok: false; message: string }

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} bytes`
  }

  return bytes < 1024 * 1024
    ? `${Math.round(bytes / 1024)} KB`
    : `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

/** Advisory only: the server alone decides content type and PDF readability. */
export function checkSelectedFile(file: Pick<File, 'name' | 'size'>): QuickCheck {
  if (file.size === 0) {
    return { ok: false, message: 'The file is empty. Choose a file that is not empty.' }
  }

  const name = file.name.toLowerCase()
  if (!ACCEPTED_EXTENSIONS.some((extension) => name.endsWith(extension))) {
    return { ok: false, message: `This file type is not supported. Choose a ${ACCEPTED_TYPES_TEXT} file.` }
  }

  if (file.size > FILE_UPLOAD_MAX_BYTES) {
    return { ok: false, message: `The file is too large. The limit is ${FILE_UPLOAD_MAX_LABEL}.` }
  }

  return { ok: true }
}

export type UploadState =
  | { status: 'idle' }
  | { status: 'selected'; file: File; idempotencyKey: string }
  | { status: 'blocked'; file: File; message: string }
  | { status: 'submitting'; file: File; idempotencyKey: string }
  | { status: 'received'; document: SourceDocumentResponse }
  | { status: 'rejected'; file: File; message: string }
  | { status: 'failed'; file: File; idempotencyKey: string; message: string }

export const initialUploadState: UploadState = { status: 'idle' }

/** A new selection always gets a new idempotency key, so a different file can never reuse an earlier submission. */
export function selectFile(file: File): UploadState {
  const check = checkSelectedFile(file)
  return check.ok
    ? { status: 'selected', file, idempotencyKey: crypto.randomUUID() }
    : { status: 'blocked', file, message: check.message }
}

export interface SubmissionFailure {
  /** `retry` keeps the file and its idempotency key; `rejected` requires choosing a file again. */
  outcome: 'retry' | 'rejected'
  message: string
}

export function describeSubmissionFailure(error: unknown): SubmissionFailure {
  if (!(error instanceof SourceDocumentRequestError)) {
    return {
      outcome: 'retry',
      message: 'Connection problem. Try again; the document will not be duplicated.',
    }
  }

  switch (true) {
    case error.status === 400:
      return { outcome: 'rejected', message: error.detail ?? 'Choose one file that is not empty.' }
    case error.status === 401:
      return { outcome: 'retry', message: 'Upload is unavailable. Sign in again and retry.' }
    case error.status === 409:
      return { outcome: 'rejected', message: 'This submission could not be reconciled. Choose the file again.' }
    case error.status === 413:
      return {
        outcome: 'rejected',
        message: error.detail ?? `The file is too large. The limit is ${FILE_UPLOAD_MAX_LABEL}.`,
      }
    case error.status === 415:
      return {
        outcome: 'rejected',
        message:
          error.detail ??
          `The file is not a supported type or could not be read. Upload a ${ACCEPTED_TYPES_TEXT} file; PDFs must not be password-protected.`,
      }
    case error.status >= 500:
      return { outcome: 'retry', message: 'The document could not be stored. Try again.' }
    default:
      return { outcome: 'rejected', message: 'The file could not be accepted. Choose another file.' }
  }
}

export function applySubmissionFailure(file: File, idempotencyKey: string, error: unknown): UploadState {
  const failure = describeSubmissionFailure(error)
  return failure.outcome === 'retry'
    ? { status: 'failed', file, idempotencyKey, message: failure.message }
    : { status: 'rejected', file, message: failure.message }
}
