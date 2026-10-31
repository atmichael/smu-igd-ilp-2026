import { useEffect, useState } from 'react'
import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Stack,
  Typography,
} from '@mui/material'
import { DocumentInfo } from './components/DocumentInfo'
import { deleteDocument, getDocumentById, updateDocument } from './service/document-service'
import type { DocumentRecord } from './model/document-model'

interface DocumentDetailsPageProps {
  documentId: number
  onBack: () => void
  onDeleted: () => void
}

export function DocumentDetailsPage({ documentId, onBack, onDeleted }: DocumentDetailsPageProps) {
  const [reloadCount, setReloadCount] = useState(0)
  const [result, setResult] = useState<{
    documentId: number
    reloadCount: number
    document?: DocumentRecord
    error?: string
  } | null>(null)
  const [editing, setEditing] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    getDocumentById(documentId)
      .then((result) => {
        if (active) setResult({ documentId, reloadCount, document: result })
      })
      .catch((reason: unknown) => {
        if (active) {
          setResult({
            documentId,
            reloadCount,
            error: reason instanceof Error ? reason.message : 'Document details could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [documentId, reloadCount])

  const current = result?.documentId === documentId && result.reloadCount === reloadCount
  const loading = !current
  const document = current ? result?.document : null
  const error = current ? result?.error : null

  const confirmDelete = async () => {
    setDeleting(true)
    setDeleteError(null)
    try {
      await deleteDocument(documentId)
      onDeleted()
    } catch (reason: unknown) {
      setDeleteError(reason instanceof Error ? reason.message : 'The document could not be deleted.')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <Stack spacing={2}>
      <div className="page-heading">
        <div>
          <Typography variant="h4" component="h1">Document details</Typography>
          <Typography color="text.secondary">View the original attachment and extracted information.</Typography>
        </div>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={onBack}>Back to source page</Button>
          {document && !editing && (
            <>
              <Button variant="contained" onClick={() => setEditing(true)}>Edit</Button>
              <Button variant="outlined" color="error" onClick={() => setConfirmingDelete(true)}>Delete</Button>
            </>
          )}
        </Stack>
      </div>
      {loading && <CircularProgress aria-label="Loading document details" />}
      {error && <Alert severity="error">{error}</Alert>}
      {document && (
        <DocumentInfo
          document={document}
          mode={editing ? 'edit' : 'view'}
          onSubmit={(request) => updateDocument(document.id, request)}
          onSaved={() => {
            setEditing(false)
            setReloadCount((count) => count + 1)
          }}
          onCancel={() => setEditing(false)}
        />
      )}
      <Dialog
        open={confirmingDelete}
        onClose={() => {
          if (!deleting) {
            setConfirmingDelete(false)
            setDeleteError(null)
          }
        }}
      >
        <DialogTitle>Delete document</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete document {document?.refNumber}?
          </DialogContentText>
          {deleteError && <Alert severity="error">{deleteError}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setConfirmingDelete(false)
              setDeleteError(null)
            }}
            disabled={deleting}
          >
            Cancel
          </Button>
          <Button onClick={() => void confirmDelete()} color="error" disabled={deleting}>
            Confirm
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}
