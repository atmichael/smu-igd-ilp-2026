import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { CompanyInfo } from './CompanyInfo'
import type { CompanyDetails } from '../model/company-model'

const company: CompanyDetails = {
  id: 1,
  uen: '201234567A',
  name: 'Example Pte Ltd',
  totalOutstandingSgd: 25,
  documents: [
    { id: 1, refNumber: 'INV-OLD', sentDate: '2026-08-01', totalAmount: 10, createdAt: '2026-08-02' },
    { id: 2, refNumber: 'INV-NEW', sentDate: '2026-09-01', totalAmount: 15, createdAt: '2026-09-02' },
  ],
}

describe('CompanyInfo', () => {
  it('displays company fields and orders recent documents newest first', () => {
    render(<CompanyInfo company={company} />)

    expect(screen.getByText('Example Pte Ltd')).toBeInTheDocument()
    expect(screen.getByText('201234567A')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Recent documents (2)' })).toBeInTheDocument()
    const list = screen.getByRole('list')
    const rows = within(list).getAllByRole('listitem')
    expect(within(rows[0]).getByText('INV-NEW')).toBeInTheDocument()
    expect(within(rows[1]).getByText('INV-OLD')).toBeInTheDocument()
  })
})
