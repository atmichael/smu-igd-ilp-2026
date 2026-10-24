import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiJson } from '../../../common/service/api-client'
import { getDocumentById, getDocumentsBelowThreshold, searchDocuments } from './document-service'

vi.mock('../../../common/service/api-client', () => ({
  apiJson: vi.fn(),
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
})
