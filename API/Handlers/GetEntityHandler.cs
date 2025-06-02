using DMIX.API.Models;
using MediatR;

namespace DMIX.API.Handlers;

public record GetEntityRequest<TModel, TKey> : IRequest<TModel?>
    where TModel : EntityBase<TKey>
{
    public TKey RowKey { get; init; } = default!;
}

public class GetEntityHandler<TModel, TKey>(StorageEntityHandler<TModel, TKey> storageEntityHandler, EntityHandler<TModel, TKey> entityHandler)
    : IRequestHandler<GetEntityRequest<TModel, TKey>, TModel?>
    where TModel : EntityBase<TKey>
    where TKey : IEquatable<TKey>
{


    public async Task<TModel?> Handle(
        GetEntityRequest<TModel, TKey> request,
        CancellationToken cancellationToken
    )
    {
        var entities = await storageEntityHandler.GetEntityAsync(
            entityHandler.GetEntityQuery(entityHandler.GetRowKeyFilters(request.RowKey.ToString())),
            cancellationToken: cancellationToken);
        return entities?.FirstOrDefault();
    }
}
