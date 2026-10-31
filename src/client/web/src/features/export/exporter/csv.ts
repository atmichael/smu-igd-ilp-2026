const formulaPrefix = /^[=+\-@\t\r]/

export function escapeCsvField(value: string | number | null | undefined, isText = true): string {
  if (value === null || value === undefined) return ''
  let text = String(value)
  if (isText && formulaPrefix.test(text)) text = `'${text}`
  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text
}

export function toCsv(headers: readonly string[], rows: ReadonlyArray<ReadonlyArray<string | number | null | undefined>>): string {
  const lines = [headers.map((header) => escapeCsvField(header, false)).join(',')]
  for (const row of rows) {
    lines.push(row.map((cell) => escapeCsvField(cell, typeof cell !== 'number')).join(','))
  }
  return lines.join('\r\n') + '\r\n'
}
