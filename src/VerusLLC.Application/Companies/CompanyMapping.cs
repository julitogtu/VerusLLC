using VerusLLC.Domain.Companies;

namespace VerusLLC.Application.Companies;

internal static class CompanyMapping
{
    public static CompanyDto ToDto(this Company company) =>
        new(company.Id, company.Name, company.WebsiteUrl);
}
