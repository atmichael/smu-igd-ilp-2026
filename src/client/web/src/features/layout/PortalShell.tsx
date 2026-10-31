import { Box, Button, Container, Link, Typography } from '@mui/material'
import type { MouseEvent, ReactNode } from 'react'

interface PortalShellProps {
  activePath: string
  onNavigate: (path: string) => void
  onSignOut: () => void
  children: ReactNode
}

const navigationItems = [
  { label: 'Camera capture', href: '/camera-capture', icon: 'camera', group: true },
  { label: 'File upload', href: '/file-upload', icon: 'upload', group: true },
  { label: 'Company', href: '/company', icon: 'company', group: false },
  { label: 'Document hub', href: '/document', icon: 'documents', group: false },
  { label: 'Export', href: '/export', icon: 'export', group: false },
] as const

function NavigationIcon({ name }: { name: (typeof navigationItems)[number]['icon'] }) {
  const paths = {
    camera: (
      <>
        <path d="M4 7h3l1.5-2h7L17 7h3v12H4z" />
        <circle cx="12" cy="13" r="3" />
      </>
    ),
    upload: (
      <>
        <path d="M5 3h9l5 5v13H5zM14 3v5h5" />
        <path d="M12 17V11m-2.5 2.5L12 11l2.5 2.5" />
      </>
    ),
    company: (
      <>
        <path d="M4 21V4h10v17M14 9h6v12M2 21h20" />
        <path d="M8 8h2m-2 4h2m-2 4h2m7-5h1m-1 4h1m-1 4h1" />
      </>
    ),
    documents: (
      <>
        <path d="M6 3h9l4 4v14H6zM15 3v4h4M3 7v14h3" />
        <path d="M9 11h7m-7 4h7" />
      </>
    ),
    export: (
      <>
        <path d="M12 15V3m-4 4 4-4 4 4M5 13v7h14v-7" />
      </>
    ),
  }

  return (
    <svg
      className="portal__nav-icon"
      aria-hidden="true"
      focusable="false"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {paths[name]}
    </svg>
  )
}

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
                  <NavigationIcon name={item.icon} />
                  <span className="portal__nav-label">{item.label}</span>
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
                <NavigationIcon name={item.icon} />
                <span className="portal__nav-label">{item.label}</span>
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
