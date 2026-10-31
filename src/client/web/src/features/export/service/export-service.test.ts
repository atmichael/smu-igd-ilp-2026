import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiJson } from '../../../common/service/api-client'
import { getDocumentsForExport } from './export-service'

vi.mock('../../../common/service/api-client', () => ({ apiJson: vi.fn() }))

describe('export service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('requests the export endpoint without filters', async () => {
    await getDocumentsForExport()
    expect(apiJson).toHaveBeenCalledWith('/api/documents/export')
  })

  it('sends the created date range', async () => {
    await getDocumentsForExport({ createdFrom: '2026-10-01', createdTo: '2026-10-31' })
    expect(apiJson).toHaveBeenCalledWith('/api/documents/export?createdFrom=2026-10-01&createdTo=2026-10-31')
  })
})
