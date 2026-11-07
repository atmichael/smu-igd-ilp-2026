import { useEffect, useState } from 'react'
import { Alert, CircularProgress, TextField, Typography } from '@mui/material'
import { DocumentDataGrid } from './components/DocumentDataGrid'
import { DocumentEditDialog } from './components/DocumentEditDialog'
import { deleteDocument, searchDocuments } from './service/document-service'
import type { DocumentRecord } from './model/document-model'

interface DocumentHubPageProps {
  onNavigate: (path: string) => void
}

export function DocumentHubPage({ onNavigate }: DocumentHubPageProps) {
  const [search, setSearch] = useState('')
  const [uenFilter, setUenFilter] = useState('')
  const [page, setPage] = useState(0)
  const [rowsPerPage, setRowsPerPage] = useState(25)
  const [result, setResult] = useState<{
    key: string
    totalCount: number
    documents: DocumentRecord[]
    error: string | null
  } | null>(null)
  const [editing, setEditing] = useState<DocumentRecord | null>(null)
  const [reloadCount, setReloadCount] = useState(0)

  const effectiveSearch = uenFilter.trim() || search
  const requestKey = `${effectiveSearch}|${page}|${rowsPerPage}`

  useEffect(() => {
    let active = true
    searchDocuments(effectiveSearch, { page: page + 1, pageSize: rowsPerPage })
      .then((result) => {
        if (active) setResult({ key: requestKey, totalCount: result.totalCount, documents: result.items, error: null })
      })
      .catch((reason: unknown) => {
        if (active) {
          setResult({
            key: requestKey,
            totalCount: 0,
            documents: [],
            error: reason instanceof Error ? reason.message : 'Documents could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [effectiveSearch, page, rowsPerPage, requestKey, reloadCount])

  const handleDelete = async (document: DocumentRecord) => {
    await deleteDocument(document.id)
    setReloadCount((count) => count + 1)
  }

  const loading = result?.key !== requestKey
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
          onChange={(event) => {
            setSearch(event.target.value)
            setPage(0)
          }}
          size="small"
        />
      </section>
      {loading && <CircularProgress aria-label="Searching documents" />}
      {error && <Alert severity="error">{error}</Alert>}
      <DocumentDataGrid
        rows={documents}
        onView={(document) => onNavigate(`/document/${document.id}?returnTo=%2Fdocument`)}
        onEdit={setEditing}
        onDelete={handleDelete}
        serverPaging={{
          totalCount: result?.totalCount ?? 0,
          page,
          rowsPerPage,
          onPageChange: setPage,
          onRowsPerPageChange: setRowsPerPage,
          onUenFilterChange: (uen) => {
            setUenFilter(uen)
            setPage(0)
          },
        }}
      />
      <DocumentEditDialog
        document={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          setReloadCount((count) => count + 1)
        }}
      />
    </>
  )
}
