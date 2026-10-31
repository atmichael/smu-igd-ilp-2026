import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { PortalShell } from './PortalShell'

describe('PortalShell', () => {
  it('shows an icon and label for each primary navigation item', () => {
    render(
      <PortalShell activePath="/camera-capture" onNavigate={() => undefined} onSignOut={() => undefined}>
        <div />
      </PortalShell>,
    )

    const navigation = screen.getByRole('navigation', { name: /primary navigation/i })
    const items = [
      'Camera capture',
      'File upload',
      'Company',
      'Document hub',
      'Export',
    ]

    for (const label of items) {
      const link = within(navigation).getByRole('link', { name: label })
      expect(link.querySelector('svg')).toBeInTheDocument()
      expect(link.querySelector('svg')).toHaveAttribute('aria-hidden', 'true')
    }

    expect(within(navigation).getByRole('link', { name: 'Camera capture' })).toHaveAttribute('aria-current', 'page')
  })
})
