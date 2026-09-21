using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using VerusLLC.Api.Contracts.Companies;
using VerusLLC.Application.Companies.Commands.CreateCompany;
using VerusLLC.Application.Companies.Queries.GetCompanyById;
using VerusLLC.Application.Companies.Queries.SearchCompanies;

namespace VerusLLC.Api.Controllers;

[ApiVersion("1.0")]
public class CompaniesController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCompanyCommand(request.Name!, request.WebsiteUrl!);

        var created = await Mediator.Send(command, cancellationToken);

        if (created.IsFailure)
        {
            return ToProblem(created.Error);
        }

        var stored = await Mediator.Send(new GetCompanyByIdQuery(created.Value), cancellationToken);

        if (stored.IsFailure)
        {
            return ToProblem(stored.Error);
        }

        var response = CompanyResponse.From(stored.Value);

        var location = Url.Action(nameof(GetById), new { id = response.Id })
            ?? $"/api/companies/{response.Id}";

        return Created(location, response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CompanyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] CompanySearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = new SearchCompaniesQuery(request.Name, request.Domain, request.Search);

        var matches = await Mediator.Send(query, cancellationToken);

        if (matches.IsFailure)
        {
            return ToProblem(matches.Error);
        }

        return Ok(matches.Value.Select(CompanyResponse.From).ToArray());
    }

    [HttpGet("{id}")]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var found = await Mediator.Send(new GetCompanyByIdQuery(id), cancellationToken);

        if (found.IsFailure)
        {
            return ToProblem(found.Error);
        }

        return Ok(CompanyResponse.From(found.Value));
    }
}
