import { useEffect, useState } from 'react'
import { Alert, Button, CircularProgress, Stack, Typography } from '@mui/material'
import { DocumentInfo } from './components/DocumentInfo'
import { getDocumentById } from './service/document-service'
import type { DocumentRecord } from './model/document-model'

interface DocumentDetailsPageProps {
  documentId: number
  onBack: () => void
}

export function DocumentDetailsPage({ documentId, onBack }: DocumentDetailsPageProps) {
  const [result, setResult] = useState<{
    documentId: number
    document?: DocumentRecord
    error?: string
  } | null>(null)

  useEffect(() => {
    let active = true
    getDocumentById(documentId)
      .then((result) => {
        if (active) setResult({ documentId, document: result })
      })
      .catch((reason: unknown) => {
        if (active) {
          setResult({
            documentId,
            error: reason instanceof Error ? reason.message : 'Document details could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [documentId])

  const loading = result?.documentId !== documentId
  const document = result?.documentId === documentId ? result.document : null
  const error = result?.documentId === documentId ? result.error : null

  return (
    <Stack spacing={2}>
      <div className="page-heading">
        <div>
          <Typography variant="h4" component="h1">Document details</Typography>
          <Typography color="text.secondary">View the original attachment and extracted information.</Typography>
        </div>
        <Button variant="outlined" onClick={onBack}>Back to documents</Button>
      </div>
      {loading && <CircularProgress aria-label="Loading document details" />}
      {error && <Alert severity="error">{error}</Alert>}
      {document && <DocumentInfo document={document} />}
    </Stack>
  )
}
