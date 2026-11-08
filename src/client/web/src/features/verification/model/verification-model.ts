export type VerificationOutcome = 'Pass' | 'Fail' | 'Unavailable' | 'NotApplicable'
export type VerificationRunStatus = 'Verified' | 'Blocked' | 'Superseded'

export interface VerificationCheck {
  verificationCheckId: string
  fieldIdentifier?: string
  outcome: VerificationOutcome
  reasonCode: string
  message: string
  extractedValue?: string
  expectedValue?: string
  evidenceReferenceIds: string[]
  requiresReview: boolean
}

export interface VerificationRun {
  verificationRunId: string
  documentId: string
  documentVariant: string
  status: VerificationRunStatus
  rulePackageVersionId: string
  checks: VerificationCheck[]
  completedAt: string
}
