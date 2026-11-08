using FluentValidation;

namespace Application.Modules.Files.Queries;

internal sealed class GetFilesQueryValidator : AbstractValidator<GetFilesQuery>
{
    public GetFilesQueryValidator() => RuleFor(query => query.Limit).InclusiveBetween(1, 500);
}
