import { beforeEach, describe, expect, it, vi } from 'vitest'
import { clearGoogleIdToken, getGoogleIdToken, setGoogleIdToken } from './authentication-service'

function makeToken(expSeconds: number): string {
  return `h.${btoa(JSON.stringify({ exp: expSeconds }))}.s`
}

const nowSeconds = () => Math.floor(Date.now() / 1000)

describe('authentication-service', () => {
  beforeEach(() => {
    clearGoogleIdToken()
    window.sessionStorage.clear()
    vi.useRealTimers()
  })

  it('returns null when the user has not logged in', () => {
    expect(getGoogleIdToken()).toBeNull()
  })

  it('restores a still-valid token after a page refresh', () => {
    const token = makeToken(nowSeconds() + 3600)
    setGoogleIdToken(token)
    // simulate refresh: memory lost, storage retained
    window.sessionStorage.setItem('ilp.googleIdToken', token)
    vi.resetModules()
    return import('./authentication-service').then((fresh) => {
      expect(fresh.getGoogleIdToken()).toBe(token)
    })
  })

  it('discards an expired stored token', () => {
    window.sessionStorage.setItem('ilp.googleIdToken', makeToken(nowSeconds() - 10))
    vi.resetModules()
    return import('./authentication-service').then((fresh) => {
      expect(fresh.getGoogleIdToken()).toBeNull()
      expect(window.sessionStorage.getItem('ilp.googleIdToken')).toBeNull()
    })
  })

  it('returns null once an in-memory token expires', () => {
    setGoogleIdToken(makeToken(nowSeconds() - 1))
    expect(getGoogleIdToken()).toBeNull()
  })
})
