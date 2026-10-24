import { useEffect, useRef, useState, type ChangeEvent, type DragEvent } from 'react'
import { Alert, Box, Button, CircularProgress, Paper, Stack, Typography, useMediaQuery } from '@mui/material'
import {
  fetchOriginalFile,
  submitSourceDocument,
} from '../../source-documents/service/source-document-service'
import type { SourceDocumentResponse } from '../../../common/model/source-document-model'
import {
  ACCEPTED_TYPES_TEXT,
  FILE_INPUT_ACCEPT,
  FILE_UPLOAD_MAX_LABEL,
  applySubmissionFailure,
  formatFileSize,
  initialUploadState,
  selectFile,
  type UploadState,
} from '../model/file-upload-model'

interface FileUploadProps {
  onExit: () => void
  onSubmit?: (request: {
    channel: 'file-upload'
    file: File
    idempotencyKey: string
  }) => Promise<SourceDocumentResponse>
  onFetchOriginal?: (sourceDocumentId: string) => Promise<Blob>
}

// WCAG 2.2 target size; MUI's default medium button is shorter.
const buttonSx = { minHeight: 44, minWidth: 44 }
const fullWidthOnPhone = { ...buttonSx, width: { xs: '100%', sm: 'auto' } }
// Secondary actions sit on the left and the one primary action on the right, in tab order.
const actionRowSx = {
  flexDirection: { xs: 'column', sm: 'row' },
  justifyContent: 'space-between',
  gap: 1,
}
const groupSx = { flexDirection: { xs: 'column', sm: 'row' }, gap: 1 }

export function FileUpload({
  onExit,
  onSubmit = async (request) => submitSourceDocument(request),
  onFetchOriginal = fetchOriginalFile,
}: FileUploadProps) {
  const [state, setState] = useState<UploadState>(initialUploadState)
  const [originalError, setOriginalError] = useState<string | null>(null)
  const [dragActive, setDragActive] = useState(false)
  const [dropMessage, setDropMessage] = useState<string | null>(null)
  // Phones and tablets cannot drag files into a browser, so the hint is only shown with a mouse or trackpad.
  const canDrag = useMediaQuery('(hover: hover) and (pointer: fine)')
  const inputRef = useRef<HTMLInputElement | null>(null)
  const headingRef = useRef<HTMLHeadingElement | null>(null)
  const objectUrlsRef = useRef<string[]>([])

  const releaseObjectUrls = () => {
    objectUrlsRef.current.forEach((url) => URL.revokeObjectURL(url))
    objectUrlsRef.current = []
  }

  useEffect(() => releaseObjectUrls, [])

  useEffect(() => {
    if (state.status === 'received') {
      headingRef.current?.focus()
    }
  }, [state.status])

  const chooseFile = () => inputRef.current?.click()

  const handleSelection = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    // Allow choosing the same file again after clearing or a rejection.
    event.target.value = ''
    setDropMessage(null)
    if (file) {
      setState(selectFile(file))
    }
  }

  const acceptsDrop = state.status !== 'submitting' && state.status !== 'received'
  const dragsFiles = (event: DragEvent<HTMLElement>) => Array.from(event.dataTransfer.types).includes('Files')

  const handleDragOver = (event: DragEvent<HTMLElement>) => {
    if (!dragsFiles(event)) {
      return
    }

    // Always cancel the default so a file dropped here never replaces the page in the browser.
    event.preventDefault()
    event.dataTransfer.dropEffect = acceptsDrop ? 'copy' : 'none'
    setDragActive(acceptsDrop)
  }

  const handleDragLeave = (event: DragEvent<HTMLElement>) => {
    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
      setDragActive(false)
    }
  }

  const handleDrop = (event: DragEvent<HTMLElement>) => {
    if (!dragsFiles(event)) {
      return
    }

    event.preventDefault()
    setDragActive(false)
    const files = event.dataTransfer.files
    if (!acceptsDrop || files.length === 0) {
      return
    }

    if (files.length > 1) {
      setDropMessage('Only one file can be uploaded at a time. Drop a single file.')
      return
    }

    setDropMessage(null)
    setState(selectFile(files[0]))
  }

  const submit = async () => {
    if (state.status !== 'selected' && state.status !== 'failed') {
      return
    }

    const { file, idempotencyKey } = state
    setState({ status: 'submitting', file, idempotencyKey })
    try {
      const document = await onSubmit({ channel: 'file-upload', file, idempotencyKey })
      setState({ status: 'received', document })
    } catch (error) {
      setState(applySubmissionFailure(file, idempotencyKey, error))
    }
  }

  const uploadAnother = () => {
    releaseObjectUrls()
    setOriginalError(null)
    setState(initialUploadState)
  }

  const openOriginal = async (document: SourceDocumentResponse, mode: 'view' | 'download') => {
    setOriginalError(null)
    try {
      const blob = await onFetchOriginal(document.sourceDocumentId)
      const url = URL.createObjectURL(blob)
      objectUrlsRef.current.push(url)

      if (mode === 'view') {
        window.open(url, '_blank', 'noopener')
        return
      }

      const link = window.document.createElement('a')
      link.href = url
      link.download = document.origin?.upload?.originalFileName ?? document.sourceDocumentId
      link.click()
    } catch {
      setOriginalError('The original file could not be retrieved. Try again.')
    }
  }

  const selectedFile = 'file' in state ? state.file : null
  const busy = state.status === 'submitting'
  // After a rejection the only way forward is a new file, so that action becomes the primary one.
  const needsNewFile = state.status === 'blocked' || state.status === 'rejected'

  return (
    <Paper
      elevation={0}
      className="app-shell__confirmation"
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      sx={dragActive ? { outline: '2px solid', outlineColor: 'primary.main', outlineOffset: -2 } : undefined}
    >
      <Stack spacing={2.5}>

        <Typography variant="h5" component="h2">
          Upload a file
        </Typography>

        <input ref={inputRef} type="file" hidden accept={FILE_INPUT_ACCEPT} onChange={handleSelection} />

        {dropMessage ? <Alert severity="warning">{dropMessage}</Alert> : null}

        {state.status === 'received' ? (
          <Stack spacing={2.5}>
            <Alert severity="success" role="status">
              <Typography
                variant="subtitle1"
                component="h3"
                tabIndex={-1}
                ref={headingRef}
                sx={{ outline: 'none' }}
              >
                Document received
              </Typography>
              <Typography>Source document ID: {state.document.sourceDocumentId}</Typography>
              {state.document.origin?.upload?.originalFileName ? (
                <Typography>File: {state.document.origin.upload.originalFileName}</Typography>
              ) : null}
            </Alert>
            {originalError ? <Alert severity="error">{originalError}</Alert> : null}
            <Stack sx={actionRowSx}>
              <Stack sx={groupSx}>
                <Button variant="outlined" sx={fullWidthOnPhone} onClick={() => void openOriginal(state.document, 'view')}>
                  View original
                </Button>
                <Button variant="outlined" sx={fullWidthOnPhone} onClick={() => void openOriginal(state.document, 'download')}>
                  Download original
                </Button>
              </Stack>
              <Button variant="contained" sx={fullWidthOnPhone} onClick={uploadAnother}>
                Upload another
              </Button>
            </Stack>
          </Stack>
        ) : selectedFile ? (
          <Stack spacing={2.5}>
            <Box
              sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 2, display: 'grid', gap: 1.5 }}
            >
              <Box>
                <Typography component="p" sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>
                  Selected file: {selectedFile.name}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {formatFileSize(selectedFile.size)}
                </Typography>
              </Box>

              {state.status === 'selected' ? (
                <Alert severity="success" role="status">
                  Ready to submit. The server will check the file when you submit it.
                </Alert>
              ) : null}
              {state.status === 'blocked' || state.status === 'rejected' || state.status === 'failed' ? (
                <Alert severity="error">{state.message}</Alert>
              ) : null}
              {busy ? (
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }} role="status">
                  <CircularProgress size={20} aria-hidden="true" />
                  <Typography>Submitting the document…</Typography>
                </Stack>
              ) : null}
            </Box>

            <Stack sx={actionRowSx}>
              <Stack sx={groupSx}>
                <Button
                  variant={needsNewFile ? 'contained' : 'outlined'}
                  sx={fullWidthOnPhone}
                  disabled={busy}
                  onClick={chooseFile}
                >
                  Choose another file
                </Button>
                <Button variant="text" sx={fullWidthOnPhone} disabled={busy} onClick={() => { setDropMessage(null); setState(initialUploadState) }}>
                  Clear selection
                </Button>
              </Stack>
              {state.status === 'selected' || state.status === 'submitting' ? (
                <Button variant="contained" sx={fullWidthOnPhone} disabled={busy} onClick={() => void submit()}>
                  Submit document
                </Button>
              ) : null}
              {state.status === 'failed' ? (
                <Button variant="contained" sx={fullWidthOnPhone} onClick={() => void submit()}>
                  Try again
                </Button>
              ) : null}
            </Stack>
          </Stack>
        ) : (
          <Stack
            spacing={2}
            sx={{
              border: 2,
              borderStyle: 'dashed',
              borderColor: dragActive ? 'primary.main' : 'divider',
              bgcolor: dragActive ? 'action.hover' : undefined,
              borderRadius: 2,
              p: 3,
              alignItems: 'center',
              textAlign: 'center',
            }}
          >
            <Typography>
              {canDrag
                ? `Drag a ${ACCEPTED_TYPES_TEXT} file here, or choose one. Up to ${FILE_UPLOAD_MAX_LABEL}.`
                : `Choose one ${ACCEPTED_TYPES_TEXT} file, up to ${FILE_UPLOAD_MAX_LABEL}.`}
            </Typography>
            <Button variant="contained" sx={fullWidthOnPhone} onClick={chooseFile}>
              Choose file
            </Button>
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
