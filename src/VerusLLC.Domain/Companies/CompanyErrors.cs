using VerusLLC.Domain.Common;

namespace VerusLLC.Domain.Companies;

public static class CompanyErrors
{
    public static readonly DomainError NameRequired =
        new("company.name.required", "Company name is required.");

    public static readonly DomainError NameTooShort =
        new("company.name.too_short",
            $"Company name must be at least {Company.NameMinLength} characters.");

    public static readonly DomainError NameTooLong =
        new("company.name.too_long",
            $"Company name must not exceed {Company.NameMaxLength} characters.");

    public static readonly DomainError WebsiteUrlRequired =
        new("company.website_url.required", "Website URL is required.");

    public static readonly DomainError WebsiteUrlInvalid =
        new("company.website_url.invalid",
            "Website URL must be an absolute http or https URL with a valid host.");

    public static readonly DomainError NameHostMismatch =
        new("company.name_host.mismatch",
            "Company name is not related to the host of the website URL.");
}
