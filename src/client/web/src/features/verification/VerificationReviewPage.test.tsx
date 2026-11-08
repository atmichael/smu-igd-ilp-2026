import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { VerificationReviewPage } from './VerificationReviewPage'

const run = {
  verificationRunId: 'run-1',
  documentId: '1',
  documentVariant: 'FullTaxInvoice',
  status: 'Blocked' as const,
  rulePackageVersionId: 'iras-sg-invoice-rules-v1',
  checks: [{
    verificationCheckId: 'check-1',
    fieldIdentifier: 'supplier-name',
    outcome: 'Fail' as const,
    reasonCode: 'REQUIRED_FIELD_MISSING_OR_UNSUPPORTED',
    message: 'Supplier name is missing from source evidence.',
    evidenceReferenceIds: [],
    requiresReview: true,
  }],
  completedAt: '2026-11-08T00:00:00Z',
}

describe('VerificationReviewPage', () => {
  it('shows the blocked run and resolution controls without payment approval', () => {
    render(<VerificationReviewPage run={run} />)
    expect(screen.getByText('Verification review')).toBeTruthy()
    expect(screen.getByText('Blocked')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Resolve review' })).toBeTruthy()
    expect(screen.queryByText(/payment approval/i)).toBeNull()
  })
})
