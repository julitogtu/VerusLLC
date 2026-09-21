using MediatR;
using Microsoft.Extensions.Logging;
using VerusLLC.Application.Common.Behaviours;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Application.Common.Results;

namespace VerusLLC.Application.Companies.Queries.SearchCompanies;

public sealed record SearchCompaniesQuery(
    string? Name = null,
    string? Domain = null,
    string? Search = null) : IRequest<Result<IReadOnlyList<CompanyDto>>>, IRetryableRequest;

internal sealed class SearchCompaniesQueryHandler(
    ICompanyRepository companies,
    ILogger<SearchCompaniesQueryHandler> logger)
    : IRequestHandler<SearchCompaniesQuery, Result<IReadOnlyList<CompanyDto>>>
{
    public async Task<Result<IReadOnlyList<CompanyDto>>> Handle(
        SearchCompaniesQuery query,
        CancellationToken cancellationToken)
    {
        var snapshot = await companies.GetAllAsync(cancellationToken);

        var matches = CompanySearchScoring.Rank(snapshot, query.Name, query.Domain, query.Search);

        logger.LogInformation(
            "VerusLLC Request: SearchCompanies matched {MatchCount} of {CandidateCount}.",
            matches.Count,
            snapshot.Count);

        return Result<IReadOnlyList<CompanyDto>>.Success(matches);
    }
}
