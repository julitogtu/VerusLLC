using MediatR;
using Microsoft.Extensions.Logging;
using VerusLLC.Application.Common.Persistence;
using VerusLLC.Application.Common.Results;
using VerusLLC.Domain.Common;
using VerusLLC.Domain.Companies;

namespace VerusLLC.Application.Companies.Commands.CreateCompany;

internal sealed class CreateCompanyCommandHandler(
    ICompanyRepository companies,
    ILogger<CreateCompanyCommandHandler> logger)
    : IRequestHandler<CreateCompanyCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        Company company;

        try
        {
            company = Company.Create(request.Name, request.WebsiteUrl);
        }
        catch (DomainException exception)
        {
            logger.LogInformation(
                "VerusLLC Request: CreateCompany rejected by {ErrorCode}.",
                exception.Error.Code);

            return Result<Guid>.Failure(
                Error.Validation(exception.Error.Code, exception.Error.Message));
        }

        await companies.AddAsync(company, cancellationToken);

        logger.LogInformation(
            "VerusLLC Request: CreateCompany stored {CompanyId}.",
            company.Id);

        return Result<Guid>.Success(company.Id);
    }
}
