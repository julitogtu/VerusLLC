using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Application.Common.Results;
using VerusLLC.Application.Companies.Commands.CreateCompany;

namespace VerusLLC.Tests.Application;

public class CreateCompanyCommandTests
{
    [Fact]
    public async Task ValidCommand_PersistsExactlyOneCompanyAndReturnsItsId()
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();
        var repository = host.GetRequiredService<ICompanyRepository>();

        var result = await sender.Send(new CreateCompanyCommand("Contoso", "https://contoso.com"));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var stored = await repository.GetAllAsync();

        Assert.Single(stored);
        Assert.Equal(result.Value, stored[0].Id);
    }

    [Fact]
    public async Task ValidCommand_PersistsTheNormalizedCompany()
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();
        var repository = host.GetRequiredService<ICompanyRepository>();

        var result = await sender.Send(
            new CreateCompanyCommand("  Acme   Labs  ", "HTTPS://ACMELABS.COM/"));

        var stored = await repository.GetByIdAsync(result.Value);

        Assert.NotNull(stored);
        Assert.Equal("Acme Labs", stored.Name);
        Assert.Equal("https://acmelabs.com", stored.WebsiteUrl);
    }

    [Theory]
    [InlineData("", "https://contoso.com", "company.name.required")]
    [InlineData("C", "https://contoso.com", "company.name.too_short")]
    [InlineData("Contoso", "not-a-url", "company.website_url.invalid")]
    [InlineData("Contoso", "ftp://contoso.com", "company.website_url.invalid")]
    [InlineData("Microsoft", "https://contoso.com", "company.name_host.mismatch")]
    [InlineData("Microsoft", "https://notmicrosoft.com", "company.name_host.mismatch")]
    public async Task InvalidCommand_FailsValidationAndPersistsNothing(
        string name,
        string websiteUrl,
        string expectedCode)
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();
        var repository = host.GetRequiredService<ICompanyRepository>();

        var result = await sender.Send(new CreateCompanyCommand(name, websiteUrl));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task RepeatedFailures_LeaveAnExistingCompanyUntouched()
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();
        var repository = host.GetRequiredService<ICompanyRepository>();

        await sender.Send(new CreateCompanyCommand("Contoso", "https://contoso.com"));

        await sender.Send(new CreateCompanyCommand("Microsoft", "https://google.com"));
        await sender.Send(new CreateCompanyCommand("C", "https://c.com"));
        await sender.Send(new CreateCompanyCommand("", ""));

        Assert.Single(await repository.GetAllAsync());
    }
}
