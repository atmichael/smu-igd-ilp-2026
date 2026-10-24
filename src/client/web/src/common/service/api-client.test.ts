import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiFetch } from './api-client'
import { clearGoogleIdToken, setGoogleIdToken } from './authentication-service'

describe('apiFetch', () => {
  afterEach(() => {
    clearGoogleIdToken()
    vi.restoreAllMocks()
  })

  it('adds the signed-in Google ID token as a bearer authorization header', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response())
    setGoogleIdToken('google-id-token')

    await apiFetch('/api/source-documents')

    const [, init] = fetchMock.mock.calls[0]
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer ' + 'google-id-token')
  })

  it('does not add a dummy authorization header when no user is signed in', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response())

    await apiFetch('/api/source-documents')

    const [, init] = fetchMock.mock.calls[0]
    expect(new Headers(init?.headers).has('Authorization')).toBe(false)
  })
})
