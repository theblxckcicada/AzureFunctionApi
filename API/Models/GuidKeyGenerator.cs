namespace DMIX.API.Models;

public class GuidKeyGenerator : IEntityBaseKeyGenerator<Guid>
{
    public TModel Generate<TModel>(TModel model)
        where TModel : EntityBase<Guid>
    {
        ArgumentNullException.ThrowIfNull(model);

        model.RowKey = Guid.NewGuid().ToString();
        return model;
    }

}
