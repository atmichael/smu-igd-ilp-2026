import { useCallback, useEffect, useState } from 'react'
import { Button, Paper, Stack, Typography } from '@mui/material'
import {
  clearGoogleIdToken,
  getGoogleIdToken,
  setGoogleIdToken,
} from './common/service/authentication-service'
import { CameraCapture } from './features/camera-capture/CameraCapture'
import { FileUpload } from './features/file-upload/FileUpload'
import { HomePage } from './features/home/HomePage'
import { LoginPage } from './features/login/LoginPage'
import { CompanyListPage } from './features/company/CompanyListPage'
import { DocumentDetailsPage } from './features/document/DocumentDetailsPage'
import { DocumentHubPage } from './features/document/DocumentHubPage'
import { PortalShell } from './features/layout/PortalShell'
import { submitSourceDocument } from './features/source-documents/service/source-document-service'
import './App.css'

function App() {
  const [submittedSourceDocument, setSubmittedSourceDocument] = useState<
    { sourceDocumentId: string; channel: string; pageCount: number; status: string } | null
  >()
  const [isAuthenticated, setIsAuthenticated] = useState(() => getGoogleIdToken() !== null)
  const [path, setPath] = useState(() => resolvePath(getGoogleIdToken() !== null, window.location.pathname))

  const navigate = useCallback((nextPath: string) => {
    window.history.pushState({}, '', nextPath)
    setPath(nextPath)
  }, [])

  useEffect(() => {
    const currentPath = resolvePath(getGoogleIdToken() !== null, window.location.pathname)
    if (currentPath !== window.location.pathname) {
      window.history.replaceState({}, '', currentPath)
    }

    const syncPath = () => {
      const nextPath = resolvePath(getGoogleIdToken() !== null, window.location.pathname)
      if (nextPath !== window.location.pathname) {
        window.history.replaceState({}, '', nextPath)
      }
      setPath(nextPath)
    }
    window.addEventListener('popstate', syncPath)
    return () => window.removeEventListener('popstate', syncPath)
  }, [])

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

  const documentId = path.match(/^\/document\/(\d+)$/)?.[1]
  const content =
    path === '/company' ? (
      <CompanyListPage />
    ) : path === '/document' ? (
      <DocumentHubPage onNavigate={navigate} />
    ) : documentId ? (
      <DocumentDetailsPage documentId={Number(documentId)} onBack={() => navigate('/document')} />
    ) : (
      <HomePage onNavigate={navigate} />
    )

  return (
    <PortalShell
      activePath={documentId ? '/document' : path}
      onNavigate={navigate}
      onSignOut={handleSignOut}
    >
      {content}
    </PortalShell>
  )
}

function resolvePath(isAuthenticated: boolean, currentPath: string): string {
  if (!isAuthenticated) {
    return '/'
  }

  return ['/home', '/camera-capture', '/file-upload', '/company', '/document'].includes(currentPath) ||
    /^\/document\/\d+$/.test(currentPath)
    ? currentPath
    : '/home'
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
