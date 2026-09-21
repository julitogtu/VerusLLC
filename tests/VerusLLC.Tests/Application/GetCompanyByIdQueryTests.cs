using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VerusLLC.Application.Common.Results;
using VerusLLC.Application.Companies.Commands.CreateCompany;
using VerusLLC.Application.Companies.Queries.GetCompanyById;

namespace VerusLLC.Tests.Application;

public class GetCompanyByIdQueryTests
{
    [Fact]
    public async Task KnownId_ReturnsTheCompanyAsADto()
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();
        var created = await sender.Send(new CreateCompanyCommand("Contoso", "https://contoso.com"));

        var result = await sender.Send(new GetCompanyByIdQuery(created.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value, result.Value.Id);
        Assert.Equal("Contoso", result.Value.Name);
        Assert.Equal("https://contoso.com", result.Value.WebsiteUrl);
    }

    [Fact]
    public async Task UnknownButWellFormedId_IsNotFound()
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();

        var result = await sender.Send(new GetCompanyByIdQuery(Guid.CreateVersion7()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("company.not_found", result.Error.Code);
    }

    [Fact]
    public async Task EmptyId_IsInvalidInputRatherThanNotFound()
    {
        using var host = TestHost.Create();
        var sender = host.GetRequiredService<ISender>();

        var result = await sender.Send(new GetCompanyByIdQuery(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("company.id.required", result.Error.Code);
    }
}
