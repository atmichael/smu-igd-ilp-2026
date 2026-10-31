import { apiJson } from '../../../common/service/api-client'
import type { DocumentSearchResponse } from '../../document/model/document-model'

export interface ExportQuery {
  createdFrom?: string
  createdTo?: string
}

export function getDocumentsForExport(query: ExportQuery = {}): Promise<DocumentSearchResponse> {
  const params = new URLSearchParams()
  if (query.createdFrom) params.set('createdFrom', query.createdFrom)
  if (query.createdTo) params.set('createdTo', query.createdTo)
  const suffix = params.size > 0 ? `?${params.toString()}` : ''
  return apiJson<DocumentSearchResponse>(`/api/documents/export${suffix}`)
}

export function downloadCsv(fileName: string, content: string): void {
  const url = URL.createObjectURL(new Blob(['\uFEFF', content], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
