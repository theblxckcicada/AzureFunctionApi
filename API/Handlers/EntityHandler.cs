using DMIX.API.Azure.Models;
using DMIX.API.Azure.Services.ConfigurationManager;
using DMIX.API.Common.Models;
using DMIX.API.Models;
using EasySMSV2.Shared.Azure.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace DMIX.API.Handlers
{
    public interface IEntityHandler<TModel, TKey>
        where TModel : EntityBase<TKey>
    {
        Task<TModel> PopulateEntityAsync(TModel entity, CancellationToken cancellationToken);

        string ValidateEntity(TModel entity);

        EntityQuery GetEntityQuery(Filter? filter = null, EntityQuery? query = null);

        Filter GetPartitionKeyFilters(string partitionKey);

        Filter GetRowKeyFilters(string rowKey);

        List<Filter> EntityBaseFilters(TModel entity);

        List<Filter> CreateFilters<T, K>(
            AppHeader appHeader,
            string? rowKey = null,
            EntityQuery? query = null
        ) where T : EntityBase<K>;
    }



    public class EntityHandler<TModel, TKey>(
            IConfigurationManagerService configurationManagerService,

            IEntityBaseKeyGenerator<TKey> keyGenerator) : IEntityHandler<TModel, TKey>
          where TModel : EntityBase<TKey>
    {
        private readonly IConfigurationManagerService configurationManagerService = configurationManagerService;

        private readonly IEntityBaseKeyGenerator<TKey> keyGenerator = keyGenerator;

        public virtual async Task<TModel> PopulateEntityAsync(
            TModel entity,
            CancellationToken cancellationToken)
        {
            // Simulate async work if needed
            return await Task.FromResult(keyGenerator.Generate(entity));
        }

        public virtual string ValidateEntity(
            TModel entity)
        {
            if (!ModelValidation.ValidateEntity(entity, out var validationMsg))
            {
                return validationMsg;
            }
            return string.Empty;

        }


        public EntityQuery GetEntityQuery(Filter? filter = null, EntityQuery? query = null)
        {
            if (filter is not null)
            {
                return new()
                {

                    PageIndex = 0,
                    PageSize = 10,
                    Query = [
                     new() {
                    Filter  = filter.Value,
                    Sort = filter.KeyName }
                     ]
                };

            }
            query ??= new()
            {

                PageIndex = 0,
                PageSize = 10,
                Query = [
                new() {
                    Filter = nameof(EntityBase<TKey>.RowKey)
                }]
            };
            return query;
        }

        public Filter GetPartitionKeyFilters(string partitionKey)
        {
            return new()
            {
                KeyName = nameof(EntityBase<TKey>.PartitionKey),
                Value = partitionKey,
                IsComparatorSupported = true,
                IsKeyQueryable = true
            };
        }

        public Filter GetRowKeyFilters(string rowKey)
        {
            return new()
            {
                KeyName = nameof(EntityBase<TKey>.RowKey),
                Value = rowKey,
                IsComparatorSupported = true,
                IsKeyQueryable = true
            };
        }

        public List<Filter> EntityBaseFilters(TModel entity)
        {
            return [
                GetPartitionKeyFilters(entity.PartitionKey),
                GetRowKeyFilters(entity.RowKey)
                ];
        }

        public List<Filter> CreateFilters<TModel, TKey>(AppHeader appHeader, string? rowKey = null, EntityQuery? query = null)

            where TModel : EntityBase<TKey>
        {
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);
            Type type = typeof(TModel);

            List<Filter> filters = [];
            if (!string.IsNullOrEmpty(rowKey))
            {
                filters.AddRange([new()
            {
                KeyName = nameof(EntityBase<TKey>.RowKey),
                Value = rowKey,
                IsComparatorSupported = true,
                IsKeyQueryable = true,
            }]);

            }
            Filter filter = new()
            {
                KeyName = nameof(EntityBase<TKey>.PartitionKey),
                Value = user.UserId,
                IsComparatorSupported = true,
                IsKeyQueryable = true,
            };

            switch (type.Name)
            {
                case nameof(Account):
                    filters.AddRange([filter]);
                    return filters;
                default:
                    foreach (var q in query.Query)
                    {
                        filters.AddRange([
                        new(){
                                KeyName = q.Sort,
                                Value = q.Filter,
                                IsComparatorSupported = false,
                                IsKeyQueryable = true,
                            }
                        ]);
                    }

                    return filters;
            }
        }
    }
}
