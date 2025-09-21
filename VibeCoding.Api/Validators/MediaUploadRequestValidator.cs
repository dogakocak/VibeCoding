using FluentValidation;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Validators;

public class MediaUploadRequestValidator : AbstractValidator<MediaUploadRequest>
{
    public MediaUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
    }
}