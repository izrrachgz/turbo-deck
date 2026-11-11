using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Deck.Commands;

internal sealed class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileCommandValidator(ValidationTextProvider messages)
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage(messages.Get(nameof(ValidationText.Field_Is_Required), messages.Get(nameof(FieldNames.Name))))
            .MaximumLength(100)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Cannot_Exceed), messages.Get(nameof(FieldNames.Name)), 100));
    }
}
