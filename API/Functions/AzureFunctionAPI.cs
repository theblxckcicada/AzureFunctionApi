using DMIX.API.Azure.Models;
using DMIX.API.Common.Models;
using DMIX.API.Handlers;
using DMIX.API.Http;
using DMIX.API.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using AuthorizationLevel = Microsoft.Azure.Functions.Worker.AuthorizationLevel;

namespace DMIX.API.Functions;

public class AzureFunctionAPI(ILogger<AzureFunctionAPI> logger, IMediator mediator, AppHeader appHeader)
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
        // request validation 
        appHeader.Claims = // Continue from here 

        var pathSegments = path?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var method = req.Method.ToUpperInvariant();
        var cancellationToken = executionContext.CancellationToken;

        if (pathSegments.Length == 0)
            return await req.CreateBadRequestResponseAsync("Invalid Request", HttpStatusCode.NotFound);

        var entityType = pathSegments[0];
        var id = pathSegments.Length > 1 ? pathSegments[1] : null;

        switch (entityType.ToLower())
        {
            case "account":

                if (id?.ToLower() == "query" && method == nameof(HttpTriggerMethod.POST))
                {
                    var query = await req.ReadFromJsonAsync<EntityQuery>(cancellationToken: cancellationToken);

                    var result = await _mediator.Send(new QueryEntitiesRequest<Account, Guid>
                    {
                        Query = query
                    }, cancellationToken);
                    return await req.CreateOkResponseAsync(result);
                }
                return await HandleEntityRequest<Account, Guid>(req, method, id, cancellationToken);

            default:
                return await req.CreateBadRequestResponseAsync("Invalid Request", HttpStatusCode.NotFound);
        }
    }
    private async Task<HttpResponseData> HandleEntityRequest<TModel, TKey>(
        HttpRequestData req,
        string method,
        string id,
        CancellationToken cancellationToken
    ) where TModel : EntityBase<TKey>
    {
        switch (method)
        {
            case nameof(HttpTriggerMethod.GET):
                if (string.IsNullOrEmpty(id))
                {
                    var entities = await _mediator.Send(new GetEntitiesRequest<TModel, TKey>(), cancellationToken);
                    return await req.CreateOkResponseAsync(entities, cancellationToken: cancellationToken);
                }
                else
                {
                    var entity = await _mediator.Send(new GetEntityRequest<TModel, TKey> { RowKey = EntityKey.ParseId<TKey>(id) }, cancellationToken);
                    return entity is not null
                        ? await req.CreateOkResponseAsync(entity, cancellationToken: cancellationToken)
                        : await req.CreateBadRequestResponseAsync("Invalid Request", HttpStatusCode.NotFound, cancellationToken: cancellationToken);
                }

            case nameof(HttpTriggerMethod.POST):
                var newEntity = await req.ReadFromJsonAsync<TModel>(cancellationToken: cancellationToken);
                var addResult = await _mediator.Send(new AddEntityRequest<TModel, TKey> { Entity = newEntity }, cancellationToken);
                return await req.CreateOkResponseAsync(addResult, cancellationToken: cancellationToken);

            case nameof(HttpTriggerMethod.PUT):
                var updatedEntity = await req.ReadFromJsonAsync<TModel>(cancellationToken: cancellationToken);
                var updateResult = await _mediator.Send(new UpdateEntityRequest<TModel, TKey>
                {
                    RowKey = EntityKey.ParseId<TKey>(id),
                    Entity = updatedEntity
                }, cancellationToken);
                return await req.CreateOkResponseAsync(updateResult, cancellationToken: cancellationToken);

            case nameof(HttpTriggerMethod.DELETE):
                var deleteResult = await _mediator.Send(new RemoveEntityRequest<TModel, TKey> { RowKey = EntityKey.ParseId<TKey>(id) }, cancellationToken);
                return deleteResult.Entities is not null
                    ? await req.CreateOkResponseAsync(deleteResult, cancellationToken: cancellationToken)
                    : await req.CreateBadRequestResponseAsync("", HttpStatusCode.NotFound, cancellationToken: cancellationToken);


            default:
                return await req.CreateBadRequestResponseAsync("", HttpStatusCode.MethodNotAllowed, cancellationToken: cancellationToken);
        }
    }


}