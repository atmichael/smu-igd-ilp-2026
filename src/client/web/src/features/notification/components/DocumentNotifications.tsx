import { Alert, Snackbar } from '@mui/material'
import { useEffect, useState } from 'react'
import {
  formatDocumentNotification,
  streamDocumentNotifications,
} from '../service/notification-service'

export function DocumentNotifications() {
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    void streamDocumentNotifications(
      (notification) => setMessage(formatDocumentNotification(notification)),
      controller.signal,
    )
    return () => controller.abort()
  }, [])

  return (
    <Snackbar
      open={message !== null}
      autoHideDuration={8000}
      onClose={(_, reason) => reason !== 'clickaway' && setMessage(null)}
      anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
    >
      <Alert severity="info" variant="filled" onClose={() => setMessage(null)}>
        {message}
      </Alert>
    </Snackbar>
  )
}
