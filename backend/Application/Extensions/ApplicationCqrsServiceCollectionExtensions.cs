using Application.Validation;
using Cqrs.Abstractions;
using Cqrs.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Application.Extensions;

internal static class ApplicationCqrsServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationRequest<
        TRequest,
        TResponse,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this IServiceCollection services)
        where TRequest : class, IRequest<TResponse>
        where THandler : class, IRequestHandler<TRequest, TResponse>
    {
        services.AddRequestHandler<TRequest, TResponse, THandler>();
        services.AddPipelineBehavior<TRequest, TResponse, RequestValidationBehavior<TRequest, TResponse>>();
        return services;
    }
}
