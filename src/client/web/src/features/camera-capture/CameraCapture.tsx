import { useEffect, useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import {
  acceptPendingPage,
  addPendingPage,
  buildSubmissionRequest,
  createInitialSession,
  removeAcceptedPage,
  type CameraCaptureSession,
} from './camera-capture-model'
import { submitSourceDocument, type SourceDocumentResponse } from './source-document-api'
import './CameraCapture.css'

interface CameraCaptureProps {
  startCamera?: () => Promise<MediaStream>
  onExit: () => void
  onSubmit?: (
    request: { source: 'camera-capture'; pages: Blob[]; idempotencyKey: string },
  ) => Promise<SourceDocumentResponse>
}

export function CameraCapture({
  startCamera,
  onExit,
  onSubmit = async (request) => submitSourceDocument(request),
}: CameraCaptureProps) {
  const videoRef = useRef<HTMLVideoElement | null>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const [session, setSession] = useState<CameraCaptureSession>(createInitialSession)
  const sessionRef = useRef(session)
  const [submittedDocument, setSubmittedDocument] = useState<SourceDocumentResponse | null>(null)

  sessionRef.current = session

  const releasePreviewUrl = (previewUrl: string | null | undefined) => {
    if (previewUrl && typeof URL.revokeObjectURL === 'function') {
      URL.revokeObjectURL(previewUrl)
    }
  }

  useEffect(() => {
    return () => {
      streamRef.current?.getTracks().forEach((track) => track.stop())
      sessionRef.current.acceptedPages.forEach((page) => releasePreviewUrl(page.previewUrl))
      if (sessionRef.current.pendingPage) {
        releasePreviewUrl(sessionRef.current.pendingPage.previewUrl)
      }
    }
  }, [])

  useEffect(() => {
    if (session.state === 'liveCapture' && videoRef.current && streamRef.current) {
      videoRef.current.srcObject = streamRef.current
      const playPromise = videoRef.current.play()
      if (playPromise && typeof playPromise.catch === 'function') {
        playPromise.catch(() => undefined)
      }
    }
  }, [session.state])

  const stopStream = () => {
    streamRef.current?.getTracks().forEach((track) => track.stop())
    streamRef.current = null
  }

  const startCapture = async () => {
    setSession((current) => ({ ...current, state: 'requestingPermission', errorMessage: null }))

    try {
      const stream = await (startCamera ?? defaultStartCamera)()
      streamRef.current = stream
      if (videoRef.current) {
        videoRef.current.srcObject = stream
      }
      setSession((current) => ({ ...current, state: 'liveCapture' }))
    } catch (error) {
      setSession((current) => ({
        ...current,
        state: 'recoverableError',
        errorMessage:
          error instanceof Error
            ? error.message
            : 'Camera access was denied or the capture device is unavailable.',
      }))
    }
  }

  const captureStill = async () => {
    const mediaStream = streamRef.current
    const videoElement = videoRef.current

    if (!mediaStream || !videoElement) {
      setSession((current) => ({
        ...current,
        state: 'recoverableError',
        errorMessage: 'The live camera view is not available. Please try again.',
      }))
      return
    }

    const canvas = document.createElement('canvas')
    canvas.width = videoElement.videoWidth || 1200
    canvas.height = videoElement.videoHeight || 1600

    const context = canvas.getContext('2d')
    if (!context) {
      setSession((current) => ({
        ...current,
        state: 'recoverableError',
        errorMessage: 'The camera capture could not be processed on this device.',
      }))
      return
    }

    context.drawImage(videoElement, 0, 0, canvas.width, canvas.height)
    const blob = await new Promise<Blob | null>((resolve) => {
      canvas.toBlob((result) => resolve(result), 'image/jpeg', 0.9)
    })

    if (!blob) {
      setSession((current) => ({
        ...current,
        state: 'recoverableError',
        errorMessage: 'A still image could not be created from the camera frame.',
      }))
      return
    }

    const previewUrl = URL.createObjectURL(blob)
    setSession((current) => addPendingPage(current, blob, previewUrl))
  }

  const acceptPage = () => {
    setSession((current) => acceptPendingPage(current))
  }

  const captureAnotherPage = () => {
    const hasLiveTrack = streamRef.current
      ?.getTracks()
      .some((track) => track.readyState === 'live')

    if (hasLiveTrack) {
      setSession((current) => ({ ...current, state: 'liveCapture', errorMessage: null }))
      return
    }

    void startCapture()
  }

  const retakePage = () => {
    if (!session.pendingPage) {
      return
    }

    releasePreviewUrl(session.pendingPage.previewUrl)
    setSession((current) => ({
      ...current,
      pendingPage: null,
      state: 'liveCapture',
      errorMessage: null,
    }))
    if (videoRef.current && streamRef.current) {
      videoRef.current.srcObject = streamRef.current
      const playPromise = videoRef.current.play()
      if (playPromise && typeof playPromise.catch === 'function') {
        playPromise.catch(() => undefined)
      }
    }
  }

  const removePage = (pageId: string) => {
    setSession((current) => removeAcceptedPage(current, pageId))
  }

  const handleSubmit = async () => {
    const request = buildSubmissionRequest(session.acceptedPages, crypto.randomUUID())
    setSession((current) => ({ ...current, state: 'submitting' }))

    try {
      const document = await onSubmit(request)
      setSubmittedDocument(document)
      stopStream()
      setSession((current) => ({ ...current, state: 'submitted' }))
    } catch (error) {
      setSession((current) => ({
        ...current,
        state: 'recoverableError',
        errorMessage:
          error instanceof Error
            ? error.message
            : 'The document could not be submitted. Please try again.',
      }))
    }
  }

  const cancelSession = () => {
    session.acceptedPages.forEach((page) => releasePreviewUrl(page.previewUrl))
    if (session.pendingPage) {
      releasePreviewUrl(session.pendingPage.previewUrl)
    }
    stopStream()
    setSession({
      state: 'cancelled',
      acceptedPages: [],
      pendingPage: null,
      errorMessage: null,
    })
    onExit()
  }

  const canCapture = session.state === 'liveCapture'

  return (
    <Paper className="camera-capture" aria-live="polite" elevation={0} sx={{ p: 3, border: '1px solid #e2e8f0', borderRadius: 3 }}>
      <Stack spacing={2}>
        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h5" component="h2">
            Camera document capture
          </Typography>
          <Button variant="outlined" onClick={cancelSession}>
            Exit
          </Button>
        </Stack>

        {session.errorMessage ? <Alert severity="error">{session.errorMessage}</Alert> : null}

        {session.state === 'pageReview' && session.pendingPage ? (
          <Box className="camera-capture__stage camera-capture__stage--review">
            <img
              src={session.pendingPage.previewUrl}
              alt="Captured page preview"
              className="camera-capture__preview"
            />
          </Box>
        ) : session.state === 'documentReview' && session.acceptedPages.length > 0 ? (
          <Box className="camera-capture__stage camera-capture__stage--review">
            <img
              src={session.acceptedPages[session.acceptedPages.length - 1].previewUrl}
              alt={`Accepted page ${session.acceptedPages.length} preview`}
              className="camera-capture__preview"
            />
          </Box>
        ) : (
          <Box className="camera-capture__stage">
            <video ref={videoRef} autoPlay playsInline muted className="camera-capture__video" />
          </Box>
        )}

        <Typography className="camera-capture__prompt" variant="body1">
          Capture one to three pages.
        </Typography>

        <Stack className="camera-capture__actions" spacing={1.25}>
          {(session.state === 'idle' || session.state === 'recoverableError') && (
            <Button variant="contained" onClick={startCapture}>
              Start Camera Capture
            </Button>
          )}

          {canCapture && session.state !== 'recoverableError' && (
            <Button variant="contained" onClick={captureStill}>
              Capture page
            </Button>
          )}

          {session.state === 'pageReview' && session.pendingPage && (
            <Stack direction="row" spacing={1}>
              <Button variant="contained" onClick={acceptPage}>
                Accept page
              </Button>
              <Button variant="outlined" onClick={retakePage}>
                Retake page
              </Button>
            </Stack>
          )}

          {session.state === 'documentReview' && session.acceptedPages.length < 3 && (
            <Button variant="outlined" onClick={captureAnotherPage}>
              Capture another page
            </Button>
          )}

          {session.acceptedPages.length > 0 && (
            <Box className="camera-capture__pages">
              <Typography variant="h6" component="h3">
                Accepted pages
              </Typography>
              <Typography variant="body2">Capture one to three pages.</Typography>
              <ul>
                {session.acceptedPages.map((page) => (
                  <li key={page.id}>
                    <img
                      src={page.previewUrl}
                      alt={`Accepted page ${page.position} thumbnail`}
                      className="camera-capture__thumbnail"
                    />
                    <span>Page {page.position}</span>
                    <Button variant="outlined" size="small" onClick={() => removePage(page.id)}>
                      Remove
                    </Button>
                  </li>
                ))}
              </ul>
            </Box>
          )}

          {session.acceptedPages.length > 0 && (
            <Button
              variant="contained"
              onClick={handleSubmit}
              disabled={session.state === 'submitting'}
            >
              {session.state === 'submitting' ? 'Submitting…' : 'Submit capture'}
            </Button>
          )}
        </Stack>

        {submittedDocument && (
          <Box className="camera-capture__success">
            <Typography variant="h6" component="h3">
              Submitted
            </Typography>
            <Typography>Source document ID: {submittedDocument.sourceDocumentId}</Typography>
            <Typography>Status: {submittedDocument.status}</Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  )
}

async function defaultStartCamera(): Promise<MediaStream> {
  if (!navigator.mediaDevices?.getUserMedia) {
    throw new Error('Camera capture is not supported on this browser.')
  }

  return navigator.mediaDevices.getUserMedia({
    video: {
      facingMode: { ideal: 'environment' },
    },
  })
}
