import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiJson } from '../../../common/service/api-client'
import { findCompaniesByUen, getCompanyById, searchCompanies } from './company-service'

vi.mock('../../../common/service/api-client', () => ({
  apiJson: vi.fn(),
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

  it('searches companies by UEN using the uen query parameter', async () => {
    vi.mocked(apiJson).mockResolvedValueOnce({ items: [{ id: 1, uen: 'A/1', name: 'Acme', totalOutstandingSgd: 0 }], totalCount: 1 })

    const result = await findCompaniesByUen(' A/1 ')

    expect(apiJson).toHaveBeenCalledWith('/api/companies?uen=A%2F1')
    expect(result.map((company) => company.name)).toEqual(['Acme'])
  })
})
