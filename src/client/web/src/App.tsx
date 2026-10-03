import { useState } from 'react'
import { Paper, Stack, Typography } from '@mui/material'
import { CameraCapture } from './features/camera-capture/CameraCapture'
import { submitSourceDocument } from './features/camera-capture/source-document-api'
import './App.css'

function App() {
  const [submittedSourceDocument, setSubmittedSourceDocument] = useState<
    { sourceDocumentId: string; source: string; pageCount: number; status: string } | null
  >()

  return (
    <main className="app-shell">
      <Stack spacing={3} sx={{ maxWidth: 960, mx: 'auto', py: 4, px: 2 }}>
        <Typography variant="h4" component="h1">
          Invoice intake
        </Typography>

        {!submittedSourceDocument ? (
          <CameraCapture
            onExit={() => {}}
            onSubmit={async (request) => {
              const result = await submitSourceDocument(request)
              setSubmittedSourceDocument(result)
              return result
            }}
          />
        ) : (
          <Paper elevation={0} className="app-shell__confirmation">
            <Stack spacing={1}>
              <Typography variant="h5" component="h2">
                Capture submitted
              </Typography>
              <Typography>Source document ID: {submittedSourceDocument.sourceDocumentId}</Typography>
              <Typography>Status: {submittedSourceDocument.status}</Typography>
            </Stack>
          </Paper>
        )}
      </Stack>
    </main>
  )
}

export default App
