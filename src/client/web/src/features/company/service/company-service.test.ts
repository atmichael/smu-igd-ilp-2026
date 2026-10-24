import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiFetch } from '../../../common/service/api-client'
import { getCompanyById, searchCompanies } from './company-service'

vi.mock('../../../common/service/api-client', () => ({
  apiFetch: vi.fn(),
}))

describe('company service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('searches companies with a partial-match query', async () => {
    vi.mocked(apiFetch).mockResolvedValue(
      new Response(JSON.stringify({ items: [], totalCount: 0 }), { status: 200 }),
    )

    await searchCompanies('Acme Ltd')

    expect(apiFetch).toHaveBeenCalledWith('/api/companies?search=Acme%20Ltd')
  })

  it('retrieves company details by id', async () => {
    vi.mocked(apiFetch).mockResolvedValue(
      new Response(JSON.stringify({ id: 42, uen: '123456789A', name: 'Acme' }), { status: 200 }),
    )

    await getCompanyById(42)

    expect(apiFetch).toHaveBeenCalledWith('/api/companies/42')
  })
})
