using DMIX.API.Azure.Models;
using DMIX.API.Handlers;
using DMIX.API.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalKingdom.API.DependencyInjection;

internal static class IServiceEntityHandlerExtensions
{
    public static IServiceCollection AddEntityHandlers(
        this IServiceCollection services,
        bool replaceExistingImplementations = false
    )
    {
        // TODO: Map all generic entity handlers here
        return services.AddEntityHandlers<Account, Guid>(replaceExistingImplementations);
    }

    public static IServiceCollection AddEntityHandlers<TModel, TKey>(
        this IServiceCollection services,
        bool replaceExistingImplementations = false
    )
        where TModel : EntityBase<TKey>
    {
        var modelType = typeof(TModel);
        var keyType = typeof(TKey);

        foreach (var definition in EntityHandlerDefinitions)
        {
            var serviceType = definition.BuildServiceType(modelType, keyType);

            var duplicate = services.FirstOrDefault(d => d.ServiceType == serviceType);
            if (duplicate != null && !replaceExistingImplementations)
            {
                continue;
            }

            if (duplicate != null)
            {
                services.Remove(duplicate);
            }

            var implementationType = definition.BuildImplementationType(modelType, keyType);
            var descriptor = new ServiceDescriptor(
                serviceType,
                implementationType,
                ServiceLifetime.Scoped
            );
            services.Add(descriptor);
        }

        return services;
    }

    private class EntityHandlerDefinition(
        Type handlerType,
        Type requestType,
        Type? responseType = null
    )
    {
        private static readonly Type GenericHandlerType = typeof(IRequestHandler<,>);
        private readonly Type handlerType = handlerType;
        private readonly Type requestType = requestType;
        private readonly Type? responseType = responseType;

        public Type BuildServiceType(Type modelType, Type keyType)
        {
            // Use the model type as the response type if not provided
            if (responseType == null)
            {
                return GenericHandlerType.MakeGenericType(
                    requestType.MakeGenericType(modelType, keyType),
                    modelType
                );
            }

            if (!responseType.IsGenericTypeDefinition)
            {
                // Use the actual response type if it is not a generic type definition
                return GenericHandlerType.MakeGenericType(
                    requestType.MakeGenericType(modelType, keyType),
                    responseType
                );
            }

            // Otherwise assume a single generic argument
            return GenericHandlerType.MakeGenericType(
                requestType.MakeGenericType(modelType, keyType),
                responseType.MakeGenericType(modelType)
            );
        }

        public Type BuildImplementationType(Type modelType, Type keyType)
        {
            return handlerType.MakeGenericType(modelType, keyType);
        }
    }

    private static readonly EntityHandlerDefinition[] EntityHandlerDefinitions =
        [
            new(
                typeof(AddEntityHandler<,>),
                typeof(AddEntityRequest<,>),
                typeof(CommandResponse<>)
            ),
            new(
                typeof(GetEntitiesHandler<,>),
                typeof(GetEntitiesRequest<,>),
                typeof(IList<>)
            ),
            new(typeof(GetEntityHandler<,>), typeof(GetEntityRequest<,>)),
            new(
                typeof(QueryEntitiesHandler<,>),
                typeof(QueryEntitiesRequest<,>),
                typeof(QueryEntitiesResponse<>)
            ),
            new(
                typeof(RemoveEntityHandler<,>),
                typeof(RemoveEntityRequest<,>),
                typeof(CommandResponse<>)
            ),
            new(
                typeof(UpdateEntityHandler<,>),
                typeof(UpdateEntityRequest<,>),
                typeof(CommandResponse<>)
            ),
        ];
}
