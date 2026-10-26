import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiFetch, apiJson } from '../../../common/service/api-client'
import { findCompanyByUen, getCompanyById, searchCompanies } from './company-service'

vi.mock('../../../common/service/api-client', () => ({
  apiJson: vi.fn(),
  apiFetch: vi.fn(),
}))

describe('company service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('searches companies with a partial-match query', async () => {
    await searchCompanies('Acme Ltd')

    expect(apiJson).toHaveBeenCalledWith('/api/companies?search=Acme+Ltd')
  })

  it('includes pagination and sorting options in company searches', async () => {
    await searchCompanies('Acme Ltd', {
      page: 2,
      pageSize: 10,
      sortBy: 'totalOutstandingSgd',
      sortDirection: 'desc',
    })

    expect(apiJson).toHaveBeenCalledWith(
      '/api/companies?search=Acme+Ltd&page=2&pageSize=10&sortBy=totalOutstandingSgd&sortDirection=desc',
    )
  })

  it('retrieves company details by id', async () => {
    await getCompanyById(42)

    expect(apiJson).toHaveBeenCalledWith('/api/companies/42')
  })

  it('finds a company by UEN and returns null when it does not exist', async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce(new Response(JSON.stringify({ id: 1, uen: 'A/1', name: 'Acme' })))
    expect((await findCompanyByUen('A/1'))?.name).toBe('Acme')
    expect(apiFetch).toHaveBeenCalledWith('/api/companies/by-uen/A%2F1')

    vi.mocked(apiFetch).mockResolvedValueOnce(new Response(null, { status: 404 }))
    expect(await findCompanyByUen('X')).toBeNull()
  })
})
