import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { clearGoogleIdToken } from './common/service/authentication-service'
import App from './App'

vi.mock('./features/login/GoogleSignInButton', () => ({
  GoogleSignInButton: ({ onCredential }: { onCredential: (credential: string) => void }) => (
    <button onClick={() => onCredential('google-id-token')}>Sign in with Google</button>
  ),
}))

vi.mock('./features/camera-capture/components/CameraCapture', () => ({
  CameraCapture: () => <h2>Camera capture route</h2>,
}))

vi.mock('./features/file-upload/components/FileUpload', () => ({
  FileUpload: () => <h2>File upload route</h2>,
}))

vi.mock('./features/document/components/DocumentDataGrid', () => ({
  DocumentDataGrid: () => <div>Manual rectification documents</div>,
}))

vi.mock('./features/document/service/document-service', () => ({
  getDocumentsBelowThreshold: async () => ({ items: [], totalCount: 0 }),
}))

vi.mock('./features/company/CompanyListPage', () => ({
  CompanyListPage: () => <h2>Company list route</h2>,
}))

vi.mock('./features/document/DocumentHubPage', () => ({
  DocumentHubPage: () => <h2>Document hub route</h2>,
}))

vi.mock('./features/export/DocumentExportPage', () => ({
  DocumentExportPage: () => <h2>Export route</h2>,
}))

describe('App authentication and navigation', () => {
  beforeEach(() => {
    clearGoogleIdToken()
    window.history.replaceState({}, '', '/')
  })

  it('shows the login page first and redirects to the post-login landing page', async () => {
    render(<App />)

    expect(screen.getByRole('heading', { name: /sign in to invoice intake/i })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))

    expect(window.location.pathname).toBe('/home')
    expect(screen.getByRole('link', { name: /invoice intake/i })).toHaveAttribute('href', '/home')
    expect(screen.getByRole('heading', { name: /manual rectification/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /camera capture/i })).toHaveAttribute('href', '/camera-capture')
    expect(screen.getByRole('link', { name: /file upload/i })).toHaveAttribute('href', '/file-upload')
    expect(screen.getByRole('link', { name: /company/i })).toHaveAttribute('href', '/company')
    expect(screen.getByRole('link', { name: /document hub/i })).toHaveAttribute('href', '/document')
    await waitFor(() => expect(screen.getByText(/manual rectification documents/i)).toBeInTheDocument())
  })

  it('navigates from the post-login page to camera capture', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))
    fireEvent.click(screen.getByRole('link', { name: /camera capture/i }))

    expect(window.location.pathname).toBe('/camera-capture')
    expect(screen.getByRole('heading', { name: /camera capture route/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /invoice intake/i })).toHaveAttribute('href', '/home')
  })

  it('navigates from the post-login page to file upload', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))
    fireEvent.click(screen.getByRole('link', { name: /file upload/i }))

    expect(window.location.pathname).toBe('/file-upload')
    expect(screen.getByRole('heading', { name: /file upload route/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /invoice intake/i })).toHaveAttribute('href', '/home')
  })

  it('navigates to the company list', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))
    fireEvent.click(screen.getByRole('link', { name: /^company$/i }))

    expect(window.location.pathname).toBe('/company')
    expect(screen.getByRole('heading', { name: /company list route/i })).toBeInTheDocument()
  })

  it('navigates to the document hub', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))
    fireEvent.click(screen.getByRole('link', { name: /document hub/i }))

    expect(window.location.pathname).toBe('/document')
    expect(screen.getByRole('heading', { name: /document hub route/i })).toBeInTheDocument()
  })

  it('navigates to the export page', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))
    fireEvent.click(screen.getByRole('link', { name: /^export$/i }))

    expect(window.location.pathname).toBe('/export')
    expect(screen.getByRole('heading', { name: /export route/i })).toBeInTheDocument()
  })

  it('redirects an unauthenticated feature route to the login page', () => {
    window.history.replaceState({}, '', '/camera-capture')
    render(<App />)

    expect(window.location.pathname).toBe('/')
    expect(screen.getByRole('heading', { name: /sign in to invoice intake/i })).toBeInTheDocument()
  })
})
