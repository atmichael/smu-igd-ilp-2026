import { Dialog, DialogContent, DialogTitle } from '@mui/material'
import { DocumentInfo } from './DocumentInfo'
import { updateDocument } from '../service/document-service'
import type { DocumentRecord } from '../model/document-model'

interface DocumentEditDialogProps {
  document: DocumentRecord | null
  onClose: () => void
  onSaved: () => void
}

export function DocumentEditDialog({ document, onClose, onSaved }: DocumentEditDialogProps) {
  return (
    <Dialog open={document !== null} onClose={onClose} maxWidth="xl" fullWidth>
      <DialogTitle>Edit document {document?.refNumber}</DialogTitle>
      <DialogContent>
        {document && (
          <DocumentInfo
            document={document}
            mode="edit"
            onSubmit={async (request) => {
              await updateDocument(document.id, request)
            }}
            onSaved={onSaved}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}
