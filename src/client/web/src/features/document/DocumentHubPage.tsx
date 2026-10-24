import { useEffect, useState } from 'react'
import { Alert, CircularProgress, TextField, Typography } from '@mui/material'
import { DocumentDataGrid } from './components/DocumentDataGrid'
import { searchDocuments } from './service/document-service'
import type { DocumentRecord } from './model/document-model'

interface DocumentHubPageProps {
  onNavigate: (path: string) => void
}

export function DocumentHubPage({ onNavigate }: DocumentHubPageProps) {
  const [search, setSearch] = useState('')
  const [result, setResult] = useState<{
    search: string
    documents: DocumentRecord[]
    error: string | null
  } | null>(null)
  const [actionMessage, setActionMessage] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    searchDocuments(search)
      .then((result) => {
        if (active) setResult({ search, documents: result.items, error: null })
      })
      .catch((reason: unknown) => {
        if (active) {
          setResult({
            search,
            documents: [],
            error: reason instanceof Error ? reason.message : 'Documents could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [search])

  const loading = result?.search !== search
  const error = loading ? null : result?.error ?? null
  const documents = loading || error ? [] : result?.documents ?? []

  return (
    <>
      <section className="page-heading">
        <div>
          <Typography variant="h4" component="h1">Document hub</Typography>
          <Typography color="text.secondary">Search and review processed documents.</Typography>
        </div>
        <TextField
          label="Search by document number, UEN, or company name"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          size="small"
        />
      </section>
      {loading && <CircularProgress aria-label="Searching documents" />}
      {error && <Alert severity="error">{error}</Alert>}
      {actionMessage && <Alert severity="info" onClose={() => setActionMessage(null)}>{actionMessage}</Alert>}
      <DocumentDataGrid
        rows={documents}
        onView={(document) => onNavigate(`/document/${document.id}`)}
        onEdit={() => setActionMessage('Document editing is not available yet.')}
        onDelete={() => setActionMessage('Document deletion is not available yet.')}
      />
    </>
  )
}
