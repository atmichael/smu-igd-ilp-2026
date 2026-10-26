import { useCallback, useEffect, useState } from 'react'
import { Alert, CircularProgress, Stack, Typography } from '@mui/material'
import { DocumentDataGrid } from '../document/components/DocumentDataGrid'
import { deleteDocument, getDocumentsBelowThreshold } from '../document/service/document-service'
import type { DocumentRecord } from '../document/model/document-model'

interface HomePageProps {
  onNavigate: (path: string) => void
}

export function HomePage({ onNavigate }: HomePageProps) {
  const [documents, setDocuments] = useState<DocumentRecord[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionMessage, setActionMessage] = useState<string | null>(null)

  const [reloadCount, setReloadCount] = useState(0)

  useEffect(() => {
    let active = true
    getDocumentsBelowThreshold()
      .then((result) => {
        if (active) {
          setDocuments(result.items)
          setError(null)
        }
      })
      .catch((reason: unknown) => {
        if (active) setError(reason instanceof Error ? reason.message : 'The rectification queue could not be loaded.')
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => {
      active = false
    }
  }, [reloadCount])

  const handleDelete = useCallback(async (document: DocumentRecord) => {
    await deleteDocument(document.id)
    setReloadCount((count) => count + 1)
  }, [])

  return (
    <Stack spacing={3}>
      <div className="page-heading">
        <div>
          <Typography variant="h4" component="h1">Manual rectification</Typography>
          <Typography color="text.secondary">
            Review documents that need attention because their confidence level is below the configured threshold.
          </Typography>
        </div>
      </div>
      {loading && <CircularProgress aria-label="Loading documents for rectification" />}
      {error && <Alert severity="error">{error}</Alert>}
      {actionMessage && <Alert severity="info" onClose={() => setActionMessage(null)}>{actionMessage}</Alert>}
      {!loading && !error && (
        <DocumentDataGrid
          rows={documents}
          onView={(document) => onNavigate(`/document/${document.id}`)}
          onEdit={() => setActionMessage('Document editing is not available yet.')}
          onDelete={handleDelete}
        />
      )}
    </Stack>
  )
}
