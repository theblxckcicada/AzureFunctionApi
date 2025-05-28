using DMIX.API.Common.Models;
using DMIX.API.Models;
using MediatR;

namespace DMIX.API.Handlers;

public record QueryEntitiesRequest<TModel, TKey> : IRequest<QueryEntitiesResponse<TModel>>
    where TModel : EntityBase<TKey>
{

    public EntityQuery Query
    {
        get; set;
    }
}

public record QueryEntitiesResponse<TModel>
{
    public IList<TModel> Entities { get; init; } = [];
    public int Total { get; init; } = 0;
}

public class QueryEntitiesHandler<TModel, TKey>(StorageEntityHandler<TModel, TKey> storageEntityHandler, EntityHandler<TModel, TKey> filterHandler, AppHeader appHeader)
    : IRequestHandler<QueryEntitiesRequest<TModel, TKey>, QueryEntitiesResponse<TModel>>
    where TModel : EntityBase<TKey>
{


    public async Task<QueryEntitiesResponse<TModel>> Handle(
        QueryEntitiesRequest<TModel, TKey> request,
        CancellationToken cancellationToken
    )
    {
        if (request.Query.PageSize <= 0 || request.Query.PageIndex < 0)
        {
            return new QueryEntitiesResponse<TModel>();
        }


        var entities = await storageEntityHandler.GetEntityAsync(appHeader, request.Query, cancellationToken: cancellationToken);


        if (entities.Count <= 0)
        {
            return new QueryEntitiesResponse<TModel>();
        }


        var entityQuery = entities.Skip(request.Query.PageIndex * request.Query.PageSize);


        // Get the total entities based on the returned query
        var total = entities.Count;

        // Get entities for the requested page size
        entityQuery = entityQuery.Take(request.Query.PageSize).ToList();


        return new QueryEntitiesResponse<TModel>
        {
            Total = total,
            Entities = [.. entityQuery],
        };
    }
}
