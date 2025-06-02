using DMIX.API.Models;
using MediatR;

namespace DMIX.API.Handlers;

public record GetEntitiesRequest<TModel, TKey> : IRequest<IList<TModel>>
    where TModel : EntityBase<TKey>
{
}

public class GetEntitiesHandler<TModel, TKey>(StorageEntityHandler<TModel, TKey> storageEntityHandler)
    : IRequestHandler<GetEntitiesRequest<TModel, TKey>, IList<TModel>>
    where TModel : EntityBase<TKey>
{

    public async Task<IList<TModel>> Handle(
        GetEntitiesRequest<TModel, TKey> request,
        CancellationToken cancellationToken
    )
    {
        var entities = await storageEntityHandler.GetEntityAsync(cancellationToken: cancellationToken);
        return [.. entities];
    }
}
