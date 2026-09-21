using System.Diagnostics;
using VerusLLC.Domain.Common;
using VerusLLC.Domain.Companies;

namespace VerusLLC.Tests.Domain;

public class CompanyNameHostPolicyTests
{
    [Theory]
    [InlineData("Microsoft", "https://microsoft.com")]
    [InlineData("Microsoft Corporation", "https://microsoft.com")]
    [InlineData("Acme Labs", "https://acmelabs.com")]
    [InlineData("Microsoft", "https://www.microsoft.com")]
    [InlineData("Acme Labs Inc", "https://acmelabs.com")]
    [InlineData("Acme-Labs", "https://acmelabs.com")]
    [InlineData("Ácme Labs", "https://acmelabs.com")]
    [InlineData("Contoso", "https://contoso.co.uk")]
    public void Relevant_NamesAreAccepted(string name, string websiteUrl)
    {
        Assert.True(CompanyNameHostPolicy.IsRelevant(name, new Uri(websiteUrl)));

        var company = Company.Create(name, websiteUrl);

        Assert.NotEqual(Guid.Empty, company.Id);
    }

    [Theory]
    [InlineData("Microsoft", "https://contoso.com")]
    [InlineData("Microsoft", "https://notmicrosoft.com")]
    [InlineData("Microsoft", "https://microsoftonline.com")]
    [InlineData("Acme", "https://acmelabs.com")]
    public void UnrelatedNamesAreRejected(string name, string websiteUrl)
    {
        Assert.False(CompanyNameHostPolicy.IsRelevant(name, new Uri(websiteUrl)));

        var exception = Assert.Throws<DomainException>(() => Company.Create(name, websiteUrl));

        Assert.Equal("company.name_host.mismatch", exception.Error.Code);
    }

    [Theory]
    [InlineData("Microsoft", "https://portal.microsoft.com")]
    [InlineData("Microsoft", "https://login.microsoft.com")]
    [InlineData("Contoso", "https://shop.contoso.co.uk")]
    public void SubdomainsAreRejected_DeclaredPolicyLimitation(string name, string websiteUrl)
    {
        Assert.False(CompanyNameHostPolicy.IsRelevant(name, new Uri(websiteUrl)));

        var exception = Assert.Throws<DomainException>(() => Company.Create(name, websiteUrl));

        Assert.Equal("company.name_host.mismatch", exception.Error.Code);
    }

    [Fact]
    public void Policy_ResolvesNoDnsAndMakesNoNetworkCall()
    {
        var unresolvable = new Uri("https://acmelabs.invalid");

        var stopwatch = Stopwatch.StartNew();
        var relevant = CompanyNameHostPolicy.IsRelevant("Acme Labs", unresolvable);
        stopwatch.Stop();

        Assert.True(relevant);
        Assert.True(
            stopwatch.ElapsedMilliseconds < 500,
            $"Policy took {stopwatch.ElapsedMilliseconds}ms, which suggests it resolved the host.");
    }

    [Fact]
    public void Policy_IsDeterministic()
    {
        var uri = new Uri("https://acmelabs.com");

        var results = Enumerable
            .Range(0, 100)
            .Select(_ => CompanyNameHostPolicy.IsRelevant("Acme Labs", uri))
            .Distinct()
            .ToArray();

        Assert.Single(results);
        Assert.True(results[0]);
    }
}
