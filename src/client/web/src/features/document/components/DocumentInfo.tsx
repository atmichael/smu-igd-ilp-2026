import { Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import type { DocumentRecord } from '../model/document-model'

interface DocumentInfoProps {
  document: DocumentRecord
}

function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('en-SG')
}

function formatAmount(value: number, currency = 'SGD'): string {
  return new Intl.NumberFormat('en-SG', { style: 'currency', currency }).format(value)
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

export function DocumentInfo({ document }: DocumentInfoProps) {
  return (
    <div className="document-info">
      <Paper variant="outlined" className="document-info__column">
        <Typography variant="h6" component="h2">Original attachment</Typography>
        {getOriginalFile(document)}
      </Paper>
      <Paper variant="outlined" className="document-info__column">
        <Typography variant="h6" component="h2">Extracted information</Typography>
        <dl className="document-info__fields">
          <dt>Document number</dt><dd>{document.refNumber}</dd>
          <dt>Company</dt><dd>{document.company.name} ({document.company.uen})</dd>
          <dt>Document type</dt><dd>{document.typeCode || '—'}</dd>
          <dt>Posting date</dt><dd>{formatDate(document.sentDate)}</dd>
          <dt>Due date</dt><dd>{formatDate(document.dueDate)}</dd>
          <dt>Subtotal</dt><dd>{formatAmount(document.subtotal)}</dd>
          <dt>Tax</dt><dd>{formatAmount(document.subtotalTaxAmount)}</dd>
          <dt>Total amount</dt><dd>{formatAmount(document.totalAmount)}</dd>
          <dt>Confidence level</dt>
          <dd>{document.confidence == null ? '—' : `${Math.round(document.confidence * 100)}%`}</dd>
          <dt>Related documents</dt><dd>{document.relatedDocumentNumbers || '—'}</dd>
        </dl>
        <Typography variant="subtitle1" component="h3">Line items</Typography>
        <Table size="small" aria-label="Document line items">
          <TableHead>
            <TableRow><TableCell>Description</TableCell><TableCell>Quantity</TableCell><TableCell align="right">Amount</TableCell></TableRow>
          </TableHead>
          <TableBody>
            {document.documentItems.map((item, index) => (
              <TableRow key={item.id ?? item.serialNumber ?? index}>
                <TableCell>{item.description}</TableCell>
                <TableCell>{item.quantity}</TableCell>
                <TableCell align="right">
                  {formatAmount(item.amount ?? item.quantity * item.unitPrice, item.currencyCode)}
                </TableCell>
              </TableRow>
            ))}
            {document.documentItems.length === 0 && (
              <TableRow><TableCell colSpan={3}>No line items available.</TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>
    </div>
  )
}
