using System.Globalization;
using System.Text;

namespace VerusLLC.Domain.Companies;

public static class CompanyNameHostPolicy
{
    private static readonly HashSet<string> LegalSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "inc", "incorporated", "llc", "llp", "ltd", "limited", "plc", "corp", "corporation",
        "co", "company", "group", "holding", "holdings", "gmbh", "ag", "bv", "nv", "sa", "sas",
        "sarl", "srl", "spa", "sl", "ab", "as", "oy", "kk", "pty", "pte"
    };

    private static readonly HashSet<string> PublicSecondLevelLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "co", "com", "net", "org", "gov", "edu", "ac", "mil", "or", "ne", "go", "gob", "nom"
    };

    public static bool IsRelevant(string name, Uri websiteUrl)
    {
        ArgumentNullException.ThrowIfNull(websiteUrl);

        var comparableName = Canonicalize(StripLegalSuffixes(name ?? string.Empty));
        var comparableHost = Canonicalize(GetRegistrableName(websiteUrl.Host));

        return comparableName.Length > 0
            && comparableHost.Length > 0
            && string.Equals(comparableName, comparableHost, StringComparison.Ordinal);
    }

    private static string GetRegistrableName(string host)
    {
        var labels = host.TrimEnd('.').Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (labels.Length == 0)
        {
            return string.Empty;
        }

        var start = labels.Length > 1 && labels[0].Equals("www", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        var end = labels.Length - 1;

        if (end - start >= 2 && PublicSecondLevelLabels.Contains(labels[end - 1]))
        {
            end--;
        }

        return end <= start
            ? string.Join('.', labels[start..])
            : string.Join('.', labels[start..end]);
    }

    private static string StripLegalSuffixes(string name)
    {
        var words = name.Split(
            [' ', '\t', '\n', '\r', ',', '.', '-', '_'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var end = words.Length;

        while (end > 0 && LegalSuffixes.Contains(words[end - 1]))
        {
            end--;
        }

        return end == 0 ? name : string.Join(' ', words[..end]);
    }

    private static string Canonicalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
