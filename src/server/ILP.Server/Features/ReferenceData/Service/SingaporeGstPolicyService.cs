using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class SingaporeGstPolicyService(SingaporeGstPolicyValidator validator)
{
    public IReadOnlyList<string> Validate(BusinessRulePackageDto package) => validator.Validate(package);
}
