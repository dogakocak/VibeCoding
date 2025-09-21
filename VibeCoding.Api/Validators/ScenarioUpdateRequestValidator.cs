using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class ScenarioUpdateRequestValidator : AbstractValidator<ScenarioUpdateRequest>
{
    public ScenarioUpdateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.MediaAssetId).NotEmpty();
        RuleForEach(x => x.Tags).MaximumLength(64);
    }
}