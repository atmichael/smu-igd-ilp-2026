import { describe, expect, it } from 'vitest'
import { theme } from './theme'

describe('application theme', () => {
  it('uses the reference slate and emerald palette', () => {
    expect(theme.palette.primary.main).toBe('#059669')
    expect(theme.palette.background.default).toBe('#f1f5f9')
    expect(theme.palette.background.paper).toBe('#ffffff')
    expect(theme.palette.text.primary).toBe('#0f172a')
    expect(theme.palette.text.secondary).toBe('#64748b')
  })

  it('uses the reference rounded surfaces and sans-serif typography', () => {
    expect(theme.shape.borderRadius).toBe(12)
    expect(theme.typography.fontFamily).toContain('system-ui')
  })
})
