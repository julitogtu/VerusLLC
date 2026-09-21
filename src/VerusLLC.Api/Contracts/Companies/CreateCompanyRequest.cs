using System.ComponentModel.DataAnnotations;

namespace VerusLLC.Api.Contracts.Companies;

public sealed record CreateCompanyRequest
{
    [Required]
    public string? Name { get; init; }

    [Required]
    public string? WebsiteUrl { get; init; }
}
