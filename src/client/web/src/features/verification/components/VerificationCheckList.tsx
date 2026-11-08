import { Alert, List, ListItem, ListItemText, Stack, Typography } from '@mui/material'
import type { VerificationCheck } from '../model/verification-model'

interface VerificationCheckListProps {
  checks: VerificationCheck[]
}

export function VerificationCheckList({ checks }: VerificationCheckListProps) {
  return (
    <Stack spacing={2}>
      <Typography variant="h6">Verification checks</Typography>
      {checks.map((check) => (
        <Alert key={check.verificationCheckId} severity={check.outcome === 'Pass' ? 'success' : check.outcome === 'NotApplicable' ? 'info' : 'warning'}>
          <List disablePadding>
            <ListItem disableGutters>
              <ListItemText
                primary={`${check.fieldIdentifier ?? check.reasonCode}: ${check.outcome}`}
                secondary={check.message}
              />
            </ListItem>
          </List>
        </Alert>
      ))}
    </Stack>
  )
}
