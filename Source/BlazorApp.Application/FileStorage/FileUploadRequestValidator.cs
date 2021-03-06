using BlazorApp.Application.Common.Validation;
using BlazorApp.Shared.FileStorage;
using FluentValidation;

namespace BlazorApp.Application.FileStorage;

public class FileUploadRequestValidator : CustomValidator<FileUploadRequest>
{
    public FileUploadRequestValidator()
    {
        RuleFor(p => p.Name).MaximumLength(150).NotEmpty().WithMessage("Image Name cannot be empty!");
        RuleFor(p => p.Extension).MaximumLength(5).NotEmpty().WithMessage("Image Extension cannot be empty!");
        RuleFor(p => p.Data).NotEmpty().WithMessage("Image Data cannot be empty!");
    }
}