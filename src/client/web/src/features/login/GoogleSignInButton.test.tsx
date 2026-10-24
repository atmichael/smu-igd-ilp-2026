import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { GoogleIdentity } from '../../common/model/google-identity-model'
import { GoogleSignInButton } from './GoogleSignInButton'

describe('GoogleSignInButton', () => {
  afterEach(() => {
    delete window.google
    vi.unstubAllEnvs()
  })

  it('renders the Google button and passes the returned credential to its caller', () => {
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', 'google-client-id')
    let callback: ((response: { credential: string }) => void) | undefined
    const initialize = vi.fn((options: { callback: (response: { credential: string }) => void }) => {
      callback = options.callback
    })
    const renderButton = vi.fn()
    window.google = { accounts: { id: { initialize, renderButton } } } as GoogleIdentity
    const onCredential = vi.fn()

    render(<GoogleSignInButton onCredential={onCredential} />)

    expect(initialize).toHaveBeenCalledWith({
      client_id: 'google-client-id',
      callback: expect.any(Function),
    })
    expect(renderButton).toHaveBeenCalledWith(expect.any(HTMLDivElement), expect.objectContaining({
      text: 'signin_with',
    }))

    act(() => callback?.({ credential: 'google-id-token' }))
    expect(onCredential).toHaveBeenCalledWith('google-id-token')
  })

  it('explains how to configure Google sign-in when the client ID is missing', () => {
    vi.stubEnv('VITE_GOOGLE_CLIENT_ID', '')

    render(<GoogleSignInButton onCredential={vi.fn()} />)

    expect(screen.getByRole('alert')).toHaveTextContent(/set VITE_GOOGLE_CLIENT_ID/i)
  })
})
