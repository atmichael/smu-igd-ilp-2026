import { useState, type FormEvent } from 'react'
import { Alert, Button, Paper, Stack, TextField, Typography } from '@mui/material'
import { createExporter } from './exporter/exporter-factory'
import { downloadCsv, getDocumentsForExport } from './service/export-service'
import { XeroIcon } from './XeroIcon'

type ExportStatus =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'success'; count: number }
  | { kind: 'error'; message: string }

function toLocalDateInput(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

export function DocumentExportPage() {
  const [createdFrom, setCreatedFrom] = useState(() => {
    const now = new Date()
    return toLocalDateInput(new Date(now.getFullYear(), now.getMonth(), 1))
  })
  const [createdTo, setCreatedTo] = useState(() => toLocalDateInput(new Date()))
  const [status, setStatus] = useState<ExportStatus>({ kind: 'idle' })
  const invalidRange = createdFrom !== '' && createdTo !== '' && createdFrom > createdTo

  const handleExport = async (event: FormEvent) => {
    event.preventDefault()
    if (invalidRange) return
    setStatus({ kind: 'loading' })
    try {
      const exporter = createExporter('xero')
      const result = await getDocumentsForExport({ createdFrom, createdTo })
      if (result.items.length === 0) {
        setStatus({ kind: 'error', message: 'No documents were found for the selected dates.' })
        return
      }
      downloadCsv(exporter.fileName(new Date()), exporter.toCsv(result.items))
      setStatus({ kind: 'success', count: result.items.length })
    } catch (reason: unknown) {
      setStatus({
        kind: 'error',
        message: reason instanceof Error ? reason.message : 'The documents could not be exported.',
      })
    }
  }

  return (
    <Stack spacing={3}>
      <div className="page-heading">
        <div>
          <Typography variant="h4" component="h1">Export</Typography>
          <Typography color="text.secondary">
            Download extracted documents as a CSV file for your accounting software.
          </Typography>
        </div>
      </div>
      <Paper variant="outlined" component="form" className="export-form" onSubmit={handleExport}>
        <div className="export-form__filters">
        <TextField
          label="Created from"
          type="date"
          value={createdFrom}
          onChange={(event) => setCreatedFrom(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          size="small"
        />
        <TextField
          label="Created to"
          type="date"
          value={createdTo}
          onChange={(event) => setCreatedTo(event.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
          error={invalidRange}
          helperText={invalidRange ? 'Created to must not be earlier than created from.' : undefined}
          size="small"
        />
        </div>
        <div className="export-form__actions">
        <Button
          type="submit"
          variant="contained"
          startIcon={<XeroIcon />}
          disabled={status.kind === 'loading' || invalidRange}
        >
          Export to Xero
        </Button>
        </div>
      </Paper>
      {status.kind === 'error' && <Alert severity="error">{status.message}</Alert>}
      {status.kind === 'success' && (
        <Alert severity="success">Exported {status.count} document(s) in Xero bill format.</Alert>
      )}
    </Stack>
  )
}
