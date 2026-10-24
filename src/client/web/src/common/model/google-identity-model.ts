export interface GoogleIdentityCredential {
  credential: string
}

export interface GoogleIdentity {
  accounts: {
    id: {
      initialize(options: {
        client_id: string
        callback: (response: GoogleIdentityCredential) => void
      }): void
      renderButton(element: HTMLElement, options: Record<string, string>): void
    }
  }
}

declare global {
  interface Window {
    google?: GoogleIdentity
  }
}
