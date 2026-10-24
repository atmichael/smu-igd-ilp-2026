import { Box, Button, Container, Link, Typography } from '@mui/material'
import type { MouseEvent, ReactNode } from 'react'

interface PortalShellProps {
  activePath: string
  onNavigate: (path: string) => void
  onSignOut: () => void
  children: ReactNode
}

const navigationItems = [
  { label: 'Camera capture', href: '/camera-capture', group: true },
  { label: 'File upload', href: '/file-upload', group: true },
  { label: 'Company', href: '/company' },
  { label: 'Document hub', href: '/document' },
]

export function PortalShell({ activePath, onNavigate, onSignOut, children }: PortalShellProps) {
  const navigate = (event: MouseEvent<HTMLAnchorElement>, path: string) => {
    event.preventDefault()
    onNavigate(path)
  }

  return (
    <Box className="portal">
      <Box component="header" className="portal__header">
        <Container maxWidth="xl" className="portal__header-inner">
          <Link
            href="/home"
            underline="none"
            className="portal__brand"
            onClick={(event) => navigate(event, '/home')}
          >
            <Typography variant="h5" component="span">Invoice Intake</Typography>
          </Link>
          <Box component="nav" aria-label="Primary navigation" className="portal__navigation">
            <Box className="portal__capture-group">
              <Typography className="portal__group-label">Capture</Typography>
              {navigationItems.filter((item) => item.group).map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={activePath === item.href ? 'page' : undefined}
                  onClick={(event) => navigate(event, item.href)}
                >
                  {item.label}
                </Link>
              ))}
            </Box>
            {navigationItems.filter((item) => !item.group).map((item) => (
              <Link
                key={item.href}
                href={item.href}
                aria-current={activePath === item.href ? 'page' : undefined}
                onClick={(event) => navigate(event, item.href)}
              >
                {item.label}
              </Link>
            ))}
          </Box>
          <Button onClick={onSignOut}>Sign out</Button>
        </Container>
      </Box>
      <Container component="main" maxWidth="xl" className="portal__content">
        {children}
      </Container>
    </Box>
  )
}
