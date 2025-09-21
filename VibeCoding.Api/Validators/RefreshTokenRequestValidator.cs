using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}