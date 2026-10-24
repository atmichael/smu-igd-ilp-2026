using ILP.Server.Features.CompaniesManagement.Repository;
using ILP.Server.Features.CompaniesManagement.Service;

namespace ILP.Server.Features.CompaniesManagement.Configuration;

public static class CompanyManagementServiceCollectionExtensions
{
    public static IServiceCollection AddCompaniesManagement(this IServiceCollection services)
    {
        services.AddSingleton<ICompanyRepository, SqliteCompanyRepository>();
        services.AddScoped<ICompanyService, CompanyService>();
        return services;
    }
}
