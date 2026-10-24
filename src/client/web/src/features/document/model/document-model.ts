export interface DocumentCompany {
  id: number
  uen: string
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

export interface DocumentSearchResponse {
  items: DocumentRecord[]
  totalCount: number
}
