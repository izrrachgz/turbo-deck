using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Deck.Queries;

internal sealed class GetProfilePagesQueryValidator : AbstractValidator<GetProfilePagesQuery>
{
    public GetProfilePagesQueryValidator(ValidationTextProvider messages)
    {
        RuleFor(query => query.ProfileId)
            .GreaterThan(0)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Greater_Than), messages.Get(nameof(FieldNames.Id)), 0));
    }
}
