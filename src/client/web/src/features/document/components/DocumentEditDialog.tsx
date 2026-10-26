import { useEffect, useState } from 'react'
import { Alert, CircularProgress, Dialog, DialogContent, DialogTitle } from '@mui/material'
import { DocumentInfo } from './DocumentInfo'
import { getDocumentById, updateDocument } from '../service/document-service'
import type { DocumentRecord } from '../model/document-model'

interface DocumentEditDialogProps {
  document: DocumentRecord | null
  onClose: () => void
  onSaved: () => void
}

export function DocumentEditDialog({ document, onClose, onSaved }: DocumentEditDialogProps) {
  const [loaded, setLoaded] = useState<{ id: number; record?: DocumentRecord; error?: string } | null>(null)
  const documentId = document?.id

  useEffect(() => {
    if (documentId === undefined) return
    let active = true
    // List rows omit the original attachment, so load the full record for the preview.
    getDocumentById(documentId)
      .then((record) => {
        if (active) setLoaded({ id: documentId, record })
      })
      .catch((reason: unknown) => {
        if (active) {
          setLoaded({
            id: documentId,
            error: reason instanceof Error ? reason.message : 'Document details could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [documentId])

  const current = loaded?.id === documentId ? loaded : null

  return (
    <Dialog
      open={document !== null}
      onClose={(_event, reason) => {
        if (reason !== 'backdropClick') onClose()
      }}
      maxWidth="xl"
      fullWidth
    >
      <DialogTitle>Edit document {document?.refNumber}</DialogTitle>
      <DialogContent>
        {document && !current && <CircularProgress aria-label="Loading document details" />}
        {current?.error && <Alert severity="error">{current.error}</Alert>}
        {document && current?.record && (
          <DocumentInfo
            document={current.record}
            mode="edit"
            onSubmit={async (request) => {
              await updateDocument(document.id, request)
            }}
            onSaved={onSaved}
            onCancel={onClose}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}
