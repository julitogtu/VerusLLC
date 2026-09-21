using VerusLLC.Domain.Common;
using VerusLLC.Domain.Companies;

namespace VerusLLC.Tests.Domain;

public class CompanyCreationTests
{
    [Fact]
    public void Create_WithValidInput_AssignsIdAndNormalizesData()
    {
        var company = Company.Create("  Contoso   Global  ", "HTTPS://WWW.CONTOSOGLOBAL.COM/");

        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal("Contoso Global", company.Name);
        Assert.Equal("https://www.contosoglobal.com", company.WebsiteUrl);
    }

    [Fact]
    public void Create_AssignsADistinctIdToEachCompany()
    {
        var ids = Enumerable
            .Range(0, 50)
            .Select(_ => Company.Create("Contoso", "https://contoso.com").Id)
            .ToArray();

        Assert.Equal(50, ids.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, ids);
    }

    [Theory]
    [InlineData("https://contoso.com:8443/about", "https://contoso.com:8443/about")]
    [InlineData("https://contoso.com/", "https://contoso.com")]
    [InlineData("HTTPS://CONTOSO.COM", "https://contoso.com")]
    public void Create_NormalizesTheWebsiteUrl(string input, string expected)
    {
        Assert.Equal(expected, Company.Create("Contoso", input).WebsiteUrl);
    }

    [Theory]
    [InlineData(null, "company.name.required")]
    [InlineData("", "company.name.required")]
    [InlineData("   ", "company.name.required")]
    [InlineData("C", "company.name.too_short")]
    [InlineData("Ab", "company.name.too_short")]
    public void Create_WithMissingOrShortName_IsRejected(string? name, string expectedCode)
    {
        var exception = Assert.Throws<DomainException>(
            () => Company.Create(name, "https://contoso.com"));

        Assert.Equal(expectedCode, exception.Error.Code);
    }

    [Fact]
    public void Create_WithANameAtTheMinimumLength_IsAccepted()
    {
        var company = Company.Create("Abc", "https://abc.com");

        Assert.Equal(3, Company.NameMinLength);
        Assert.Equal("Abc", company.Name);
    }

    [Fact]
    public void Create_WithNameOverTheMaximumLength_IsRejected()
    {
        var name = new string('a', Company.NameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(
            () => Company.Create(name, $"https://{name}.com"));

        Assert.Equal("company.name.too_long", exception.Error.Code);
    }

    [Theory]
    [InlineData(null, "company.website_url.required")]
    [InlineData("", "company.website_url.required")]
    [InlineData("   ", "company.website_url.required")]
    [InlineData("/companies/contoso", "company.website_url.invalid")]
    [InlineData("contoso.com", "company.website_url.invalid")]
    [InlineData("ftp://contoso.com", "company.website_url.invalid")]
    [InlineData("file:///c:/contoso", "company.website_url.invalid")]
    [InlineData("https://localhost", "company.website_url.invalid")]
    public void Create_WithRelativeOrNonWebUrl_IsRejected(string? websiteUrl, string expectedCode)
    {
        var exception = Assert.Throws<DomainException>(
            () => Company.Create("Contoso", websiteUrl));

        Assert.Equal(expectedCode, exception.Error.Code);
    }

    [Fact]
    public void Create_DoesNotExposePublicMutators()
    {
        var mutable = typeof(Company)
            .GetProperties()
            .Where(property => property.SetMethod is not null && property.SetMethod.IsPublic)
            .Select(property => property.Name);

        Assert.Empty(mutable);
        Assert.Empty(typeof(Company).GetConstructors());
    }
}
