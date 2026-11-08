using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Files.Commands;

internal sealed class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator(ValidationTextProvider messages)
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Greater_Than), messages.Get(nameof(FieldNames.Id)), 0));
    }
}
