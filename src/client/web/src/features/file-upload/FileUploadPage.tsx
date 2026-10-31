import { useState } from 'react'
import { Stack } from '@mui/material'
import { SubmissionConfirmation } from '../../common/components/SubmissionConfirmation'
import { submitSourceDocument } from '../source-documents/service/source-document-service'
import { FileUpload } from './components/FileUpload'

interface FileUploadPageProps {
  onNavigate: (path: string) => void
}

export function FileUploadPage({ onNavigate }: FileUploadPageProps) {
  const [submittedSourceDocument, setSubmittedSourceDocument] = useState<
    { sourceDocumentId: string; channel: string; pageCount: number; status: string } | null
  >(null)

  return (
    <Stack spacing={3} sx={{ maxWidth: 960, mx: 'auto', py: 4, px: 2 }}>
      {submittedSourceDocument ? (
        <SubmissionConfirmation
          sourceDocumentId={submittedSourceDocument.sourceDocumentId}
          status={submittedSourceDocument.status}
          onBack={() => {
            setSubmittedSourceDocument(null)
          }}
        />
      ) : (
        <FileUpload
          onExit={() => onNavigate('/home')}
          onSubmit={async (request) => {
            const result = await submitSourceDocument(request)
            setSubmittedSourceDocument(result)
            return result
          }}
        />
      )}
    </Stack>
  )
}
