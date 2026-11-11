using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Deck.Queries;

internal sealed class GetDeckGridQueryValidator : AbstractValidator<GetDeckGridQuery>
{
    public GetDeckGridQueryValidator(ValidationTextProvider messages)
    {
        RuleFor(query => query.PageId)
            .GreaterThan(0)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Greater_Than), messages.Get(nameof(FieldNames.Id)), 0));
    }
}
