using VerusLLC.Domain.Companies;

namespace VerusLLC.Application.Common.Persistence;

public interface ICompanyRepository
{
    Task AddAsync(Company company, CancellationToken cancellationToken = default);

    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default);
}
