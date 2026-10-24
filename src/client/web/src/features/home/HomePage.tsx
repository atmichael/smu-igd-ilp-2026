import { Button, Paper, Stack, Typography } from '@mui/material'

interface HomePageProps {
  onNavigate: (path: string) => void
  onSignOut: () => void
}

export function HomePage({ onNavigate, onSignOut }: HomePageProps) {
  return (
    <main className="app-shell">
      <Stack spacing={3} sx={{ maxWidth: 960, mx: 'auto', py: 4, px: 2 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Typography variant="h4" component="h1">
            Invoice Intake
          </Typography>
          <Button onClick={onSignOut}>Sign out</Button>
        </Stack>
        <Paper className="app-shell__confirmation" elevation={0}>
          <Stack spacing={2}>
            <Typography variant="h5" component="h2">
              Choose a document source
            </Typography>
            <Typography>Select how you want to submit your invoice.</Typography>
            <Button
              component="a"
              href="/camera-capture"
              variant="contained"
              sx={{ minHeight: 44 }}
              onClick={(event) => {
                event.preventDefault()
                onNavigate('/camera-capture')
              }}
            >
              Camera capture
            </Button>
            <Button
              component="a"
              href="/file-upload"
              variant="outlined"
              sx={{ minHeight: 44 }}
              onClick={(event) => {
                event.preventDefault()
                onNavigate('/file-upload')
              }}
            >
              File upload
            </Button>
          </Stack>
        </Paper>
      </Stack>
    </main>
  )
}
