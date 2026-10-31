import type { DocumentItem, DocumentRecord } from '../../document/model/document-model'
import { toCsv } from './csv'
import type { DocumentExporter } from './export-provider'

export const xeroHeaders = [
  '*ContactName',
  'EmailAddress',
  'POAddressLine1',
  'POAddressLine2',
  'POAddressLine3',
  'POAddressLine4',
  'POCity',
  'PORegion',
  'POPostalCode',
  'POCountry',
  '*InvoiceNumber',
  'Reference',
  '*InvoiceDate',
  '*DueDate',
  'InventoryItemCode',
  '*Description',
  '*Quantity',
  '*UnitAmount',
  '*AccountCode',
  '*TaxType',
  'TaxAmount',
  'TrackingName1',
  'TrackingOption1',
  'TrackingName2',
  'TrackingOption2',
  'Currency',
] as const

export function formatXeroDate(value: string | null | undefined): string {
  const match = value?.match(/^(\d{4})-(\d{2})-(\d{2})/)
  return match ? `${match[3]}/${match[2]}/${match[1]}` : ''
}

function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}

function lineItemsOf(document: DocumentRecord): DocumentItem[] {
  if (document.documentItems.length > 0) return document.documentItems
  return [
    {
      description: document.refNumber,
      quantity: 1,
      currencyCode: 'SGD',
      unitPrice: document.subtotal,
      taxName: 'GST',
      taxRate: document.subtotalTaxRate,
      amount: document.subtotal,
      taxAmount: document.subtotalTaxAmount,
    },
  ]
}

function toRows(document: DocumentRecord): Array<Array<string | number>> {
  return lineItemsOf(document).map((item) => {
    const amount = item.amount ?? item.quantity * item.unitPrice
    return [
      document.company.name,
      document.company.email ?? '',
      document.company.address ?? '',
      '',
      '',
      '',
      '',
      '',
      '',
      '',
      document.refNumber,
      document.relatedDocumentNumbers ?? '',
      formatXeroDate(document.sentDate),
      formatXeroDate(document.dueDate ?? document.sentDate),
      '',
      item.description,
      item.quantity,
      item.unitPrice,
      '',
      item.taxName,
      item.taxAmount ?? round2(amount * item.taxRate),
      '',
      '',
      '',
      '',
      item.currencyCode,
    ]
  })
}

export const xeroExporter: DocumentExporter = {
  id: 'xero',
  label: 'Xero',
  fileName: (generatedAt) => `xero-bills-${generatedAt.toISOString().slice(0, 10)}.csv`,
  toCsv: (documents) => toCsv(xeroHeaders, documents.flatMap(toRows)),
}
