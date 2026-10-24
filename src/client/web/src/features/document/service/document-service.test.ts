import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiFetch } from '../../../common/service/api-client'
import { getDocumentById, getDocumentsBelowThreshold, searchDocuments } from './document-service'

vi.mock('../../../common/service/api-client', () => ({
  apiFetch: vi.fn(),
}))

describe('document service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('searches documents using the search query', async () => {
    vi.mocked(apiFetch).mockResolvedValue(
      new Response(JSON.stringify({ items: [], totalCount: 0 }), { status: 200 }),
    )

    await searchDocuments('INV 100')

    expect(apiFetch).toHaveBeenCalledWith('/api/documents?search=INV%20100')
  })

  it('loads below-threshold documents without sending a threshold', async () => {
    vi.mocked(apiFetch).mockResolvedValue(
      new Response(JSON.stringify({ items: [], totalCount: 0 }), { status: 200 }),
    )

    await getDocumentsBelowThreshold()

    expect(apiFetch).toHaveBeenCalledWith('/api/documents/below-threshold')
  })

  it('retrieves a document by id', async () => {
    vi.mocked(apiFetch).mockResolvedValue(
      new Response(JSON.stringify({ id: 17, refNumber: 'INV-17' }), { status: 200 }),
    )

    await getDocumentById(17)

    expect(apiFetch).toHaveBeenCalledWith('/api/documents/17')
  })
})
