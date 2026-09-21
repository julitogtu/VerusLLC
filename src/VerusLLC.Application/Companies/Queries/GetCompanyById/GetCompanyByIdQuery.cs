using MediatR;
using VerusLLC.Application.Common.Behaviours;
using VerusLLC.Application.Common.Results;

namespace VerusLLC.Application.Companies.Queries.GetCompanyById;

public sealed record GetCompanyByIdQuery(Guid CompanyId)
    : IRequest<Result<CompanyDto>>, IRetryableRequest;
