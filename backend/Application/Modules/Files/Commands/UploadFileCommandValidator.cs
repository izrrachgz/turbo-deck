using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Files.Commands;

internal sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator(ValidationTextProvider messages)
    {
        RuleFor(command => command.FileName)
            .NotEmpty()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get(nameof(FieldNames.File_Name))))
            .MaximumLength(255)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get(nameof(FieldNames.File_Name)), 255));
        RuleFor(command => command.ContentType)
            .NotEmpty()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get(nameof(FieldNames.Content_Type))))
            .MaximumLength(255)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get(nameof(FieldNames.Content_Type)), 255));
        RuleFor(command => command.Content)
            .NotNull()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get(nameof(FieldNames.File_Content))))
            .Must(stream => stream is not null && stream.CanRead)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Be_Read), messages.Get(nameof(FieldNames.File_Content))));
        RuleFor(command => command.MaximumSizeBytes)
            .GreaterThan(0)
            .WithMessage(messages.Get(
                nameof(ValidationText.Field_Must_Be_Greater_Than),
                messages.Get(nameof(FieldNames.Maximum_File_Size)),
                0));
        RuleFor(command => command.Purpose)
            .MaximumLength(100)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get(nameof(FieldNames.Purpose)), 100));
    }
}
