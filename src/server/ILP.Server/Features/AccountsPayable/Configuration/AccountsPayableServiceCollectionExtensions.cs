using ILP.Server.Features.AccountsPayable.Service;
using ILP.Shared.AccountsPayable.Abstraction;

namespace ILP.Server.Features.AccountsPayable.Configuration;

public static class AccountsPayableServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsPayable(this IServiceCollection services)
    {
        services.AddScoped<IAccountsPayableService, AccountsPayableService>();
        services.AddSingleton<IAccountsPayableCodingProcessor, InboxAccountsPayableCodingProcessor>();
        return services;
    }
}
