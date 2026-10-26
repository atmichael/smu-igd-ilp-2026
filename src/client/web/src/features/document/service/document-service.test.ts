import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiFetch, apiJson } from '../../../common/service/api-client'
import { deleteDocument, getDocumentById, getDocumentsBelowThreshold, searchDocuments } from './document-service'

vi.mock('../../../common/service/api-client', () => ({
  apiJson: vi.fn(),
  apiFetch: vi.fn(),
}))

describe('document service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('searches documents using the search query', async () => {
    await searchDocuments('INV 100')

    expect(apiJson).toHaveBeenCalledWith('/api/documents?search=INV+100')
  })

  it('includes pagination and sorting options in document searches', async () => {
    await searchDocuments('INV 100', {
      page: 2,
      pageSize: 10,
      sortBy: 'totalAmount',
      sortDirection: 'asc',
    })

    expect(apiJson).toHaveBeenCalledWith(
      '/api/documents?search=INV+100&page=2&pageSize=10&sortBy=totalAmount&sortDirection=asc',
    )
  })

  it('loads below-threshold documents without sending a threshold', async () => {
    await getDocumentsBelowThreshold()

    expect(apiJson).toHaveBeenCalledWith('/api/documents/below-threshold')
  })

  it('includes pagination and sorting options for below-threshold documents', async () => {
    await getDocumentsBelowThreshold({ page: 3, pageSize: 5, sortBy: 'confidence', sortDirection: 'desc' })

    expect(apiJson).toHaveBeenCalledWith(
      '/api/documents/below-threshold?page=3&pageSize=5&sortBy=confidence&sortDirection=desc',
    )
  })

  it('retrieves a document by id', async () => {
    await getDocumentById(17)

    expect(apiJson).toHaveBeenCalledWith('/api/documents/17')
  })

  it('deletes a document and surfaces API errors', async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce(new Response(null, { status: 204 }))
    await deleteDocument(5)
    expect(apiFetch).toHaveBeenCalledWith('/api/documents/5', { method: 'DELETE' })

    vi.mocked(apiFetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ detail: 'gone' }), { status: 404 }),
    )
    await expect(deleteDocument(5)).rejects.toThrow('gone')
  })
})
