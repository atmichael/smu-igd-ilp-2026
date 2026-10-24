import { useEffect, useRef, useState } from 'react'
import type { GoogleIdentityCredential } from '../../common/model/google-identity-model'

interface GoogleSignInButtonProps {
  onCredential: (credential: string) => void
}

const GOOGLE_IDENTITY_SCRIPT = 'https://accounts.google.com/gsi/client'

export function GoogleSignInButton({ onCredential }: GoogleSignInButtonProps) {
  const buttonRef = useRef<HTMLDivElement | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID

    if (!clientId) {
      setError('Google sign-in is not configured. Set VITE_GOOGLE_CLIENT_ID and restart the web app.')
      return
    }

    const initialize = () => {
      if (!buttonRef.current || !window.google) {
        setError('Google sign-in could not be loaded. Please refresh and try again.')
        return
      }

      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: (response: GoogleIdentityCredential) => {
          if (response.credential) {
            onCredential(response.credential)
          } else {
            setError('Google sign-in did not return a credential. Please try again.')
          }
        },
      })
      window.google.accounts.id.renderButton(buttonRef.current, {
        theme: 'outline',
        size: 'large',
        text: 'signin_with',
        shape: 'rectangular',
      })
    }

    const script = document.querySelector<HTMLScriptElement>(`script[src="${GOOGLE_IDENTITY_SCRIPT}"]`)

    if (window.google) {
      initialize()
      return
    }

    const googleScript = script ?? document.createElement('script')
    const onLoad = () => initialize()
    const onError = () => setError('Google sign-in could not be loaded. Please refresh and try again.')

    googleScript.addEventListener('load', onLoad)
    googleScript.addEventListener('error', onError)

    if (!script) {
      googleScript.src = GOOGLE_IDENTITY_SCRIPT
      googleScript.async = true
      googleScript.defer = true
      document.head.appendChild(googleScript)
    }

    return () => {
      googleScript.removeEventListener('load', onLoad)
      googleScript.removeEventListener('error', onError)
    }
  }, [onCredential])

  return (
    <div>
      {error ? <p role="alert">{error}</p> : null}
      <div ref={buttonRef} />
    </div>
  )
}
