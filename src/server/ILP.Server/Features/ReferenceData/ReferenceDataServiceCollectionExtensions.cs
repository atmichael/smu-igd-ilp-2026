using ILP.Server.Features.ReferenceData.Adapters;
using ILP.Server.Features.ReferenceData.Repository;
using ILP.Server.Features.ReferenceData.Service;

namespace ILP.Server.Features.ReferenceData;

public static class ReferenceDataServiceCollectionExtensions
{
    public static IServiceCollection AddReferenceData(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var root = configuration["ReferenceData:RootPath"] ?? Path.Combine(contentRootPath, "App_Data", "reference-data");
        services.AddSingleton<IReferenceDataRepository>(new FileReferenceDataRepository(root));
        services.AddSingleton<IReferenceDataSourceAdapter, ErpVendorSourceAdapter>();
        services.AddSingleton<IReferenceDataSourceAdapter, ErpPurchasingSourceAdapter>();
        services.AddSingleton<IReferenceDataSourceAdapter, IRasPolicySourceAdapter>();
        services.AddSingleton<ReferenceDataSnapshotValidator>();
        services.AddSingleton<ReferenceDataLookupService>();
        services.AddSingleton<ReferenceAliasResolver>();
        services.AddSingleton<RuleDecisionTraceService>();
        services.AddSingleton<DocumentRuleSelector>();
        services.AddSingleton<SingaporeGstPolicyValidator>();
        services.AddSingleton<SingaporeGstPolicyService>();
        services.AddSingleton<ReferenceChangeApprovalService>();
        services.AddSingleton<ReferenceDataAuditService>();
        services.AddSingleton<ReferenceDataSynchronizationService>();
        services.AddHostedService<ReferenceDataReconciliationBackgroundService>();
        return services;
    }
}
