export interface DocumentCompany {
  id: number
  uen: string | null
  name: string
}

export interface DocumentItem {
  id?: number | null
  description: string
  quantity: number
  currencyCode: string
  unitPrice: number
  taxName: string
  taxRate: number
  serialNumber?: number | null
  amount?: number | null
  taxAmount?: number | null
}

export interface DocumentRecord {
  id: number
  refNumber: string
  typeCode: string
  company: DocumentCompany
  sentDate: string
  dueDate?: string | null
  totalAmount: number
  confidence?: number | null
  manualOverrideFlag?: boolean | null
  manualOverrideBy?: string | null
  relatedDocumentNumbers?: string | null
  subtotal: number
  subtotalTaxRate: number
  subtotalTaxAmount: number
  createdAt?: string
  documentItems: DocumentItem[]
  originalFileUrl?: string | null
  originalFileBase64?: string | null
  originalFileContentType?: string | null
}

export interface DocumentUpdateItem {
  id?: number | null
  description: string
  quantity: number
  currencyCode: string
  unitPrice: number
  taxName: string
  taxRate: number
  serialNumber?: number | null
  amount?: number | null
  taxAmount?: number | null
}

export interface DocumentUpdateRequest {
  refNumber: string
  sentDate: string
  companyUen: string | null
  companyName: string
  subtotal: number
  subtotalTaxAmount: number
  totalAmount: number
  relatedDocumentNumbers: string | null
  documentItems: DocumentUpdateItem[]
  manualOverrideFlag?: boolean | null
}

export interface DocumentSearchResponse {
  items: DocumentRecord[]
  totalCount: number
}

export interface DocumentListQuery {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}
