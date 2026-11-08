using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class VendorIdentityVerifier
{
    public static VerificationCheckDto Verify(string supplierName, IReadOnlyCollection<string> approvedVendorNames, IReadOnlyCollection<string> approvedAliases, bool referenceAvailable)
    {
        if (!referenceAvailable) return Unavailable();
        var supported = approvedVendorNames.Contains(supplierName, StringComparer.OrdinalIgnoreCase) || approvedAliases.Contains(supplierName, StringComparer.OrdinalIgnoreCase);
        return supported ? Supported(supplierName) : Failed(supplierName);
    }

    private static VerificationCheckDto Unavailable() => new(Guid.NewGuid().ToString("N"), "vendor-identity", VerificationCheckType.Vendor, VerificationOutcome.Unavailable,
        "APPROVED_VENDOR_UNAVAILABLE", "Approved vendor reference data is unavailable.", null, null, [], true, null);
    private static VerificationCheckDto Supported(string name) => new(Guid.NewGuid().ToString("N"), "vendor-identity", VerificationCheckType.Vendor, VerificationOutcome.Pass,
        "SUPPORTED", "Supplier identity matches an approved vendor or alias.", name, name, ["vendor-reference"], false, null);
    private static VerificationCheckDto Failed(string name) => new(Guid.NewGuid().ToString("N"), "vendor-identity", VerificationCheckType.Vendor, VerificationOutcome.Fail,
        "VENDOR_NOT_APPROVED", "Supplier identity does not match an approved vendor or alias.", name, null, ["vendor-reference"], true, null);
}
