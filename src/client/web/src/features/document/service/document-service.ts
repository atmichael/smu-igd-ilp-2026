import { apiJson } from '../../../common/service/api-client'
import type { DocumentListQuery, DocumentRecord, DocumentSearchResponse } from '../model/document-model'

export function searchDocuments(search: string, options?: DocumentListQuery): Promise<DocumentSearchResponse> {
  const query = new URLSearchParams()
  if (search.trim()) query.set('search', search.trim())
  addListOptions(query, options)
  const suffix = query.size > 0 ? `?${query.toString()}` : ''
  return apiJson<DocumentSearchResponse>(`/api/documents${suffix}`)
}

export function getDocumentsBelowThreshold(options?: DocumentListQuery): Promise<DocumentSearchResponse> {
  const query = new URLSearchParams()
  addListOptions(query, options)
  const suffix = query.size > 0 ? `?${query.toString()}` : ''
  return apiJson<DocumentSearchResponse>(`/api/documents/below-threshold${suffix}`)
}

export function getDocumentById(documentId: number): Promise<DocumentRecord> {
  return apiJson<DocumentRecord>(`/api/documents/${encodeURIComponent(String(documentId))}`)
}

function addListOptions(query: URLSearchParams, options?: DocumentListQuery): void {
  if (!options) return
  if (options.page !== undefined) query.set('page', String(options.page))
  if (options.pageSize !== undefined) query.set('pageSize', String(options.pageSize))
  if (options.sortBy !== undefined) query.set('sortBy', options.sortBy)
  if (options.sortDirection !== undefined) query.set('sortDirection', options.sortDirection)
}
