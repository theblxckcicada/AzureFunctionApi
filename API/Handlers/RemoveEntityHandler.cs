using DMIX.API.Common.Models;
using DMIX.API.Models;
using FluentValidation.Results;
using MediatR;

namespace DMIX.API.Handlers;

public record RemoveEntityRequest<TModel, TKey> : IRequest<CommandResponse<TModel>>
    where TModel : EntityBase<TKey>
{
    public TKey RowKey { get; init; } = default!;
}

public class RemoveEntityHandler<TModel, TKey>(
    StorageEntityHandler<TModel, TKey> storageEntityHandler, EntityHandler<TModel, TKey> entityHandler, AppHeader appHeader
) : IRequestHandler<RemoveEntityRequest<TModel, TKey>, CommandResponse<TModel>>
    where TModel : EntityBase<TKey>
    where TKey : IEquatable<TKey>
{

    public async Task<CommandResponse<TModel>> Handle(
        RemoveEntityRequest<TModel, TKey> request,
        CancellationToken cancellationToken
    )
    {
        var entity = (await storageEntityHandler.GetEntityAsync(appHeader,
               entityHandler.GetEntityQuery(
                   entityHandler.GetRowKeyFilters(request.RowKey.ToString())),
               cancellationToken)).FirstOrDefault();

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

        var removed = await storageEntityHandler.DeleteEntityAsync(entity, cancellationToken);
        return new CommandResponse<TModel> { Entities = [removed] };
    }

    protected virtual Task<ValidationResult> ValidateEntityAsync(
        TModel entity,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult(new ValidationResult());
    }
}
