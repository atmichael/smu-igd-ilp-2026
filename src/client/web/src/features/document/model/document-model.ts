export interface DocumentCompany {
  id: number
  uen: string | null
  name: string
  email?: string | null
  address?: string | null
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
  apJournal?: DocumentAPJournal | null
}

export interface DocumentAPSuggestion {
  lineIndex: number
  accountCode: string
  reason: string
  requiresReview?: boolean
}

export interface DocumentAPJournal {
  confidence: number | null
  gstCode: string
  coaSuggestions: DocumentAPSuggestion[]
  verificationNotes: string[]
}

export interface AccountsPayableReferenceData {
  accounts: Array<{
    code: string
    name: string
    apRole: string
    enabled: boolean
  }>
  gstCodes: Array<{ code: string; description: string }>
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
  dueDate?: string | null
  companyUen: string | null
  companyName: string
  subtotal: number
  subtotalTaxAmount: number
  totalAmount: number
  relatedDocumentNumbers: string | null
  documentItems: DocumentUpdateItem[]
  manualOverrideFlag?: boolean | null
  apJournal?: {
    gstCode: string
    coaSuggestions: Array<{ lineIndex: number; accountCode: string; reason: string }>
  } | null
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
