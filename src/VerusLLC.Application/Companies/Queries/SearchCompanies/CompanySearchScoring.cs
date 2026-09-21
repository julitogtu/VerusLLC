using System.Globalization;
using System.Text;
using VerusLLC.Domain.Companies;

namespace VerusLLC.Application.Companies.Queries.SearchCompanies;

internal static class CompanySearchScoring
{
    private const int ExactName = 100;
    private const int NamePrefix = 80;
    private const int NameContains = 60;
    private const int ExactHost = 50;
    private const int HostPrefix = 40;
    private const int HostContains = 20;

    public static IReadOnlyList<CompanyDto> Rank(
        IReadOnlyList<Company> candidates,
        string? name,
        string? domain,
        string? search)
    {
        var nameTerm = Normalize(name);
        var domainTerm = StripWww(Normalize(domain));
        var searchTerm = Normalize(search);

        var matches = new List<(Company Company, int Score)>();

        foreach (var candidate in candidates)
        {
            var candidateName = Normalize(candidate.Name);
            var candidateHost = StripWww(Normalize(HostOf(candidate.WebsiteUrl)));

            if (!MatchesName(candidateName, nameTerm)
                || !MatchesHost(candidateHost, domainTerm)
                || !MatchesAnything(candidateName, candidateHost, searchTerm))
            {
                continue;
            }

            var score = Score(candidateName, candidateHost, nameTerm)
                + Score(candidateName, candidateHost, domainTerm)
                + Score(candidateName, candidateHost, searchTerm);

            matches.Add((candidate, score));
        }

        return matches
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Company.Name, StringComparer.Ordinal)
            .ThenBy(match => match.Company.Id)
            .Select(match => match.Company.ToDto())
            .ToArray();
    }

    private static bool MatchesName(string candidateName, string term) =>
        term.Length == 0 || candidateName.Contains(term, StringComparison.Ordinal);

    private static bool MatchesHost(string candidateHost, string term) =>
        term.Length == 0 || string.Equals(candidateHost, term, StringComparison.Ordinal);

    private static string StripWww(string host) =>
        host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;

    private static bool MatchesAnything(string candidateName, string candidateHost, string term) =>
        term.Length == 0
        || candidateName.Contains(term, StringComparison.Ordinal)
        || candidateHost.Contains(term, StringComparison.Ordinal);

    private static int Score(string candidateName, string candidateHost, string term)
    {
        if (term.Length == 0)
        {
            return 0;
        }

        if (string.Equals(candidateName, term, StringComparison.Ordinal))
        {
            return ExactName;
        }

        if (candidateName.StartsWith(term, StringComparison.Ordinal))
        {
            return NamePrefix;
        }

        if (candidateName.Contains(term, StringComparison.Ordinal))
        {
            return NameContains;
        }

        if (string.Equals(candidateHost, term, StringComparison.Ordinal))
        {
            return ExactHost;
        }

        if (candidateHost.StartsWith(term, StringComparison.Ordinal))
        {
            return HostPrefix;
        }

        return candidateHost.Contains(term, StringComparison.Ordinal) ? HostContains : 0;
    }

    private static string HostOf(string websiteUrl) =>
        Uri.TryCreate(websiteUrl, UriKind.Absolute, out var uri) ? uri.Host : websiteUrl;

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSeparator = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSeparator = builder.Length > 0;
                continue;
            }

            if (pendingSeparator)
            {
                builder.Append(' ');
                pendingSeparator = false;
            }

            builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
