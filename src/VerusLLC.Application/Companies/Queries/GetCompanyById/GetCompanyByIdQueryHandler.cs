using MediatR;
using Microsoft.Extensions.Logging;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Application.Common.Results;

namespace VerusLLC.Application.Companies.Queries.GetCompanyById;

internal sealed class GetCompanyByIdQueryHandler(
    ICompanyRepository companies,
    ILogger<GetCompanyByIdQueryHandler> logger)
    : IRequestHandler<GetCompanyByIdQuery, Result<CompanyDto>>
{
    private static readonly Error CompanyIdRequired =
        Error.Validation("company.id.required", "Company id is required.");

    public async Task<Result<CompanyDto>> Handle(GetCompanyByIdQuery query, CancellationToken cancellationToken)
    {
        if (query.CompanyId == Guid.Empty)
        {
            logger.LogInformation(
                "VerusLLC Request: GetCompanyById rejected by {ErrorCode}.",
                CompanyIdRequired.Code);

            return Result<CompanyDto>.Failure(CompanyIdRequired);
        }

        var company = await companies.GetByIdAsync(query.CompanyId, cancellationToken);

        if (company is null)
        {
            logger.LogInformation(
                "VerusLLC Request: GetCompanyById found no company {CompanyId}.",
                query.CompanyId);

            return Result<CompanyDto>.Failure(
                Error.NotFound("company.not_found", $"Company {query.CompanyId} was not found."));
        }

        return Result<CompanyDto>.Success(company.ToDto());
    }
}
