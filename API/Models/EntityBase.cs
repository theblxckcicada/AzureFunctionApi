namespace AnimalKingdom.API.Models;

public interface IEntityBase { }

public interface IEntityBase<TKey> : IEntityBase
{
    public TKey Id { get; set; }
}
