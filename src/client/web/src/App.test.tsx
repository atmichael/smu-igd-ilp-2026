import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { clearGoogleIdToken } from './common/service/authentication-service'
import App from './App'

vi.mock('./features/login/GoogleSignInButton', () => ({
  GoogleSignInButton: ({ onCredential }: { onCredential: (credential: string) => void }) => (
    <button onClick={() => onCredential('google-id-token')}>Sign in with Google</button>
  ),
}))

vi.mock('./features/camera-capture/CameraCapture', () => ({
  CameraCapture: () => <h2>Camera capture route</h2>,
}))

vi.mock('./features/file-upload/FileUpload', () => ({
  FileUpload: () => <h2>File upload route</h2>,
}))

describe('App authentication and navigation', () => {
  beforeEach(() => {
    clearGoogleIdToken()
    window.history.replaceState({}, '', '/')
  })

  it('shows the login page first and redirects to the post-login landing page', () => {
    render(<App />)

    expect(screen.getByRole('heading', { name: /sign in to invoice intake/i })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))

    expect(window.location.pathname).toBe('/home')
    expect(screen.getByRole('heading', { name: /invoice intake/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /camera capture/i })).toHaveAttribute('href', '/camera-capture')
    expect(screen.getByRole('link', { name: /file upload/i })).toHaveAttribute('href', '/file-upload')
  })

  it('navigates from the post-login page to camera capture', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))
    fireEvent.click(screen.getByRole('link', { name: /camera capture/i }))

    expect(window.location.pathname).toBe('/camera-capture')
    expect(screen.getByRole('heading', { name: /camera capture route/i })).toBeInTheDocument()
  })

  it('navigates from the post-login page to file upload', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign in with google/i }))
    fireEvent.click(screen.getByRole('link', { name: /file upload/i }))

    expect(window.location.pathname).toBe('/file-upload')
    expect(screen.getByRole('heading', { name: /file upload route/i })).toBeInTheDocument()
  })

  it('redirects an unauthenticated feature route to the login page', () => {
    window.history.replaceState({}, '', '/camera-capture')
    render(<App />)

    expect(window.location.pathname).toBe('/')
    expect(screen.getByRole('heading', { name: /sign in to invoice intake/i })).toBeInTheDocument()
  })
})
