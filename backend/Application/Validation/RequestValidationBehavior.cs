using Cqrs.Abstractions;
using FluentValidation;
using FluentValidation.Results;

namespace Application.Validation;

internal sealed class RequestValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();

        foreach (IValidator<TRequest> validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors.Where(failure => failure is not null));
        }

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next(request, cancellationToken);
    }
}
