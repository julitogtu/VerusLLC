using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VerusLLC.Application.Companies;
using VerusLLC.Application.Companies.Commands.CreateCompany;
using VerusLLC.Application.Companies.Queries.SearchCompanies;

namespace VerusLLC.Tests.Application;

public class SearchCompaniesQueryTests
{
    private static async Task<ISender> SeededAsync(ServiceProvider host)
    {
        var sender = host.GetRequiredService<ISender>();

        foreach (var slug in new[] { "microsoft", "microsoftware", "microsoftworks", "notmicrosoft", "contoso" })
        {
            var name = char.ToUpperInvariant(slug[0]) + slug[1..];
            var created = await sender.Send(new CreateCompanyCommand(name, $"https://{slug}.com"));

            Assert.True(created.IsSuccess, $"seed failed for {name}: {created.Error.Code}");
        }

        return sender;
    }

    private static string[] Names(IReadOnlyList<CompanyDto> companies) =>
        companies.Select(company => company.Name).ToArray();

    [Fact]
    public async Task NoFilters_ReturnsEveryCompany()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    [InlineData(null)]
    public async Task BlankFilters_BehaveLikeAbsentFilters(string? blank)
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(blank, blank, blank));

        Assert.Equal(5, result.Value.Count);
    }

    [Fact]
    public async Task EmptyRepository_ReturnsAnEmptyList()
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();

        var result = await sender.Send(new SearchCompaniesQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task NoMatches_ReturnsAnEmptyListRatherThanEverything()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Search: "zzzznothing"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task PartialName_MatchesEveryCompanyContainingIt()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Name: "microsoft"));

        Assert.Equal(4, result.Value.Count);
        Assert.DoesNotContain("Contoso", Names(result.Value));
    }

    [Fact]
    public async Task CompleteName_MatchesTheExactCompany()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Name: "Microsoftware"));

        Assert.Equal(["Microsoftware"], Names(result.Value));
    }

    [Theory]
    [InlineData("MICROSOFTWARE")]
    [InlineData("microsoftware")]
    [InlineData("  Microsoftware  ")]
    [InlineData("Mícrosoftware")]
    public async Task NameFilter_IgnoresCaseAccentsAndSurroundingSpace(string term)
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Name: term));

        Assert.Equal(["Microsoftware"], Names(result.Value));
    }

    [Fact]
    public async Task DomainFilter_MatchesTheExactHostOnly()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Domain: "microsoft.com"));

        Assert.Equal(["Microsoft"], Names(result.Value));
    }

    [Fact]
    public async Task DomainFilter_WithAnUnknownDomain_ReturnsNothing()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Domain: "unknown-domain.example"));

        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task CombinedFilters_AreAndedTogether()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var matching = await sender.Send(
            new SearchCompaniesQuery(Name: "microsoft", Domain: "microsoftware.com"));

        Assert.Equal(["Microsoftware"], Names(matching.Value));

        var conflicting = await sender.Send(
            new SearchCompaniesQuery(Name: "contoso", Domain: "microsoft.com"));

        Assert.Empty(conflicting.Value);
    }

    [Fact]
    public async Task Ranking_PutsExactBeforePrefixAndPrefixBeforePartial()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Search: "microsoft"));

        Assert.Equal(
            ["Microsoft", "Microsoftware", "Microsoftworks", "Notmicrosoft"],
            Names(result.Value));
    }

    [Fact]
    public async Task Ranking_BreaksTiesByNameThenId()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Search: "microsoft"));
        var tied = Names(result.Value).Where(name => name.StartsWith("Microsoftw")).ToArray();

        Assert.Equal(["Microsoftware", "Microsoftworks"], tied);
    }

    [Fact]
    public async Task Ranking_ExcludesCompaniesThatScoreZero()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery(Search: "microsoft"));

        Assert.DoesNotContain("Contoso", Names(result.Value));
    }

    [Fact]
    public async Task Ranking_IsDeterministicAcrossRuns()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var first = await sender.Send(new SearchCompaniesQuery(Search: "microsoft"));
        var second = await sender.Send(new SearchCompaniesQuery(Search: "microsoft"));

        Assert.Equal(
            first.Value.Select(company => company.Id),
            second.Value.Select(company => company.Id));
    }

    [Fact]
    public async Task ResultsAreMaterializedDtos()
    {
        using var host = TestHost.Create();
        var sender = await SeededAsync(host);

        var result = await sender.Send(new SearchCompaniesQuery());

        Assert.IsType<CompanyDto[]>(result.Value);
    }
}
