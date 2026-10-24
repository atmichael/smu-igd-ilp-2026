let googleIdToken: string | null = null

export function setGoogleIdToken(token: string): void {
  googleIdToken = token
}

export function clearGoogleIdToken(): void {
  googleIdToken = null
}

export function getGoogleIdToken(): string | null {
  return googleIdToken
}
