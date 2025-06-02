using DMIX.API.Azure.Services.TableStorage;
using DMIX.API.Common.Models;
using DMIX.API.Models;

namespace DMIX.API.Handlers
{
    public interface IStorageEntityHandler<TModel, TKey>
     where TModel : EntityBase<TKey>
    {
        Task<List<TModel>> GetEntityAsync(

            EntityQuery? query = null,
            CancellationToken cancellationToken = default
        );

        Task<TModel> AddEntityAsync(
            TModel entity,

            CancellationToken cancellationToken = default
        );

        Task<TModel> UpdateEntityAsync(
            TModel entity,

            CancellationToken cancellationToken = default
        );

        Task<TModel> DeleteEntityAsync(
            TModel entity,
            CancellationToken cancellationToken = default
        );
    }

    public class StorageEntityHandler<TModel, TKey>(IAzureTableStorageService<TModel, TKey> tableStorageService, EntityHandler<TModel, TKey> entityHandler) : IStorageEntityHandler<TModel, TKey>
        where TModel : EntityBase<TKey>
    {


        public async Task<List<TModel>> GetEntityAsync(
         EntityQuery? query = null,
                     CancellationToken cancellationToken = default
  )
        {
            Type type = typeof(TModel);
            string tableName = type.Name;
        

            query = entityHandler.GetEntityQuery(query: query);
            List<Filter> filters = entityHandler.CreateFilters(query: query);
            return await tableStorageService.GetAsync(
                    tableName,
                   filters, query,
                    cancellationToken
                );
        }


        public async Task<TModel> AddEntityAsync(
            TModel entity,
            CancellationToken cancellationToken = default
        )

        {
            Type type = typeof(TModel);
            string tableName = type.Name;
            var filters = entityHandler.CreateFilters();
            entity = entityHandler.UpdateModelDateTime(entity);
            return await tableStorageService.InsertAsync(
                    tableName,
                    entity,
                   filters,
                    cancellationToken
                );
        }


        public async Task<TModel> UpdateEntityAsync(
            TModel entity,
            CancellationToken cancellationToken = default
        )

        {
            Type type = typeof(TModel);
            string tableName = type.Name;
            var filters = entityHandler.EntityBaseFilters(entity);
            entity = entityHandler.UpdateModelDateTime(entity);
            return await tableStorageService.UpdateAsync(
                    tableName,
                    entity,
                   filters,
                    cancellationToken
                );
        }



        public async Task<TModel> DeleteEntityAsync(
            TModel entity,
            CancellationToken cancellationToken = default
        )

        {
            Type type = typeof(TModel);
            string tableName = type.Name;
            var filters = entityHandler.EntityBaseFilters(entity);

            return await tableStorageService.DeleteAsync(
                    tableName,
                    filters,
                    cancellationToken
                );
        }


    }
}
