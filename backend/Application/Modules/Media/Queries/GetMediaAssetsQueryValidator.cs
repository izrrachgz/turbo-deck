using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Media.Queries;

internal sealed class GetMediaAssetsQueryValidator : AbstractValidator<GetMediaAssetsQuery>
{
    public GetMediaAssetsQueryValidator(ValidationTextProvider messages)
    {
        RuleFor(query => query.Limit)
            .InclusiveBetween(1, 500)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Between), messages.Get(nameof(FieldNames.Limit)), 1, 500));
    }
}
