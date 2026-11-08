import { Divider, Stack, Typography } from '@mui/material'
import type { VerificationRun } from '../model/verification-model'
import { VerificationCheckList } from './VerificationCheckList'
import { VerificationDependencyNotice } from './VerificationDependencyNotice'

export function VerificationDetail({ run }: { run: VerificationRun }) {
  return (
    <Stack spacing={2}>
      <Typography variant="h6">Invoice verification</Typography>
      <Typography color="text.secondary">Document variant: {run.documentVariant}</Typography>
      <VerificationDependencyNotice checks={run.checks} />
      <Divider />
      <VerificationCheckList checks={run.checks} />
    </Stack>
  )
}
