using FluentValidation;

namespace Application.Modules.Files.Commands;

internal sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(255);
        RuleFor(command => command.ContentType).NotEmpty().MaximumLength(255);
        RuleFor(command => command.Content).NotNull().Must(stream => stream.CanRead);
        RuleFor(command => command.MaximumSizeBytes).GreaterThan(0);
        RuleFor(command => command.Purpose).MaximumLength(100);
    }
}
