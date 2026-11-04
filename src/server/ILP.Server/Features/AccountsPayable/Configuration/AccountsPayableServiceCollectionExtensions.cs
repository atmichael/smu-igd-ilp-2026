using ILP.Server.Features.AccountsPayable.Service;

namespace ILP.Server.Features.AccountsPayable.Configuration;

public static class AccountsPayableServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsPayable(this IServiceCollection services)
    {
        services.AddScoped<IAccountsPayableService, AccountsPayableService>();
        return services;
    }
}
