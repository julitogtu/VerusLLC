using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Domain.Companies;

namespace VerusLLC.Tests.Persistence;

public class InMemoryCompanyRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheStoredCompany()
    {
        using var host = TestHost.Create();
        var repository = host.GetRequiredService<ICompanyRepository>();
        var company = Company.Create("Contoso", "https://contoso.com");

        await repository.AddAsync(company);
        var found = await repository.GetByIdAsync(company.Id);

        Assert.NotNull(found);
        Assert.Equal(company.Id, found.Id);
        Assert.Equal("Contoso", found.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        using var host = TestHost.Create();
        var repository = host.GetRequiredService<ICompanyRepository>();

        Assert.Null(await repository.GetByIdAsync(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task TheStoreIsSharedAcrossScopes()
    {
        using var host = TestHost.Create();
        var company = Company.Create("Contoso", "https://contoso.com");

        using (var writing = host.CreateScope())
        {
            await writing.ServiceProvider
                .GetRequiredService<ICompanyRepository>()
                .AddAsync(company);
        }

        using var reading = host.CreateScope();
        var found = await reading.ServiceProvider
            .GetRequiredService<ICompanyRepository>()
            .GetByIdAsync(company.Id);

        Assert.NotNull(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAProtectedSnapshot()
    {
        using var host = TestHost.Create();
        var repository = host.GetRequiredService<ICompanyRepository>();
        await repository.AddAsync(Company.Create("Contoso", "https://contoso.com"));

        var snapshot = await repository.GetAllAsync();

        Assert.IsNotType<List<Company>>(snapshot);
        Assert.True(((IList)snapshot).IsFixedSize);

        if (snapshot is IList { IsReadOnly: false } mutable)
        {
            try
            {
                mutable.Clear();
            }
            catch (NotSupportedException)
            {
            }
        }

        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsANewSnapshotEachTime()
    {
        using var host = TestHost.Create();
        var repository = host.GetRequiredService<ICompanyRepository>();
        await repository.AddAsync(Company.Create("Contoso", "https://contoso.com"));

        Assert.NotSame(await repository.GetAllAsync(), await repository.GetAllAsync());
    }

    [Fact]
    public async Task ConcurrentWrites_WithDistinctIds_KeepEveryCompany()
    {
        using var host = TestHost.Create();
        var repository = host.GetRequiredService<ICompanyRepository>();

        const int Writers = 32;
        const int PerWriter = 25;

        await Task.WhenAll(Enumerable.Range(0, Writers).Select(writer => Task.Run(async () =>
        {
            for (var index = 0; index < PerWriter; index++)
            {
                var slug = $"acme{writer}x{index}";
                await repository.AddAsync(Company.Create(slug, $"https://{slug}.com"));
            }
        })));

        var all = await repository.GetAllAsync();

        Assert.Equal(Writers * PerWriter, all.Count);
        Assert.Equal(all.Count, all.Select(company => company.Id).Distinct().Count());
    }

    [Fact]
    public async Task ANewHostStartsEmpty()
    {
        using (var first = TestHost.Create())
        {
            await first.GetRequiredService<ICompanyRepository>()
                .AddAsync(Company.Create("Contoso", "https://contoso.com"));
        }

        using var second = TestHost.Create();

        Assert.Empty(await second.GetRequiredService<ICompanyRepository>().GetAllAsync());
    }

    [Fact]
    public async Task OperationsHonourCancellation()
    {
        using var host = TestHost.Create();
        var repository = host.GetRequiredService<ICompanyRepository>();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.GetAllAsync(cancelled.Token));
    }
}
