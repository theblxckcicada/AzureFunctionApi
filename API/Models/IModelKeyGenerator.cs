﻿namespace DMIX.API.Models;

public interface IEntityBaseKeyGenerator<TKey>
{
    TModel Generate<TModel>(TModel model)
        where TModel : EntityBase<TKey>;
}

