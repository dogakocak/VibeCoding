using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class ImportCreateRequestValidator : AbstractValidator<ImportCreateRequest>
{
    public ImportCreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x).Must(HasManifest)
            .WithMessage("Provide either a sourceBlobName or at least one inline scenario");
        RuleForEach(x => x.Scenarios).SetValidator(new ImportScenarioRequestValidator());
    }

    private static bool HasManifest(ImportCreateRequest request)
        => !string.IsNullOrWhiteSpace(request.SourceBlobName) || request.Scenarios.Any();
}