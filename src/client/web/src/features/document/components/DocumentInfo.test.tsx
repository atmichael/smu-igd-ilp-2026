import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DocumentInfo } from './DocumentInfo'
import type { DocumentRecord } from '../model/document-model'

const base: DocumentRecord = {
  id: 1,
  refNumber: 'INV-001',
  typeCode: 'INV',
  company: { id: 1, uen: '111111111A', name: 'Alpha Ltd' },
  sentDate: '2026-10-01',
  dueDate: '2026-10-31',
  totalAmount: 100,
  confidence: 0.42,
  subtotal: 100,
  subtotalTaxRate: 0,
  subtotalTaxAmount: 0,
  documentItems: [],
}

describe('DocumentInfo original attachment', () => {
  it('renders a preview object for a URL pdf', () => {
    const { container } = render(
      <DocumentInfo document={{ ...base, originalFileUrl: 'https://example.com/a.pdf', originalFileContentType: 'application/pdf' }} />,
    )
    const object = container.querySelector('object.document-info__preview')
    expect(object?.getAttribute('data')).toBe('https://example.com/a.pdf')
  })

  it('renders an image for a URL image', () => {
    render(<DocumentInfo document={{ ...base, originalFileUrl: 'https://example.com/a.png', originalFileContentType: 'image/png' }} />)
    expect(screen.getByAltText('Original document attachment').getAttribute('src')).toBe('https://example.com/a.png')
  })

  it('rejects unsafe URLs', () => {
    render(<DocumentInfo document={{ ...base, originalFileUrl: 'javascript:alert(1)' }} />)
    expect(screen.getByText('The original attachment link is invalid.')).toBeTruthy()
  })
})
