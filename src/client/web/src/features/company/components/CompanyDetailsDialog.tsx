import { useEffect, useState } from 'react'
import { Alert, CircularProgress, Dialog, DialogContent, DialogTitle, IconButton } from '@mui/material'
import { CompanyInfo } from './CompanyInfo'
import { getCompanyById } from '../service/company-service'
import type { CompanyDetails } from '../model/company-model'

interface CompanyDetailsDialogProps {
  companyId: number | null
  onClose: () => void
}

export function CompanyDetailsDialog({ companyId, onClose }: CompanyDetailsDialogProps) {
  const [result, setResult] = useState<{
    companyId: number
    company?: CompanyDetails
    error?: string
  } | null>(null)

  useEffect(() => {
    if (companyId === null) return
    let active = true
    getCompanyById(companyId)
      .then((result) => {
        if (active) setResult({ companyId, company: result })
      })
      .catch((reason: unknown) => {
        if (active) {
          setResult({
            companyId,
            error: reason instanceof Error ? reason.message : 'Company details could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [companyId])

  const loading = companyId !== null && result?.companyId !== companyId
  const company = result?.companyId === companyId ? result.company : null
  const error = result?.companyId === companyId ? result.error : null

  return (
    <Dialog open={companyId !== null} onClose={onClose} fullWidth maxWidth="md" aria-labelledby="company-dialog-title">
      <DialogTitle id="company-dialog-title">
        Company details
        <IconButton aria-label="Close company details" onClick={onClose} className="dialog-close">
          ×
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        {loading && <CircularProgress aria-label="Loading company details" />}
        {error && <Alert severity="error">{error}</Alert>}
        {company && <CompanyInfo company={company} />}
      </DialogContent>
    </Dialog>
  )
}
