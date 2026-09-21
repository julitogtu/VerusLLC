using Microsoft.Extensions.DependencyInjection.Extensions;
using VerusLLC.Application.Common.Correlation;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Persistence.Companies;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.TryAddScoped<ICorrelationIdAccessor, NullCorrelationIdAccessor>();
        services.TryAddSingleton<ICompanyRepository, InMemoryCompanyRepository>();
        services.TryAddSingleton<CompanySeeder>();

        return services;
    }
}
