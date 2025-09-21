using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class MediaRegisterRequestValidator : AbstractValidator<MediaRegisterRequest>
{
    public MediaRegisterRequestValidator()
    {
        RuleFor(x => x.BlobName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0);
        RuleFor(x => x.Sha256Hash).NotEmpty().Length(64 * 2)
            .Matches("^[A-Fa-f0-9]+$").WithMessage("Hash must be hex encoded");
    }
}