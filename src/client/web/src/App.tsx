import { useCallback, useEffect, useState } from 'react'
import { Button, Paper, Stack, Typography } from '@mui/material'
import { clearGoogleIdToken, setGoogleIdToken } from './common/service/authentication-service'
import { CameraCapture } from './features/camera-capture/CameraCapture'
import { FileUpload } from './features/file-upload/FileUpload'
import { HomePage } from './features/home/HomePage'
import { LoginPage } from './features/login/LoginPage'
import { submitSourceDocument } from './features/source-documents/service/source-document-service'
import './App.css'

function App() {
  const [submittedSourceDocument, setSubmittedSourceDocument] = useState<
    { sourceDocumentId: string; channel: string; pageCount: number; status: string } | null
  >()
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [path, setPath] = useState(() => window.location.pathname)

  const navigate = useCallback((nextPath: string) => {
    window.history.pushState({}, '', nextPath)
    setPath(nextPath)
  }, [])

  useEffect(() => {
    const syncPath = () => setPath(window.location.pathname)
    window.addEventListener('popstate', syncPath)
    return () => window.removeEventListener('popstate', syncPath)
  }, [])

  useEffect(() => {
    if (!isAuthenticated && path !== '/') {
      navigate('/')
    } else if (isAuthenticated && !['/home', '/camera-capture', '/file-upload'].includes(path)) {
      navigate('/home')
    }
  }, [isAuthenticated, navigate, path])

  const handleCredential = (credential: string) => {
    setGoogleIdToken(credential)
    setIsAuthenticated(true)
    navigate('/home')
  }

  const handleSignOut = () => {
    clearGoogleIdToken()
    setIsAuthenticated(false)
    setSubmittedSourceDocument(null)
    navigate('/')
  }

  if (!isAuthenticated) {
    return <LoginPage onCredential={handleCredential} />
  }

  if (path === '/camera-capture') {
    return (
      <main className="app-shell">
        <Stack spacing={3} sx={{ maxWidth: 960, mx: 'auto', py: 4, px: 2 }}>
          {submittedSourceDocument ? (
            <SubmissionConfirmation
              sourceDocumentId={submittedSourceDocument.sourceDocumentId}
              status={submittedSourceDocument.status}
              onBack={() => {
                setSubmittedSourceDocument(null)
                navigate('/home')
              }}
            />
          ) : (
            <CameraCapture
              autoStart
              onExit={() => navigate('/home')}
              onSubmit={async (request) => {
                const result = await submitSourceDocument(request)
                setSubmittedSourceDocument(result)
                return result
              }}
            />
          )}
        </Stack>
      </main>
    )
  }

  if (path === '/file-upload') {
    return (
      <main className="app-shell">
        <Stack spacing={3} sx={{ maxWidth: 960, mx: 'auto', py: 4, px: 2 }}>
          {submittedSourceDocument ? (
            <SubmissionConfirmation
              sourceDocumentId={submittedSourceDocument.sourceDocumentId}
              status={submittedSourceDocument.status}
              onBack={() => {
                setSubmittedSourceDocument(null)
                navigate('/home')
              }}
            />
          ) : (
            <FileUpload
              onExit={() => navigate('/home')}
              onSubmit={async (request) => {
                const result = await submitSourceDocument(request)
                setSubmittedSourceDocument(result)
                return result
              }}
            />
          )}
        </Stack>
      </main>
    )
  }

  return (
    <HomePage onNavigate={navigate} onSignOut={handleSignOut} />
  )
}

function SubmissionConfirmation({
  sourceDocumentId,
  status,
  onBack,
}: {
  sourceDocumentId: string
  status: string
  onBack: () => void
}) {
  return (
    <Paper elevation={0} className="app-shell__confirmation">
      <Stack spacing={1}>
        <Typography variant="h5" component="h2">
          Document submitted
        </Typography>
        <Typography>Source document ID: {sourceDocumentId}</Typography>
        <Typography>Status: {status}</Typography>
        <Button variant="contained" onClick={onBack}>
          Back to source selection
        </Button>
      </Stack>
    </Paper>
  )
}

export default App
