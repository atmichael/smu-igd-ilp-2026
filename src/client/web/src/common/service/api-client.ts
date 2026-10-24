import { getGoogleIdToken } from './authentication-service'

export function apiFetch(input: RequestInfo | URL, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers)
  const token = getGoogleIdToken()

  if (token) {
    headers.set('Authorization', 'Bearer ' + token)
  }

  return fetch(input, { ...init, headers })
}

export async function apiJson<T>(input: RequestInfo | URL, init: RequestInit = {}): Promise<T> {
  const response = await apiFetch(input, init)
  const body = await response.json().catch(() => undefined)

  if (!response.ok) {
    const problem = body as { title?: unknown; detail?: unknown } | undefined
    const message =
      (typeof problem?.detail === 'string' && problem.detail) ||
      (typeof problem?.title === 'string' && problem.title) ||
      'The request could not be completed.'
    throw new Error(message)
  }

  return body as T
}
