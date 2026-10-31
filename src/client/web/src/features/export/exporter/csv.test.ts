import { describe, expect, it } from 'vitest'
import { escapeCsvField, toCsv } from './csv'

describe('csv', () => {
  it('quotes fields containing commas, quotes and line breaks', () => {
    expect(escapeCsvField('a,b')).toBe('"a,b"')
    expect(escapeCsvField('say "hi"')).toBe('"say ""hi"""')
    expect(escapeCsvField('a\nb')).toBe('"a\nb"')
  })

  it('neutralises spreadsheet formulas in text but keeps numbers intact', () => {
    expect(escapeCsvField('=SUM(A1)')).toBe("'=SUM(A1)")
    expect(escapeCsvField(-5, false)).toBe('-5')
    expect(toCsv(['n'], [[-5]])).toBe('n\r\n-5\r\n')
  })

  it('renders empty values as empty fields', () => {
    expect(toCsv(['a', 'b'], [[null, undefined]])).toBe('a,b\r\n,\r\n')
  })
})
