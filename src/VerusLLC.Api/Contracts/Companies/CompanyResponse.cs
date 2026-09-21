using VerusLLC.Application.Companies;

namespace VerusLLC.Api.Contracts.Companies;

public sealed record CompanyResponse(Guid Id, string Name, string WebsiteUrl)
{
    public static CompanyResponse From(CompanyDto company) =>
        new(company.Id, company.Name, company.WebsiteUrl);
}
