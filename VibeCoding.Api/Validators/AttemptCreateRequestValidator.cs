using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class AttemptCreateRequestValidator : AbstractValidator<AttemptCreateRequest>
{
    public AttemptCreateRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ConfidencePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.ResponseTimeMilliseconds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Explanation).MaximumLength(1024).When(x => !string.IsNullOrWhiteSpace(x.Explanation));
    }
}