const storageKey = 'ilp.googleIdToken'

let googleIdToken: string | null = null

function getExpiryMs(token: string): number | null {
  try {
    const payload = token.split('.')[1]
    if (!payload) {
      return null
    }
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    const exp = (JSON.parse(json) as { exp?: unknown }).exp
    return typeof exp === 'number' ? exp * 1000 : null
  } catch {
    return null
  }
}

function isExpired(token: string): boolean {
  const expiry = getExpiryMs(token)
  return expiry !== null && expiry <= Date.now()
}

function readStored(): string | null {
  try {
    return window.sessionStorage.getItem(storageKey)
  } catch {
    return null
  }
}

function writeStored(token: string | null): void {
  try {
    if (token === null) {
      window.sessionStorage.removeItem(storageKey)
    } else {
      window.sessionStorage.setItem(storageKey, token)
    }
  } catch {
    // storage unavailable; token stays in memory only
  }
}

export function setGoogleIdToken(token: string): void {
  googleIdToken = token
  writeStored(token)
}

export function clearGoogleIdToken(): void {
  googleIdToken = null
  writeStored(null)
}

export function getGoogleIdToken(): string | null {
  if (googleIdToken === null) {
    const stored = readStored()
    // A stored token must be a decodable, unexpired JWT to be restored after a refresh.
    if (stored !== null) {
      const expiry = getExpiryMs(stored)
      if (expiry !== null && expiry > Date.now()) {
        googleIdToken = stored
      } else {
        writeStored(null)
      }
    }
  }

  if (googleIdToken !== null && isExpired(googleIdToken)) {
    clearGoogleIdToken()
  }

  return googleIdToken
}
