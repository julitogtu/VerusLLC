using System.Collections.Concurrent;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Domain.Companies;

namespace VerusLLC.Persistence.Companies;

internal sealed class InMemoryCompanyRepository : ICompanyRepository
{
    private readonly ConcurrentDictionary<Guid, Company> companies = new();

    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);
        cancellationToken.ThrowIfCancellationRequested();

        companies[company.Id] = company;

        return Task.CompletedTask;
    }

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        companies.TryGetValue(id, out var company);

        return Task.FromResult<Company?>(company);
    }

    public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyList<Company>>(companies.Values.ToArray());
    }
}
