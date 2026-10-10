import { useState } from 'react'
import { Button, Paper, Stack, Typography } from '@mui/material'
import { CameraCapture } from './features/camera-capture/CameraCapture'
import { submitSourceDocument } from './features/camera-capture/source-document-api'
import './App.css'

function App() {
  const [submittedSourceDocument, setSubmittedSourceDocument] = useState<
    { sourceDocumentId: string; channel: string; pageCount: number; status: string } | null
  >()
  const [showCameraCapture, setShowCameraCapture] = useState(false)

  return (
    <main className="app-shell">
      <Stack spacing={3} sx={{ maxWidth: 960, mx: 'auto', py: 4, px: 2 }}>
        <Typography variant="h4" component="h1">
          Invoice intake
        </Typography>

        {!submittedSourceDocument ? (
          showCameraCapture ? (
            <CameraCapture
              autoStart
              onExit={() => setShowCameraCapture(false)}
              onSubmit={async (request) => {
                const result = await submitSourceDocument(request)
                setSubmittedSourceDocument(result)
                return result
              }}
            />
          ) : (
            <Paper elevation={0} className="app-shell__confirmation">
              <Stack spacing={2}>
                <Typography variant="h5" component="h2">
                  Choose a document source
                </Typography>
                <Typography>Select how you want to submit your invoice.</Typography>
                <Button
                  variant="contained"
                  onClick={() => {
                    setShowCameraCapture(true)
                  }}
                >
                  Start camera capture
                </Button>
              </Stack>
            </Paper>
          )
        ) : (
          <Paper elevation={0} className="app-shell__confirmation">
            <Stack spacing={1}>
              <Typography variant="h5" component="h2">
                Document submitted
              </Typography>
              <Typography>Source document ID: {submittedSourceDocument.sourceDocumentId}</Typography>
              <Typography>Status: {submittedSourceDocument.status}</Typography>
              <Button
                variant="contained"
                onClick={() => {
                  setSubmittedSourceDocument(null)
                  setShowCameraCapture(false)
                }}
              >
                Back to source selection
              </Button>
            </Stack>
          </Paper>
        )}
      </Stack>
    </main>
  )
}

export default App
