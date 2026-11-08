import { Alert, Stack } from '@mui/material'
import type { VerificationCheck } from '../model/verification-model'

export function VerificationDependencyNotice({ checks }: { checks: VerificationCheck[] }) {
  const unavailable = checks.filter(check => check.outcome === 'Unavailable')
  if (!unavailable.length) return null
  return (
    <Stack spacing={1}>
      {unavailable.map(check => (
        <Alert key={check.verificationCheckId} severity="warning">
          {check.fieldIdentifier ?? check.reasonCode}: {check.message}
        </Alert>
      ))}
    </Stack>
  )
}
