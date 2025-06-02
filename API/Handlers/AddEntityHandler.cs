using DMIX.API.Models;
using MediatR;

namespace DMIX.API.Handlers;

public record AddEntityRequest<TModel, TKey> : IRequest<CommandResponse<TModel>>
    where TModel : EntityBase<TKey>
{
    public TModel Entity { get; init; } = default!;
}

public class AddEntityHandler<TModel, TKey>(
    StorageEntityHandler<TModel, TKey> storageEntityHandler, EntityHandler<TModel, TKey> entityHandler
) : IRequestHandler<AddEntityRequest<TModel, TKey>, CommandResponse<TModel>>
    where TModel : EntityBase<TKey>
{

    public async Task<CommandResponse<TModel>> Handle(
        AddEntityRequest<TModel, TKey> request,
        CancellationToken cancellationToken
    )
    {
        var entity = await entityHandler.PopulateEntityAsync(request.Entity, cancellationToken);
        var validationResult = entityHandler.ValidateEntity(entity);

        if (!string.IsNullOrEmpty(validationResult))
        {
            return new CommandResponse<TModel>
            {
                Entities = [entity],
                ValidationResult = validationResult,
            };
        }

        var added = await storageEntityHandler.AddEntityAsync(entity, cancellationToken);

        return new CommandResponse<TModel> { Entities = [added] };
    }



}
