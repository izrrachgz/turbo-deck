using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Deck.Commands;

internal sealed class DeleteProfileCommandValidator : AbstractValidator<DeleteProfileCommand>
{
    public DeleteProfileCommandValidator(ValidationTextProvider messages)
    {
        RuleFor(command => command.ProfileId)
            .GreaterThan(0)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Greater_Than), messages.Get(nameof(FieldNames.Id)), 0));
        RuleFor(command => command.ConfirmationName)
            .NotEmpty()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get(nameof(FieldNames.Name))))
            .MaximumLength(100)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get(nameof(FieldNames.Name)), 100));
    }
}
