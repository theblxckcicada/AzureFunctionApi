using DMIX.API.Common.Models;
using DMIX.API.Models;
using MediatR;

namespace DMIX.API.Handlers;

public record GetEntityRequest<TModel, TKey> : IRequest<TModel?>
    where TModel : EntityBase<TKey>
{
    public TKey RowKey { get; init; } = default!;
}

public class GetEntityHandler<TModel, TKey>(StorageEntityHandler<TModel, TKey> storageEntityHandler, AppHeader appHeader)
    : IRequestHandler<GetEntityRequest<TModel, TKey>, TModel?>
    where TModel : EntityBase<TKey>
    where TKey : IEquatable<TKey>
{


    public async Task<TModel?> Handle(
        GetEntityRequest<TModel, TKey> request,
        CancellationToken cancellationToken
    )
    {
        var entities = await storageEntityHandler.GetEntityAsync(appHeader, cancellationToken: cancellationToken);
        return entities.FirstOrDefault();
    }
}
