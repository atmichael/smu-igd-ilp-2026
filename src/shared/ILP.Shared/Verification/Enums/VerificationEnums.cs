namespace ILP.Shared.Verification.Enums;

public enum VerificationOutcome { Pass, Fail, Unavailable, NotApplicable }
public enum VerificationRunStatus { Verified, Blocked, Superseded }
public enum VerificationCheckType { FieldPresence, Title, Adjustment, Arithmetic, Currency, Vendor, NumericFormat, Applicability }
