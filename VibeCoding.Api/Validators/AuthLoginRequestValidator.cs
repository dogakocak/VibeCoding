using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class AuthLoginRequestValidator : AbstractValidator<AuthLoginRequest>
{
    public AuthLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}