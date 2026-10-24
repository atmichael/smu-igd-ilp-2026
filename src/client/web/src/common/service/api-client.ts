import { getGoogleIdToken } from './authentication-service'

export function apiFetch(input: RequestInfo | URL, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers)
  const token = getGoogleIdToken()

  if (token) {
    headers.set('Authorization', 'Bearer ' + token)
  }

  return fetch(input, { ...init, headers })
}
