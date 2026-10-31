import { useEffect, useState } from 'react'
import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControlLabel,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { findCompaniesByUen } from '../../company/service/company-service'
import type { CompanySummary } from '../../company/model/company-model'
import { IconAction } from '../../../common/components/IconAction'
import type { DocumentRecord, DocumentUpdateRequest } from '../model/document-model'

interface DocumentInfoProps {
  document: DocumentRecord
  mode?: 'view' | 'edit'
  onSubmit?: (request: DocumentUpdateRequest) => Promise<void>
  onSaved?: () => void
  onCancel?: () => void
}

interface EditableItem {
  key: number
  id: number | null
  description: string
  quantity: string
  unitPrice: string
  amount: string
  currencyCode: string
  taxName: string
  taxRate: number
  taxAmount: number | null
}

const round2 = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100

function toNumber(value: string): number {
  return value.trim() === '' ? Number.NaN : Number(value)
}

function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('en-SG')
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

function formatDdMmmYyyy(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)
  return match ? `${match[3]}-${MONTHS[Number(match[2]) - 1] ?? match[2]}-${match[1]}` : ''
}

function formatAmount(value: number, currency = 'SGD'): string {
  return new Intl.NumberFormat('en-SG', { style: 'currency', currency }).format(value)
}

function getGoogleDrivePreviewUrl(url: URL): string | null {
  if (url.protocol !== 'https:' || url.hostname !== 'drive.google.com') return null
  const match = url.pathname.match(/^\/file\/d\/([\w-]+)/) ?? null
  const id = match?.[1] ?? url.searchParams.get('id')
  if (!id || !/^[\w-]+$/.test(id)) return null
  if (!match && url.pathname !== '/open' && url.pathname !== '/uc') return null
  return `https://drive.google.com/file/d/${id}/preview`
}

function getOriginalFile(document: DocumentRecord) {
  if (document.originalFileUrl) {
    let attachmentUrl: URL
    try {
      attachmentUrl = new URL(document.originalFileUrl, window.location.origin)
    } catch {
      return <Typography color="text.secondary">The original attachment link is invalid.</Typography>
    }
    if (!['http:', 'https:'].includes(attachmentUrl.protocol)) {
      return <Typography color="text.secondary">The original attachment link is invalid.</Typography>
    }
    const link = (
      <a href={attachmentUrl.href} target="_blank" rel="noreferrer">
        Open original attachment
      </a>
    )
    const drivePreviewUrl = getGoogleDrivePreviewUrl(attachmentUrl)
    if (drivePreviewUrl) {
      return (
        <iframe
          className="document-info__preview"
          src={drivePreviewUrl}
          title="Original document attachment"
          sandbox="allow-scripts allow-same-origin"
        />
      )
    }
    const urlContentType = document.originalFileContentType || ''
    if (urlContentType.startsWith('image/')) {
      return <img className="document-info__image" src={attachmentUrl.href} alt="Original document attachment" />
    }
    return (
      <object className="document-info__preview" data={attachmentUrl.href} type={urlContentType || 'application/pdf'}>
        {link}
      </object>
    )
  }

  if (!document.originalFileBase64) {
    return <Typography color="text.secondary">No original attachment is available.</Typography>
  }

  const providedContentType = document.originalFileContentType || ''
  const allowedContentTypes = ['application/pdf', 'image/jpeg', 'image/png']
  const contentType = allowedContentTypes.includes(providedContentType)
    ? providedContentType
    : 'application/octet-stream'
  const source = `data:${contentType};base64,${document.originalFileBase64}`
  if (contentType === 'application/octet-stream') {
    return <a href={source} download={`${document.refNumber}.attachment`}>Download original attachment</a>
  }
  return contentType.startsWith('image/') ? (
    <img className="document-info__image" src={source} alt="Original document attachment" />
  ) : (
    <object className="document-info__preview" data={source} type={contentType}>
      <a href={source} download={`${document.refNumber}.pdf`}>Download original attachment</a>
    </object>
  )
}

function toEditableItems(document: DocumentRecord): EditableItem[] {
  return document.documentItems.map((item, index) => ({
    key: index,
    id: item.id ?? null,
    description: item.description,
    quantity: String(item.quantity),
    unitPrice: String(item.unitPrice),
    amount: String(item.amount ?? round2(item.quantity * item.unitPrice)),
    currencyCode: item.currencyCode,
    taxName: item.taxName,
    taxRate: item.taxRate,
    taxAmount: item.taxAmount ?? null,
  }))
}

interface DocumentEditFieldsProps {
  document: DocumentRecord
  onSubmit?: (request: DocumentUpdateRequest) => Promise<void>
  onSaved?: () => void
  onCancel?: () => void
}

function DocumentEditFields({ document, onSubmit, onSaved, onCancel }: DocumentEditFieldsProps) {
  const [refNumber, setRefNumber] = useState(document.refNumber)
  const [sentDate, setSentDate] = useState(document.sentDate?.slice(0, 10) ?? '')
  const [dueDate, setDueDate] = useState(document.dueDate?.slice(0, 10) ?? '')
  const [uen, setUen] = useState(document.company.uen ?? '')
  const [typedCompanyName, setTypedCompanyName] = useState(document.company.name)
  const [taxAmount, setTaxAmount] = useState(String(document.subtotalTaxAmount))
  const [related, setRelated] = useState(document.relatedDocumentNumbers ?? '')
  const [manualOverrideFlag, setManualOverrideFlag] = useState(document.manualOverrideFlag ?? false)
  const [items, setItems] = useState<EditableItem[]>(() => toEditableItems(document))
  const [lookup, setLookup] = useState<{ uen: string; companies: CompanySummary[] } | null>(null)
  const [showErrors, setShowErrors] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const trimmedUen = uen.trim()

  useEffect(() => {
    if (!trimmedUen) return
    let active = true
    findCompaniesByUen(trimmedUen)
      .then((companies) => {
        if (active) setLookup({ uen: trimmedUen, companies })
      })
      .catch(() => {
        if (active) setLookup({ uen: trimmedUen, companies: [] })
      })
    return () => {
      active = false
    }
  }, [trimmedUen])

  const candidates = trimmedUen && lookup?.uen === trimmedUen ? lookup.companies : []
  const matchedCompany =
    candidates.find((company) => company.uen?.toLowerCase() === trimmedUen.toLowerCase()) ?? null
  const choices = matchedCompany ? [] : candidates.filter((company) => company.uen)
  const companyName = matchedCompany ? matchedCompany.name : typedCompanyName

  const subtotal = round2(items.reduce((sum, item) => sum + (toNumber(item.amount) || 0), 0))
  const tax = toNumber(taxAmount)
  const total = round2(subtotal + (Number.isFinite(tax) ? tax : 0))

  const validate = () => {
    const result: Record<string, string> = {}
    if (!refNumber.trim()) result.refNumber = 'Document number is required.'
    if (!sentDate) result.sentDate = 'Posting date is required.'
    if (!companyName.trim()) result.companyName = 'Company name is required when no UEN is provided.'
    if (!Number.isFinite(tax) || tax < 0) result.taxAmount = 'Tax amount must be zero or more.'
    items.forEach((item) => {
      if (!item.description.trim()) result[`description-${item.key}`] = 'Description is required.'
      const quantity = toNumber(item.quantity)
      if (!Number.isFinite(quantity) || quantity <= 0) result[`quantity-${item.key}`] = 'Quantity must be above zero.'
      const price = toNumber(item.unitPrice)
      if (!Number.isFinite(price) || price < 0) result[`unitPrice-${item.key}`] = 'Unit price must be zero or more.'
      if (!Number.isFinite(toNumber(item.amount))) result[`amount-${item.key}`] = 'Amount must be a number.'
    })
    return result
  }
  const errors = validate()

  const updateItem = (key: number, changes: Partial<EditableItem>) =>
    setItems((current) =>
      current.map((item) => {
        if (item.key !== key) return item
        const next = { ...item, ...changes }
        if (changes.quantity !== undefined || changes.unitPrice !== undefined) {
          const quantity = toNumber(next.quantity)
          const price = toNumber(next.unitPrice)
          if (Number.isFinite(quantity) && Number.isFinite(price)) next.amount = String(round2(quantity * price))
        }
        return next
      }),
    )

  const addItem = () =>
    setItems((current) => [
      ...current,
      {
        key: current.reduce((max, item) => Math.max(max, item.key), -1) + 1,
        id: null,
        description: '',
        quantity: '1',
        unitPrice: '0',
        amount: '0',
        currencyCode: current[0]?.currencyCode ?? 'SGD',
        taxName: current[0]?.taxName ?? 'GST',
        taxRate: current[0]?.taxRate ?? 0,
        taxAmount: null,
      },
    ])

  const removeItem = (key: number) => setItems((current) => current.filter((item) => item.key !== key))

  const requestSubmit = () => {
    setShowErrors(true)
    if (Object.keys(errors).length > 0) return
    setSubmitError(null)
    setConfirming(true)
  }

  const closeConfirmation = () => {
    if (submitting) return
    setConfirming(false)
    setSubmitError(null)
  }

  const confirmSubmit = async () => {
    if (!onSubmit) return
    setSubmitting(true)
    setSubmitError(null)
    try {
      await onSubmit({
        refNumber: refNumber.trim(),
        sentDate,
        dueDate: dueDate || null,
        companyUen: trimmedUen || null,
        companyName: companyName.trim(),
        subtotal,
        subtotalTaxAmount: tax,
        totalAmount: total,
        relatedDocumentNumbers: related.trim() || null,
        manualOverrideFlag: document.manualOverrideFlag == null && manualOverrideFlag ? true : null,
        documentItems: items.map((item, index) => ({
          id: item.id,
          description: item.description.trim(),
          quantity: toNumber(item.quantity),
          currencyCode: item.currencyCode,
          unitPrice: toNumber(item.unitPrice),
          taxName: item.taxName,
          taxRate: item.taxRate,
          serialNumber: index + 1,
          amount: toNumber(item.amount),
          taxAmount: item.taxAmount,
        })),
      })
      setConfirming(false)
      onSaved?.()
    } catch (reason: unknown) {
      setSubmitError(reason instanceof Error ? reason.message : 'The document could not be updated.')
    } finally {
      setSubmitting(false)
    }
  }

  const error = (name: string) => (showErrors ? errors[name] : undefined)

  return (
    <Stack spacing={2} component="form" noValidate onSubmit={(event) => { event.preventDefault(); requestSubmit() }}>
      <TextField
        label="Document number" size="small" value={refNumber} required
        onChange={(event) => setRefNumber(event.target.value)}
        error={!!error('refNumber')} helperText={error('refNumber')}
      />
      <TextField
        label="Posting date" size="small" type="date" value={sentDate} required
        onChange={(event) => setSentDate(event.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
        error={!!error('sentDate')} helperText={error('sentDate')}
      />
      <TextField
        label="Due date" size="small" type="date" value={dueDate}
        onChange={(event) => setDueDate(event.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
        helperText={formatDdMmmYyyy(dueDate) || undefined}
      />
      <TextField
        label="Company UEN" size="small" value={uen}
        onChange={(event) => setUen(event.target.value)}
      />
      {choices.length > 1 && (
        <TextField
          label="Matching companies" size="small" select value=""
          onChange={(event) => setUen(event.target.value)}
          slotProps={{ select: { native: true }, inputLabel: { shrink: true } }}
        >
          <option value="">Select a company…</option>
          {choices.map((company) => (
            <option key={company.id} value={company.uen ?? ''}>
              {company.name} ({company.uen})
            </option>
          ))}
        </TextField>
      )}
      <TextField
        label="Company name" size="small" value={companyName} required
        onChange={(event) => setTypedCompanyName(event.target.value)}
        disabled={matchedCompany !== null}
        helperText={error('companyName') ?? (matchedCompany ? 'Name follows the registered company for this UEN.' : undefined)}
        error={!!error('companyName')}
      />
      <TextField
        label="Related documents" size="small" value={related}
        onChange={(event) => setRelated(event.target.value)}
      />
      <TextField
        label="Tax amount" size="small" value={taxAmount} required
        onChange={(event) => setTaxAmount(event.target.value)}
        error={!!error('taxAmount')} helperText={error('taxAmount')}
        slotProps={{ htmlInput: { inputMode: 'decimal' } }}
      />
      <dl className="document-info__fields">
        <dt>Document type</dt><dd>{document.typeCode || '—'}</dd>
        <dt>Subtotal</dt><dd>{formatAmount(subtotal)}</dd>
        <dt>Total amount</dt><dd>{formatAmount(total)}</dd>
        <dt>Manual override</dt>
        <dd>
          <FormControlLabel
            control={
              <Checkbox
                checked={manualOverrideFlag}
                disabled={document.manualOverrideFlag != null}
                onChange={(event) => setManualOverrideFlag(event.target.checked)}
              />
            }
            label="Manual override"
          />
        </dd>
        <dt>Manual override by</dt><dd>{document.manualOverrideBy ?? '-'}</dd>
        <dt>Confidence level</dt>
        <dd>{document.confidence == null ? '—' : `${Math.round(document.confidence * 100)}%`}</dd>
      </dl>
      <Typography variant="subtitle1" component="h3">Line items</Typography>
      <Table size="small" aria-label="Editable line items" className="document-info__items">
        <TableHead>
          <TableRow>
            <TableCell>Description</TableCell>
            <TableCell>Quantity</TableCell>
            <TableCell>Unit price</TableCell>
            <TableCell>Amount</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((item, index) => (
            <TableRow key={item.key}>
              <TableCell data-label="Description">
                <TextField
                  size="small" value={item.description} multiline minRows={2}
                  onChange={(event) => updateItem(item.key, { description: event.target.value })}
                  error={!!error(`description-${item.key}`)} helperText={error(`description-${item.key}`)}
                  slotProps={{ htmlInput: { 'aria-label': `Line item ${index + 1} description` } }}
                />
              </TableCell>
              <TableCell data-label="Quantity">
                <TextField
                  size="small" value={item.quantity}
                  onChange={(event) => updateItem(item.key, { quantity: event.target.value })}
                  error={!!error(`quantity-${item.key}`)} helperText={error(`quantity-${item.key}`)}
                  slotProps={{ htmlInput: { 'aria-label': `Line item ${index + 1} quantity`, inputMode: 'decimal' } }}
                />
              </TableCell>
              <TableCell data-label="Unit price">
                <TextField
                  size="small" value={item.unitPrice}
                  onChange={(event) => updateItem(item.key, { unitPrice: event.target.value })}
                  error={!!error(`unitPrice-${item.key}`)} helperText={error(`unitPrice-${item.key}`)}
                  slotProps={{ htmlInput: { 'aria-label': `Line item ${index + 1} unit price`, inputMode: 'decimal' } }}
                />
              </TableCell>
              <TableCell data-label="Amount">
                <TextField
                  size="small" value={item.amount}
                  onChange={(event) => updateItem(item.key, { amount: event.target.value })}
                  error={!!error(`amount-${item.key}`)} helperText={error(`amount-${item.key}`)}
                  slotProps={{ htmlInput: { 'aria-label': `Line item ${index + 1} amount`, inputMode: 'decimal' } }}
                />
              </TableCell>
              <TableCell data-label="Actions">
                <IconAction kind="delete" label={`Remove line item ${index + 1}`} onClick={() => removeItem(item.key)} />
              </TableCell>
            </TableRow>
          ))}
          {items.length === 0 && (
            <TableRow><TableCell colSpan={5}>No line items available.</TableCell></TableRow>
          )}
        </TableBody>
      </Table>
      <div>
        <Button onClick={addItem} aria-label="Add line item">Add</Button>
      </div>
      <Stack direction="row" spacing={1}>
        <Button type="submit" variant="contained">Submit</Button>
        <Button type="button" onClick={onCancel}>Cancel</Button>
      </Stack>
      <Dialog open={confirming} onClose={closeConfirmation}>
        <DialogTitle>Confirm modification</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to save the changes to document {refNumber}?
          </DialogContentText>
          {submitError && <Alert severity="error">{submitError}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeConfirmation} disabled={submitting}>Cancel</Button>
          <Button onClick={confirmSubmit} variant="contained" disabled={submitting}>Confirm</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

export function DocumentInfo({ document, mode = 'view', onSubmit, onSaved, onCancel }: DocumentInfoProps) {
  const editing = mode === 'edit'
  return (
    <div className="document-info">
      <Paper variant="outlined" className="document-info__column">
        <Typography variant="h6" component="h2">Original attachment</Typography>
        {getOriginalFile(document)}
      </Paper>
      <Paper variant="outlined" className="document-info__column">
        <Typography variant="h6" component="h2">Extracted information</Typography>
        {editing ? (
          <DocumentEditFields document={document} onSubmit={onSubmit} onSaved={onSaved} onCancel={onCancel} />
        ) : (
          <>
            <dl className="document-info__fields">
              <dt>Document number</dt><dd>{document.refNumber}</dd>
              <dt>Company UEN</dt><dd>{document.company.uen || '—'}</dd>
              <dt>Company name</dt><dd>{document.company.name}</dd>
              <dt>Document type</dt><dd>{document.typeCode || '—'}</dd>
              <dt>Posting date</dt><dd>{formatDate(document.sentDate)}</dd>
              <dt>Due date</dt><dd>{formatDate(document.dueDate)}</dd>
              <dt>Subtotal</dt><dd>{formatAmount(document.subtotal)}</dd>
              <dt>Tax</dt><dd>{formatAmount(document.subtotalTaxAmount)}</dd>
              <dt>Total amount</dt><dd>{formatAmount(document.totalAmount)}</dd>
              <dt>Confidence level</dt>
              <dd>{document.confidence == null ? '—' : `${Math.round(document.confidence * 100)}%`}</dd>
              <dt>Manual override</dt>
              <dd>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={document.manualOverrideFlag ?? false}
                      disabled
                    />
                  }
                  label="Manual override"
                />
              </dd>
              <dt>Manual override by</dt><dd>{document.manualOverrideBy ?? '-'}</dd>
              <dt>Related documents</dt><dd>{document.relatedDocumentNumbers || '—'}</dd>
            </dl>
            <Typography variant="subtitle1" component="h3">Line items</Typography>
            <Table size="small" aria-label="Document line items" className="document-info__items">
              <TableHead>
                <TableRow><TableCell>Description</TableCell><TableCell>Quantity</TableCell><TableCell align="right">Amount</TableCell></TableRow>
              </TableHead>
              <TableBody>
                {document.documentItems.map((item, index) => (
                  <TableRow key={item.id ?? item.serialNumber ?? index}>
                    <TableCell data-label="Description">{item.description}</TableCell>
                    <TableCell data-label="Quantity">{item.quantity}</TableCell>
                    <TableCell align="right" data-label="Amount">
                      {formatAmount(item.amount ?? item.quantity * item.unitPrice, item.currencyCode)}
                    </TableCell>
                  </TableRow>
                ))}
                {document.documentItems.length === 0 && (
                  <TableRow><TableCell colSpan={3}>No line items available.</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </>
        )}
      </Paper>
    </div>
  )
}
