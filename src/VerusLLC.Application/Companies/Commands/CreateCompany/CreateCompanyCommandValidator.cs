using FluentValidation;

namespace VerusLLC.Application.Companies.Commands.CreateCompany;

internal sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(100).WithMessage("Company name must not exceed 100 characters.");
        RuleFor(x => x.WebsiteUrl)
            .NotEmpty().WithMessage("Website URL is required.")
            .Must(BeAValidUrl).WithMessage("Website URL must be a valid URL.");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
