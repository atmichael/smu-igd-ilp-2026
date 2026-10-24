import { Button, Paper, Stack, Typography } from '@mui/material'

interface SubmissionConfirmationProps {
  sourceDocumentId: string
  status: string
  onBack: () => void
}

export function SubmissionConfirmation({ sourceDocumentId, status, onBack }: SubmissionConfirmationProps) {
  return (
    <Paper elevation={0} className="app-shell__confirmation">
      <Stack spacing={1}>
        <Typography variant="h5" component="h2">
          Document submitted
        </Typography>
        <Typography>Source document ID: {sourceDocumentId}</Typography>
        <Typography>Status: {status}</Typography>
        <Button variant="contained" onClick={onBack}>
          Back to source selection
        </Button>
      </Stack>
    </Paper>
  )
}
