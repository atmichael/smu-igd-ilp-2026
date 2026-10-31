import { useCallback, useEffect, useState, type ReactNode } from 'react'
import {
  clearGoogleIdToken,
  getGoogleIdToken,
  setGoogleIdToken,
} from './common/service/authentication-service'
import { CameraCapturePage } from './features/camera-capture/CameraCapturePage'
import { FileUploadPage } from './features/file-upload/FileUploadPage'
import { HomePage } from './features/home/HomePage'
import { LoginPage } from './features/login/LoginPage'
import { CompanyListPage } from './features/company/CompanyListPage'
import { DocumentDetailsPage } from './features/document/DocumentDetailsPage'
import { DocumentHubPage } from './features/document/DocumentHubPage'
import { DocumentExportPage } from './features/export/DocumentExportPage'
import { DocumentNotifications } from './features/notification/components/DocumentNotifications'
import { PortalShell } from './features/layout/PortalShell'
import './App.css'

function App() {
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
    navigate('/')
  }

  if (!isAuthenticated) {
    return <LoginPage onCredential={handleCredential} />
  }

  const content = resolveContent(path, navigate)

  return (
    <PortalShell
      activePath={/^\/document\/\d+$/.test(path) ? '/document' : path}
      onNavigate={navigate}
      onSignOut={handleSignOut}
    >
      <DocumentNotifications />
      {content}
    </PortalShell>
  )
}

function resolvePath(isAuthenticated: boolean, currentPath: string): string {
  if (!isAuthenticated) {
    return '/'
  }

  return ['/home', '/camera-capture', '/file-upload', '/company', '/document', '/export'].includes(currentPath) ||
    /^\/document\/\d+$/.test(currentPath)
    ? currentPath
    : '/home'
}

function resolveContent(path: string, navigate: (nextPath: string) => void): ReactNode {
  if (path === '/camera-capture') {
    return <CameraCapturePage onNavigate={navigate} />
  }

  if (path === '/file-upload') {
    return <FileUploadPage onNavigate={navigate} />
  }

  if (path === '/company') {
    return <CompanyListPage />
  }

  if (path === '/document') {
    return <DocumentHubPage onNavigate={navigate} />
  }

  if (path === '/export') {
    return <DocumentExportPage />
  }

  const documentId = path.match(/^\/document\/(\d+)$/)?.[1]
  if (documentId) {
    return <DocumentDetailsPage documentId={Number(documentId)} onBack={() => navigate('/document')} />
  }

  return <HomePage onNavigate={navigate} />
}

export default App
