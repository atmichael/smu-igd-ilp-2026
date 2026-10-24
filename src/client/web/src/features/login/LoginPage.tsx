import { Paper, Stack, Typography } from '@mui/material'
import { GoogleSignInButton } from './GoogleSignInButton'

interface LoginPageProps {
  onCredential: (credential: string) => void
}

export function LoginPage({ onCredential }: LoginPageProps) {
  return (
    <main className="app-shell">
      <Paper className="app-shell__confirmation" elevation={0} sx={{ maxWidth: 560, mx: 'auto', mt: 8 }}>
        <Stack spacing={2} sx={{ alignItems: 'flex-start' }}>
          <Typography variant="h4" component="h1">
            Sign in to Invoice Intake
          </Typography>
          <Typography>Sign in with your Google account to continue.</Typography>
          <GoogleSignInButton onCredential={onCredential} />
        </Stack>
      </Paper>
    </main>
  )
}
