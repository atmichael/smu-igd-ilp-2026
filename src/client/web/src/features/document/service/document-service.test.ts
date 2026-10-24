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

  it('loads below-threshold documents without sending a threshold', async () => {
    await getDocumentsBelowThreshold()

    expect(apiJson).toHaveBeenCalledWith('/api/documents/below-threshold')
  })

  it('retrieves a document by id', async () => {
    await getDocumentById(17)

    expect(apiJson).toHaveBeenCalledWith('/api/documents/17')
  })
})
