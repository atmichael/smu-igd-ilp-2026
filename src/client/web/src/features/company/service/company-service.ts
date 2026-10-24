import { apiJson } from '../../../common/service/api-client'
import type { CompanyDetails, CompanySearchResponse } from '../model/company-model'

export function searchCompanies(search: string): Promise<CompanySearchResponse> {
  const query = new URLSearchParams()
  if (search.trim()) query.set('search', search.trim())
  const suffix = query.size > 0 ? `?${query.toString()}` : ''
  return apiJson<CompanySearchResponse>(`/api/companies${suffix}`)
}

export function getCompanyById(companyId: number): Promise<CompanyDetails> {
  return apiJson<CompanyDetails>(`/api/companies/${encodeURIComponent(String(companyId))}`)
}
