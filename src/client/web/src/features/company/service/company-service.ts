import { apiJson } from '../../../common/service/api-client'
import type { CompanyDetails, CompanySummary, CompanyListQuery, CompanySearchResponse } from '../model/company-model'

export function searchCompanies(search: string, options?: CompanyListQuery): Promise<CompanySearchResponse> {
  const query = new URLSearchParams()
  if (search.trim()) query.set('search', search.trim())
  if (options?.uen?.trim()) query.set('uen', options.uen.trim())
  if (options?.page !== undefined) query.set('page', String(options.page))
  if (options?.pageSize !== undefined) query.set('pageSize', String(options.pageSize))
  if (options?.sortBy !== undefined) query.set('sortBy', options.sortBy)
  if (options?.sortDirection !== undefined) query.set('sortDirection', options.sortDirection)
  const suffix = query.size > 0 ? `?${query.toString()}` : ''
  return apiJson<CompanySearchResponse>(`/api/companies${suffix}`)
}

export function getCompanyById(companyId: number): Promise<CompanyDetails> {
  return apiJson<CompanyDetails>(`/api/companies/${encodeURIComponent(String(companyId))}`)
}

export async function findCompaniesByUen(uen: string): Promise<CompanySummary[]> {
  const result = await searchCompanies('', { uen })
  return result.items
}
