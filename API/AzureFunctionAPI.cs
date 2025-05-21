using EasySMS.API.Common.Models;
using EasySMS.API.Http;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using AuthorizationLevel = Microsoft.Azure.Functions.Worker.AuthorizationLevel;

namespace EasySMS.API;

public class AzureFunctionAPI(ILogger<AzureFunctionAPI> logger, IMediator mediator)
{
    private readonly ILogger<AzureFunctionAPI> _logger = logger;
    private readonly IMediator _mediator = mediator;

    [Function("AzureFunctionAPI")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function,
                nameof(HttpTriggerMethod.GET),
                nameof(HttpTriggerMethod.POST),
                nameof(HttpTriggerMethod.PUT),
                nameof(HttpTriggerMethod.DELETE), Route= "{*path}")]  HttpRequestData req, string path, FunctionContext executionContext)
    {
        var pathSegments = path?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var method = req.Method.ToUpperInvariant();
        var cancellationToken = executionContext.CancellationToken;

        if (pathSegments.Length == 0)
            return await req.CreateResponseAsync(HttpStatusCode.NotFound, "");

        var entityType = pathSegments[0];
        var id = pathSegments.Length > 1 ? pathSegments[1] : null;

        switch (entityType.ToLower())
        {
            case "animal":
                
                if (id?.ToLower() == "query" && method == "POST")
                {
                    var body = await req.ReadFromJsonAsync<EntityQuery>(cancellationToken: cancellationToken);
                    var result = await _mediator.Send(new QueryEntitiesRequest<BookingModel, Guid> { Query = body }, cancellationToken);
                    return await req.CreateResponse(HttpStatusCode.OK, result);
                }
                return await HandleEntityRequest<BookingModel, Guid>(req, method, id, cancellationToken);

            default:
                return await req.CreateResponse(HttpStatusCode.NotFound);
        }
    }
    private async Task<HttpResponseData> HandleEntityRequest<TModel, TKey>(
        HttpRequestData req,
        string method,
        string id,
        CancellationToken cancellationToken
    ) where TModel : class, IEntityBase<TKey>
    {
        switch (method)
        {
            case "GET":
                if (string.IsNullOrEmpty(id))
                {
                    var entities = await _mediator.Send(new GetEntitiesRequest<TModel, TKey>(), cancellationToken);
                    return await req.CreateOkResponseAsync(HttpStatusCode.OK, entities);
                }
                else
                {
                    var entity = await _mediator.Send(new GetEntityRequest<TModel, TKey> { Id = ParseId<TKey>(id) }, cancellationToken);
                    return entity is not null
                        ? await req.CreateResponse(HttpStatusCode.OK, entity)
                        : await req.CreateResponse(HttpStatusCode.NotFound);
                }

            case "POST":
                var newEntity = await req.ReadFromJsonAsync<TModel>(cancellationToken: cancellationToken);
                var addResult = await _mediator.Send(new AddEntityRequest<TModel, TKey> { Entity = newEntity }, cancellationToken);
                return await req.CreateResponse(HttpStatusCode.Created, addResult.Entity);

            case "PUT":
                var updatedEntity = await req.ReadFromJsonAsync<TModel>(cancellationToken: cancellationToken);
                var updateResult = await _mediator.Send(new UpdateEntityRequest<TModel, TKey>
                {
                    Id = ParseId<TKey>(id),
                    Entity = updatedEntity
                }, cancellationToken);
                return await req.CreateResponse(HttpStatusCode.OK, updateResult.Entity);

            case "DELETE":
                var deleteResult = await _mediator.Send(new RemoveEntityRequest<TModel, TKey> { Id = ParseId<TKey>(id) }, cancellationToken);
                return deleteResult.Entity is not null
                    ? await req.CreateResponse(HttpStatusCode.OK, deleteResult.Entity)
                    : await req.CreateResponse(HttpStatusCode.NotFound);

            default:
                return await req.CreateResponse(HttpStatusCode.MethodNotAllowed);
        }
    }

    private static TKey ParseId<TKey>(string id)
    {
        return (TKey)Convert.ChangeType(id, typeof(TKey));
    }
}