import { apiJson } from '../../../common/service/api-client'
import type { VerificationRun } from '../model/verification-model'

export async function startVerification(documentId: string, evidencePackageId?: string): Promise<VerificationRun> {
  return apiJson('/api/verification-runs', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ documentId, evidencePackageId }),
  })
}

export async function getVerificationRun(runId: string): Promise<VerificationRun> {
  return apiJson(`/api/verification-runs/${runId}`)
}

export async function resolveReviewItem(reviewItemId: string, resolutionReason: string): Promise<void> {
  await apiJson(`/api/verification-review-items/${reviewItemId}/resolve`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ resolutionReason }),
  })
}
