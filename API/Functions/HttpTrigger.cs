using DMIX.API.Azure.Models;
using DMIX.API.Common;
using DMIX.API.Common.Models;
using DMIX.API.Handlers;
using DMIX.API.Helpers;
using DMIX.API.Http;
using DMIX.API.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using System.Net;
using AuthorizationLevel = Microsoft.Azure.Functions.Worker.AuthorizationLevel;

namespace DMIX.API.Functions;

public class HttpTriggerAPI(ILogger<HttpTriggerAPI> logger, IMediator mediator, IHeaderHandler headerHandler)
{
    private readonly ILogger<HttpTriggerAPI> _logger = logger;
    private readonly IMediator _mediator = mediator;
    private readonly IHeaderHandler _headerHandler = headerHandler;

    [Function("HttpTriggerAPI")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function,
                nameof(HttpTriggerMethod.GET),
                nameof(HttpTriggerMethod.POST),
                nameof(HttpTriggerMethod.PUT),
                nameof(HttpTriggerMethod.DELETE), Route= "{*path}")]  HttpRequestData req, string path, FunctionContext executionContext)
    {
        var cancellationToken = executionContext.CancellationToken;

        try
        {

            // grab the claims with every request
            _headerHandler.Claims = await _headerHandler.GetClaims(req, cancellationToken);


            var pathSegments = path?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
            var method = req.Method.ToUpperInvariant();


            if (pathSegments.Length == 0)
                return await req.CreateBadRequestResponseAsync("Invalid Request", HttpStatusCode.NotFound);

            var entityType = pathSegments[0];
            var id = pathSegments.Length > 1 ? pathSegments[1] : null;

            // Define a mapping from entityType string to (entityType, keyType)
            var entityMap = new Dictionary<string, (Type EntityType, Type KeyType)>
                {
                    { "account", (typeof(Account), typeof(Guid)) },
                    { "user",    (typeof(User), typeof(Guid)) },
                    // Add more entity types here
                };

            if (entityMap.TryGetValue(entityType.ToLower(), out var types))
            {
                var (modelType, keyType) = types;

                if (id?.ToLower() == "query" && method == nameof(HttpTriggerMethod.POST))
                {
                    var query = await req.ReadFromJsonAsync<EntityQuery>(cancellationToken: cancellationToken);
                    var queryMethod = typeof(DispatcherHelper)
                        .GetMethod(nameof(DispatcherHelper.QueryEntitiesAsync))!
                        .MakeGenericMethod(modelType, keyType);

                    var resultTask = (Task<object>)queryMethod.Invoke(null, [_mediator, query, cancellationToken])!;
                    var result = await resultTask;
                    return await req.CreateOkResponseAsync(result);
                }

                if (!string.IsNullOrEmpty(id) && id.IsValidUUID() || int.TryParse(id, out _))
                {
                    var handleMethod = typeof(DispatcherHelper)
                        .GetMethod(nameof(DispatcherHelper.InvokeEntityHandlerAsync))!
                        .MakeGenericMethod(modelType, keyType);

                    var resultTask = (Task<HttpResponseData>)handleMethod.Invoke(null, [this, req, method, id, cancellationToken])!;
                    return await resultTask;
                }
            }

            return await req.CreateBadRequestResponseAsync("Invalid entity type", HttpStatusCode.NotFound);


        }
        catch (BadHttpRequestException ex)
        {
            string errorMessage = ex.Message.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return await req.CreateBadRequestResponseAsync(
                errorMessage,
                (HttpStatusCode)ex.StatusCode,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            string errorMessage = ex.Message.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)[0].Trim();

            return await req.CreateBadRequestResponseAsync(
                errorMessage,
                HttpStatusCode.BadRequest,
                cancellationToken
            );
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
                    return await req.CreateOkResponseAsync(new CommandResponse<TModel>() { Entities = [.. entities] }, cancellationToken: cancellationToken);
                }
                else
                {
                    var entity = await _mediator.Send(new GetEntityRequest<TModel, TKey> { RowKey = EntityKey.ParseId<TKey>(id) }, cancellationToken);
                    return entity is not null
                        ? await req.CreateOkResponseAsync(new CommandResponse<TModel>() { Entities = [entity] }, cancellationToken: cancellationToken)
                        : await req.CreateBadRequestResponseAsync("Entity not found", HttpStatusCode.NotFound, cancellationToken: cancellationToken);
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