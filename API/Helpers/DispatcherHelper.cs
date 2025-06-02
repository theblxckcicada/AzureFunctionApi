using DMIX.API.Handlers;
using DMIX.API.Models;
using MediatR;
using Microsoft.Azure.Functions.Worker.Http;
using System.Reflection;

namespace DMIX.API.Helpers
{
    public static class DispatcherHelper
    {
        public static async Task<object> QueryEntitiesAsync<TModel, TKey>(
            IMediator mediator, EntityQuery query, CancellationToken cancellationToken) 
            where TModel : EntityBase<TKey>
        {
            return await mediator.Send(new QueryEntitiesRequest<TModel, TKey>
            {
                Query = query
            }, cancellationToken);
        }

        public static async Task<HttpResponseData> InvokeEntityHandlerAsync<TModel, TKey>(
            object instance, HttpRequestData req, string method, string id, CancellationToken cancellationToken)
            where TModel : EntityBase<TKey>
        {
            // Assumes HandleEntityRequest is an instance method on `instance` (your class)
            var typedMethod = instance
                .GetType()
                .GetMethod("HandleEntityRequest", BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(typeof(TModel), typeof(TKey));

            var task = (Task<HttpResponseData>)typedMethod.Invoke(instance, [req, method, id, cancellationToken])!;
            return await task;
        }


    }

}
