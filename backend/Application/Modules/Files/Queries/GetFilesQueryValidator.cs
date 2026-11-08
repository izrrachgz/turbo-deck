using FluentValidation;
using Localization.Providers;
using Application.Localization.Resources;

namespace Application.Modules.Files.Queries;

internal sealed class GetFilesQueryValidator : AbstractValidator<GetFilesQuery>
{
    public GetFilesQueryValidator(ValidationTextProvider messages)
    {
        RuleFor(query => query.Limit)
            .InclusiveBetween(1, 500)
            .WithMessage(messages.Get(nameof(ValidationText.Field_Must_Be_Between), messages.Get(nameof(FieldNames.Limit)), 1, 500));
    }
}
