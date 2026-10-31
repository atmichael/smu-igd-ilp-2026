import type { DocumentExporter, ExportProviderId } from './export-provider'
import { xeroExporter } from './xero-exporter'

const exporters: Record<ExportProviderId, DocumentExporter> = {
  xero: xeroExporter,
}

export function createExporter(provider: ExportProviderId): DocumentExporter {
  const exporter = exporters[provider]
  if (!exporter) throw new Error(`Unsupported export provider: ${provider}`)
  return exporter
}
