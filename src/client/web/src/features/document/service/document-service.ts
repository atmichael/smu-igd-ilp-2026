import { apiJson } from '../../../common/service/api-client'
import type { DocumentRecord, DocumentSearchResponse } from '../model/document-model'

export function searchDocuments(search: string): Promise<DocumentSearchResponse> {
  const query = new URLSearchParams()
  if (search.trim()) query.set('search', search.trim())
  const suffix = query.size > 0 ? `?${query.toString()}` : ''
  return apiJson<DocumentSearchResponse>(`/api/documents${suffix}`)
}

export function getDocumentsBelowThreshold(): Promise<DocumentSearchResponse> {
  return apiJson<DocumentSearchResponse>('/api/documents/below-threshold')
}

export function getDocumentById(documentId: number): Promise<DocumentRecord> {
  return apiJson<DocumentRecord>(`/api/documents/${encodeURIComponent(String(documentId))}`)
}
