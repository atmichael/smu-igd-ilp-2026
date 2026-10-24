export interface CompanyDocumentSummary {
  id: number
  refNumber: string
  sentDate: string
  totalAmount: number
  createdAt: string
}

export interface CompanySummary {
  id: number
  uen: string | null
  name: string
  totalOutstandingSgd: number
}

export interface CompanyDetails extends CompanySummary {
  address?: string
  taxRegistrationNumber?: string
  documents?: CompanyDocumentSummary[]
}

export interface CompanySearchResponse {
  items: CompanySummary[]
  totalCount: number
}

export interface CompanyListQuery {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}
