using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class TelemetryEventRequestValidator : AbstractValidator<TelemetryEventRequest>
{
    public TelemetryEventRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PayloadJson).NotEmpty().MaximumLength(2048);
    }
}