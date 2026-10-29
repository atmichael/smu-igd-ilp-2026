import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { DocumentInfo } from './DocumentInfo'
import { findCompaniesByUen } from '../../company/service/company-service'
import type { DocumentRecord } from '../model/document-model'

vi.mock('../../company/service/company-service', () => ({
  findCompaniesByUen: vi.fn(),
}))

const base: DocumentRecord = {
  id: 1,
  refNumber: 'INV-001',
  typeCode: 'INV',
  company: { id: 1, uen: '111111111A', name: 'Alpha Ltd' },
  sentDate: '2026-10-01',
  dueDate: '2026-10-31',
  totalAmount: 100,
  confidence: 0.42,
  manualOverrideFlag: null,
  manualOverrideBy: null,
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

  it('renders a Google Drive preview iframe for a Drive view URL', () => {
    render(
      <DocumentInfo
        document={{ ...base, originalFileUrl: 'https://drive.google.com/file/d/1pPrdCXnnGl4LY_4QbbKXNvSr64FhJ4yi/view?usp=drivesdk' }}
      />,
    )
    expect(screen.getByTitle('Original document attachment').getAttribute('src')).toBe(
      'https://drive.google.com/file/d/1pPrdCXnnGl4LY_4QbbKXNvSr64FhJ4yi/preview',
    )
  })

  it('labels document line items for the mobile card layout', () => {
    render(
      <DocumentInfo
        document={{
          ...base,
          documentItems: [
            { id: 1, description: 'Widget', quantity: 2, currencyCode: 'SGD', unitPrice: 10, taxName: 'GST', taxRate: 9, amount: 20 },
          ],
        }}
      />,
    )

    const table = screen.getByRole('table', { name: 'Document line items' })
    expect(Array.from(table.querySelectorAll('tbody [data-label]')).map((cell) => cell.getAttribute('data-label'))).toEqual([
      'Description',
      'Quantity',
      'Amount',
    ])
  })
})

describe('DocumentInfo edit mode', () => {
  const editable: DocumentRecord = {
    ...base,
    subtotal: 20,
    subtotalTaxAmount: 2,
    totalAmount: 22,
    documentItems: [
      { id: 9, description: 'Widget', quantity: 2, currencyCode: 'SGD', unitPrice: 10, taxName: 'GST', taxRate: 9, amount: 20 },
    ],
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(findCompaniesByUen).mockResolvedValue([])
  })

  const change = (label: string | RegExp, value: string) =>
    fireEvent.change(screen.getByLabelText(label), { target: { value } })

  it('auto-calculates amount, subtotal and total from line items and tax', () => {
    render(<DocumentInfo document={editable} mode="edit" />)

    change(/line item 1 quantity/i, '3')
    expect((screen.getByLabelText(/line item 1 amount/i) as HTMLInputElement).value).toBe('30')
    fireEvent.click(screen.getByRole('button', { name: /add line item/i }))
    change(/line item 2 description/i, 'Gadget')
    change(/line item 2 unit price/i, '5')
    change(/tax amount/i, '3')

    const fields = screen.getByText('Subtotal').nextElementSibling
    expect(fields?.textContent).toContain('35.00')
    expect(screen.getByText('Total amount').nextElementSibling?.textContent).toContain('38.00')

    fireEvent.click(screen.getByRole('button', { name: /remove line item 2/i }))
    expect(screen.getByText('Subtotal').nextElementSibling?.textContent).toContain('30.00')
  })

  it('labels editable line item fields for the mobile card layout', () => {
    render(<DocumentInfo document={editable} mode="edit" />)

    const table = screen.getByRole('table', { name: 'Editable line items' })
    expect(Array.from(table.querySelectorAll('tbody [data-label]')).map((cell) => cell.getAttribute('data-label'))).toEqual([
      'Description',
      'Quantity',
      'Unit price',
      'Amount',
      'Actions',
    ])
  })

  it('makes the due date editable and submits the new value', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<DocumentInfo document={editable} mode="edit" onSubmit={onSubmit} />)

    expect((screen.getByLabelText('Due date') as HTMLInputElement).value).toBe('2026-10-31')
    expect(screen.getByText('31-Oct-2026')).toBeInTheDocument()
    change('Due date', '2026-12-05')
    expect(screen.getByText('05-Dec-2026')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /^submit$/i }))
    fireEvent.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ dueDate: '2026-12-05' }),
    ))
  })

  it('allows marking a document verified and submits the override flag', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<DocumentInfo document={editable} mode="edit" onSubmit={onSubmit} />)

    const checkbox = screen.getByRole('checkbox', { name: 'Manual override' }) as HTMLInputElement
    expect(checkbox.checked).toBe(false)
    expect(checkbox.disabled).toBe(false)
    expect(screen.getByText('-', { selector: 'dd' })).toBeInTheDocument()
    fireEvent.click(checkbox)
    fireEvent.click(screen.getByRole('button', { name: /^submit$/i }))
    fireEvent.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ manualOverrideFlag: true }),
    ))
  })

  it('shows and locks existing manual verification details', () => {
    render(
      <DocumentInfo
        document={{ ...editable, manualOverrideFlag: true, manualOverrideBy: 'reviewer@example.com' }}
        mode="edit"
      />,
    )

    const checkbox = screen.getByRole('checkbox', { name: 'Manual override' }) as HTMLInputElement
    expect(checkbox.checked).toBe(true)
    expect(checkbox.disabled).toBe(true)
    expect(screen.getByText('reviewer@example.com')).toBeInTheDocument()
  })

  it('locks the company name to the registered company when the UEN exists', async () => {
    vi.mocked(findCompaniesByUen).mockResolvedValue([{ id: 5, uen: '555E', name: 'Registered Co', totalOutstandingSgd: 0 }])
    render(<DocumentInfo document={editable} mode="edit" />)

    change('Company UEN', '555E')

    await waitFor(() => expect((screen.getByLabelText(/company name/i) as HTMLInputElement).value).toBe('Registered Co'))
    expect((screen.getByLabelText(/company name/i) as HTMLInputElement).disabled).toBe(true)

    change('Company UEN', '')
    expect((screen.getByLabelText(/company name/i) as HTMLInputElement).disabled).toBe(false)
  })

  it('offers a drop down of matching companies when several match the UEN', async () => {
    vi.mocked(findCompaniesByUen).mockResolvedValue([
      { id: 1, uen: '555E', name: 'Registered Co', totalOutstandingSgd: 0 },
      { id: 2, uen: '555F', name: 'Other Co', totalOutstandingSgd: 0 },
    ])
    render(<DocumentInfo document={editable} mode="edit" />)

    change('Company UEN', '555')
    const dropdown = await screen.findByLabelText('Matching companies')
    expect(screen.getByRole('option', { name: 'Other Co (555F)' })).toBeInTheDocument()
    expect((screen.getByLabelText(/company name/i) as HTMLInputElement).disabled).toBe(false)

    fireEvent.change(dropdown, { target: { value: '555F' } })
    vi.mocked(findCompaniesByUen).mockResolvedValue([{ id: 2, uen: '555F', name: 'Other Co', totalOutstandingSgd: 0 }])

    expect((screen.getByLabelText('Company UEN') as HTMLInputElement).value).toBe('555F')
    await waitFor(() => expect((screen.getByLabelText(/company name/i) as HTMLInputElement).value).toBe('Other Co'))
    expect(screen.queryByLabelText('Matching companies')).not.toBeInTheDocument()
  })

  it('calls onCancel without submitting when Cancel is clicked', () => {
    const onSubmit = vi.fn()
    const onCancel = vi.fn()
    render(<DocumentInfo document={editable} mode="edit" onSubmit={onSubmit} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: /^cancel$/i }))

    expect(onCancel).toHaveBeenCalled()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('shows the original attachment preview in edit mode', () => {
    render(
      <DocumentInfo
        document={{ ...editable, originalFileUrl: 'https://example.com/a.png', originalFileContentType: 'image/png' }}
        mode="edit"
      />,
    )
    expect(screen.getByAltText('Original document attachment')).toBeInTheDocument()
  })

  it('validates input before asking for confirmation', () => {
    const onSubmit = vi.fn()
    render(<DocumentInfo document={editable} mode="edit" onSubmit={onSubmit} />)

    change(/^document number/i, ' ')
    fireEvent.click(screen.getByRole('button', { name: /^submit$/i }))

    expect(screen.getByText('Document number is required.')).toBeInTheDocument()
    expect(screen.queryByText('Confirm modification')).not.toBeInTheDocument()
  })

  it('keeps editing when the confirmation is cancelled and submits only after confirming', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    const onSaved = vi.fn()
    render(<DocumentInfo document={editable} mode="edit" onSubmit={onSubmit} onSaved={onSaved} />)

    change(/^document number/i, 'INV-002')
    fireEvent.click(screen.getByRole('button', { name: /^submit$/i }))
    fireEvent.click(screen.getByRole('button', { name: /cancel/i }))
    await waitFor(() => expect(screen.queryByText('Confirm modification')).not.toBeInTheDocument())
    expect(onSubmit).not.toHaveBeenCalled()
    expect((screen.getByLabelText(/^document number/i) as HTMLInputElement).value).toBe('INV-002')

    fireEvent.click(screen.getByRole('button', { name: /^submit$/i }))
    fireEvent.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => expect(onSaved).toHaveBeenCalled())
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        refNumber: 'INV-002',
        companyUen: '111111111A',
        subtotal: 20,
        subtotalTaxAmount: 2,
        totalAmount: 22,
        documentItems: [expect.objectContaining({ id: 9, description: 'Widget', serialNumber: 1, amount: 20 })],
      }),
    )
  })

  it('stays open and shows the error when the API call fails', async () => {
    const onSaved = vi.fn()
    render(
      <DocumentInfo document={editable} mode="edit" onSubmit={vi.fn().mockRejectedValue(new Error('boom'))} onSaved={onSaved} />,
    )

    fireEvent.click(screen.getByRole('button', { name: /^submit$/i }))
    fireEvent.click(screen.getByRole('button', { name: /confirm/i }))

    expect(await screen.findByText('boom')).toBeInTheDocument()
    expect(onSaved).not.toHaveBeenCalled()
  })
})
