import { Alert, Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import type { VerificationRun } from './model/verification-model'
import { VerificationCheckList } from './components/VerificationCheckList'
import { resolveReviewItem } from './service/verification-service'

export function VerificationReviewPage({ run }: { run: VerificationRun }) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string>()
  const [resolved, setResolved] = useState(false)

  const resolve = async () => {
    try {
      await resolveReviewItem(run.verificationRunId, reason)
      setResolved(true)
      setError(undefined)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Resolution could not be saved.')
    }
  }

  return (
    <Stack spacing={2}>
      <Typography variant="h5">Verification review</Typography>
      <Alert severity={run.status === 'Verified' ? 'success' : 'warning'}>{run.status}</Alert>
      <VerificationCheckList checks={run.checks} />
      {!resolved ? (
        <Stack spacing={1} direction={{ xs: 'column', sm: 'row' }}>
          <TextField label="Resolution reason" value={reason} onChange={(event) => setReason(event.target.value)} required fullWidth />
          <Button variant="contained" disabled={!reason.trim()} onClick={() => void resolve()}>Resolve review</Button>
        </Stack>
      ) : <Alert severity="success">Review resolved. Automated verification remains unchanged.</Alert>}
      {error && <Alert severity="error">{error}</Alert>}
    </Stack>
  )
}
