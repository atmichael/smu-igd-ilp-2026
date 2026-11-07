import { useEffect, useState } from 'react'
import { Alert, CircularProgress, Stack } from '@mui/material'
import { SubmissionConfirmation } from '../../common/components/SubmissionConfirmation'
import { submitSourceDocument } from '../source-documents/service/source-document-service'
import { FileUpload } from './components/FileUpload'
import { getUploadConfig } from './service/upload-config-service'

interface FileUploadPageProps {
  onNavigate: (path: string) => void
}

export function FileUploadPage({ onNavigate }: FileUploadPageProps) {
  const [submittedSourceDocument, setSubmittedSourceDocument] = useState<
    { sourceDocumentId: string; channel: string; pageCount: number; status: string } | null
  >(null)
  const [limitInMB, setLimitInMB] = useState<number | null>(null)
  const [configError, setConfigError] = useState(false)

  useEffect(() => {
    let active = true
    getUploadConfig().then((config) => {
      if (!Number.isInteger(config.attachmentLimitInMB) || config.attachmentLimitInMB <= 0) throw new Error('Invalid upload limit')
      if (active) setLimitInMB(config.attachmentLimitInMB)
    }).catch(() => { if (active) setConfigError(true) })
    return () => { active = false }
  }, [])

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
      ) : configError ? (
        <Alert severity="error">The upload limit could not be loaded. Please refresh and try again.</Alert>
      ) : limitInMB === null ? (
        <CircularProgress aria-label="Loading upload configuration" />
      ) : (
        <FileUpload
          limitInMB={limitInMB}
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
