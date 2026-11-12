using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Media.Commands;

internal sealed class UploadCellThumbnailCommandValidator : AbstractValidator<UploadCellThumbnailCommand>
{
    public UploadCellThumbnailCommandValidator(ValidationTextProvider messages)
    {
        RuleFor(command => command.FileName)
            .NotEmpty()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get(nameof(FieldNames.File_Name))))
            .MaximumLength(255)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get(nameof(FieldNames.File_Name)), 255));
        RuleFor(command => command.Content)
            .NotNull()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get(nameof(FieldNames.File_Content))))
            .Must(stream => stream is not null && stream.CanRead)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Be_Read), messages.Get(nameof(FieldNames.File_Content))));
        RuleFor(command => command.DisplayName)
            .MaximumLength(160)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get(nameof(FieldNames.Display_Name)), 160));
    }
}
