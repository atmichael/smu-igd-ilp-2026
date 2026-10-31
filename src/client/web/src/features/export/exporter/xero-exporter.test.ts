import { describe, expect, it } from 'vitest'
import type { DocumentRecord } from '../../document/model/document-model'
import { createExporter } from './exporter-factory'
import { formatXeroDate, xeroHeaders } from './xero-exporter'

const document: DocumentRecord = {
  id: 1,
  refNumber: 'INV-1',
  typeCode: 'Invoice',
  company: { id: 1, uen: 'UEN-1', name: 'Acme, Ltd', email: 'ap@acme.example', address: '1 Road' },
  sentDate: '2026-10-04T00:00:00',
  dueDate: '2026-11-03T00:00:00',
  totalAmount: 21.8,
  subtotal: 20,
  subtotalTaxRate: 0.09,
  subtotalTaxAmount: 1.8,
  relatedDocumentNumbers: 'PO-1',
  documentItems: [
    { description: 'Pallet', quantity: 2, currencyCode: 'USD', unitPrice: 10, taxName: 'GST', taxRate: 0.09, amount: 20, taxAmount: 1.8 },
    { description: 'Service', quantity: 1, currencyCode: 'USD', unitPrice: 5, taxName: 'GST', taxRate: 0.09 },
  ],
}

describe('xero exporter', () => {
  const exporter = createExporter('xero')

  it('formats dates as DD/MM/YYYY', () => {
    expect(formatXeroDate('2026-10-04T00:00:00')).toBe('04/10/2026')
    expect(formatXeroDate(null)).toBe('')
  })

  it('writes the Xero header and one row per line item', () => {
    const lines = exporter.toCsv([document]).trimEnd().split('\r\n')

    expect(lines[0]).toBe(xeroHeaders.join(','))
    expect(lines).toHaveLength(3)
    expect(lines[1]).toBe(
      '"Acme, Ltd",ap@acme.example,1 Road,,,,,,,,INV-1,PO-1,04/10/2026,03/11/2026,,Pallet,2,10,,GST,1.8,,,,,USD',
    )
    expect(lines[2].split(',')[21]).toBe('0.45')
  })

  it('writes a single summary row for documents without line items', () => {
    const lines = exporter.toCsv([{ ...document, documentItems: [] }]).trimEnd().split('\r\n')

    expect(lines).toHaveLength(2)
    expect(lines[1]).toContain(',INV-1,')
    expect(lines[1]).toContain(',1,20,')
  })

  it('names the file by generation date', () => {
    expect(exporter.fileName(new Date('2026-10-31T01:00:00Z'))).toBe('xero-bills-2026-10-31.csv')
  })

  it('rejects unsupported providers', () => {
    expect(() => createExporter('other' as never)).toThrow(/unsupported/i)
  })
})
