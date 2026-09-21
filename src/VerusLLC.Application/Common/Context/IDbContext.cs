namespace VerusLLC.Application.Common.Context;

public interface IDbContext
{
    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
