using System.ComponentModel.DataAnnotations;

namespace VerusLLC.Api.Contracts.Companies;

public sealed record CompanySearchRequest
{
    [StringLength(200)]
    public string? Name { get; init; }

    [StringLength(200)]
    public string? Domain { get; init; }

    [StringLength(200)]
    public string? Search { get; init; }
}
