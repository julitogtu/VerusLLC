using VerusLLC.Domain.Common;

namespace VerusLLC.Domain.Companies;

public sealed class Company : Entity
{
    public const int NameMinLength = 3;

    public const int NameMaxLength = 100;

    private Company(Guid id, string name, string websiteUrl)
        : base(id)
    {
        Name = name;
        WebsiteUrl = websiteUrl;
    }

    public string Name { get; private set; }

    public string WebsiteUrl { get; private set; }

    public static Company Create(string? name, string? websiteUrl)
    {
        var normalizedName = NormalizeName(name);
        var uri = ParseWebsiteUrl(websiteUrl);

        if (!CompanyNameHostPolicy.IsRelevant(normalizedName, uri))
        {
            throw new DomainException(CompanyErrors.NameHostMismatch);
        }

        return new Company(Guid.CreateVersion7(), normalizedName, NormalizeWebsiteUrl(uri));
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(CompanyErrors.NameRequired);
        }

        var normalized = string.Join(
            ' ',
            name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return normalized.Length switch
        {
            < NameMinLength => throw new DomainException(CompanyErrors.NameTooShort),
            > NameMaxLength => throw new DomainException(CompanyErrors.NameTooLong),
            _ => normalized
        };
    }

    private static Uri ParseWebsiteUrl(string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            throw new DomainException(CompanyErrors.WebsiteUrlRequired);
        }

        if (!Uri.TryCreate(websiteUrl.Trim(), UriKind.Absolute, out var uri))
        {
            throw new DomainException(CompanyErrors.WebsiteUrlInvalid);
        }

        var isWeb = uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

        if (!isWeb || !uri.Host.Trim('.').Contains('.'))
        {
            throw new DomainException(CompanyErrors.WebsiteUrlInvalid);
        }

        return uri;
    }

    private static string NormalizeWebsiteUrl(Uri uri)
    {
        var normalized = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Port = uri.IsDefaultPort ? -1 : uri.Port
        }.Uri.ToString();

        return uri.AbsolutePath == "/" && normalized.EndsWith('/')
            ? normalized[..^1]
            : normalized;
    }
}
