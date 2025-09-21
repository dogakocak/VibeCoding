using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class ImportScenarioRequestValidator : AbstractValidator<ImportScenarioRequest>
{
    public ImportScenarioRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.MediaBlobName).NotEmpty().MaximumLength(256);
        RuleForEach(x => x.Tags).MaximumLength(64);
    }
}