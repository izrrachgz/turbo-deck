using FluentValidation;

namespace Application.Modules.Files.Commands;

internal sealed class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator() => RuleFor(command => command.Id).GreaterThan(0);
}
