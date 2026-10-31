import type { DocumentRecord } from '../../document/model/document-model'

export type ExportProviderId = 'xero'

export interface DocumentExporter {
  id: ExportProviderId
  label: string
  fileName: (generatedAt: Date) => string
  toCsv: (documents: readonly DocumentRecord[]) => string
}
