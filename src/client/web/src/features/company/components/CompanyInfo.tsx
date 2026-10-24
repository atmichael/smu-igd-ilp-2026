import { Paper, Stack, Typography } from '@mui/material'
import type { CompanyDetails } from '../model/company-model'

interface CompanyInfoProps {
  company: CompanyDetails
}

function formatDate(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('en-SG')
}

export function CompanyInfo({ company }: CompanyInfoProps) {
  const documents = [...(company.documents ?? [])].sort(
    (left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
  )

  return (
    <Stack spacing={2} className="company-info">
      <Paper variant="outlined" className="company-info__section">
        <Typography variant="h5" component="h2">{company.name}</Typography>
        <dl className="company-info__fields">
          <dt>UEN</dt><dd>{company.uen}</dd>
          <dt>Address</dt><dd>{company.address || '—'}</dd>
          <dt>Tax registration number</dt><dd>{company.taxRegistrationNumber || '—'}</dd>
          <dt>Total outstanding (SGD)</dt>
          <dd>{new Intl.NumberFormat('en-SG', { style: 'currency', currency: 'SGD' }).format(company.totalOutstandingSgd)}</dd>
        </dl>
      </Paper>
      <Paper variant="outlined" className="company-info__section">
        <Typography variant="h6" component="h3">
          Recent documents ({documents.length})
        </Typography>
        {documents.length === 0 ? (
          <Typography color="text.secondary">No documents are available for this company.</Typography>
        ) : (
          <ul className="company-info__documents">
            {documents.map((document) => (
              <li key={document.id}>
                <span>{document.refNumber}</span>
                <span>{formatDate(document.createdAt)}</span>
                <span>
                  {new Intl.NumberFormat('en-SG', { style: 'currency', currency: 'SGD' }).format(document.totalAmount)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </Paper>
    </Stack>
  )
}
