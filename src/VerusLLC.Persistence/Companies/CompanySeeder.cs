using Microsoft.Extensions.Logging;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Domain.Common;
using VerusLLC.Domain.Companies;

namespace VerusLLC.Persistence.Companies;

public sealed class CompanySeeder(ICompanyRepository companies, ILogger<CompanySeeder> logger)
{
    private static readonly (string Name, string WebsiteUrl)[] SampleCompanies =
    [
        ("Microsoft", "https://www.microsoft.com"),
        ("GitHub", "https://github.com"),
        ("Shopify", "https://www.shopify.com"),
        ("Atlassian", "https://www.atlassian.com"),
        ("Contoso", "https://contoso.com"),
        ("Fabrikam", "https://fabrikam.com"),
        ("Acme Labs", "https://acmelabs.com"),
        ("Northwind Traders", "https://northwindtraders.com"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await companies.GetAllAsync(cancellationToken);

        if (existing.Count > 0)
        {
            return;
        }

        var seeded = 0;

        foreach (var (name, websiteUrl) in SampleCompanies)
        {
            try
            {
                await companies.AddAsync(Company.Create(name, websiteUrl), cancellationToken);
                seeded++;
            }
            catch (DomainException exception)
            {
                logger.LogWarning(
                    "VerusLLC Seed: sample company {Name} was rejected by {ErrorCode}.",
                    name,
                    exception.Error.Code);
            }
        }

        logger.LogInformation("VerusLLC Seed: stored {Count} sample companies.", seeded);
    }
}
