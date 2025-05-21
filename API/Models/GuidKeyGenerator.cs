namespace AnimalKingdom.API.Models;

public class GuidKeyGenerator : IEntityBaseKeyGenerator<Guid>
{
    public TModel Generate<TModel>(TModel model)
        where TModel : IEntityBase<Guid>
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        model.Id = Guid.NewGuid();
        return model;
    }
}
