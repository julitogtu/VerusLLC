using MediatR;
using VerusLLC.Application.Common.Results;

namespace VerusLLC.Application.Companies.Commands.CreateCompany;

public sealed record CreateCompanyCommand(
    string Name,
    string WebsiteUrl) : IRequest<Result<Guid>>;
