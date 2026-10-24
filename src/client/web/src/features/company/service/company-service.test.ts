import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiJson } from '../../../common/service/api-client'
import { getCompanyById, searchCompanies } from './company-service'

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
})
