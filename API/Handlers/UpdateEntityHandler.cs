using DMIX.API.Models;
using MediatR;

namespace DMIX.API.Handlers;

public record UpdateEntityRequest<TModel, TKey> : IRequest<CommandResponse<TModel>>
    where TModel : EntityBase<TKey>
{
    public TKey RowKey { get; init; } = default!;
    public TModel Entity { get; init; } = default!;
}

public class UpdateEntityHandler<TModel, TKey>(
        StorageEntityHandler<TModel, TKey> storageEntityHandler, EntityHandler<TModel, TKey> entityHandler

) : IRequestHandler<UpdateEntityRequest<TModel, TKey>, CommandResponse<TModel>>
    where TModel : EntityBase<TKey>
    where TKey : IEquatable<TKey>
{
    public async Task<CommandResponse<TModel>> Handle(
        UpdateEntityRequest<TModel, TKey> request,
        CancellationToken cancellationToken
    )
    {
        var entity = request.Entity;
        if (!entity.RowKey.Equals(request.RowKey.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            entity = (await storageEntityHandler.GetEntityAsync(
                entityHandler.GetEntityQuery(
                    entityHandler.GetRowKeyFilters(request.RowKey.ToString())),
                cancellationToken)).FirstOrDefault();
        }
        if (entity == null)
        {
            return new CommandResponse<TModel>();
        }

        var validationResult = entityHandler.ValidateEntity(entity);

        if (!string.IsNullOrEmpty(validationResult))
        {
            return new CommandResponse<TModel>
            {
                Entities = [entity],
                ValidationResult = validationResult,
            };
        }

        var updated = await storageEntityHandler.UpdateEntityAsync(entity, cancellationToken);

        return new CommandResponse<TModel> { Entities = [updated] };
    }



}
