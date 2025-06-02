using DMIX.API.Azure.Models;
using DMIX.API.Azure.Services.ConfigurationManager;
using DMIX.API.Common.Models;
using DMIX.API.Models;
using EasySMSV2.Shared.Azure.Models;

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

        List<Filter> CreateFilters(

            string? rowKey = null,
            EntityQuery? query = null
        );


    }



    public class EntityHandler<TModel, TKey>(
            IConfigurationManagerService configurationManagerService,
            IEntityBaseKeyGenerator<TKey> keyGenerator, IHeaderHandler headerHandler, TimeProvider timeProvider) : IEntityHandler<TModel, TKey>
          where TModel : EntityBase<TKey>
    {
        private readonly IConfigurationManagerService configurationManagerService = configurationManagerService;

        private readonly IEntityBaseKeyGenerator<TKey> keyGenerator = keyGenerator;
        private readonly TimeProvider timeProvider = timeProvider;

        public TModel UpdateModelDateTime(TModel entity)
        {
            var utcNow = timeProvider.GetUtcNow(); // returns DateTimeOffset
            var user = configurationManagerService.GetEasySMSUser(headerHandler.Claims);

            var utcDateTime = DateTime.SpecifyKind(utcNow.UtcDateTime, DateTimeKind.Utc);

            if (string.IsNullOrEmpty(entity.CreatedBy))
            {
                entity.CreatedBy = $"{user.FirstName} {user.LastName}";
                entity.CreatedDate = utcDateTime;
            }

            entity.UserId = user.UserId;
            entity.ModifiedBy = $"{user.FirstName} {user.LastName}";
            entity.ModifiedDate = utcDateTime;

            return entity;
        }


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

            if (query?.Query?.FirstOrDefault(x => x.Sort.Equals(nameof(EntityBase<TKey>.RowKey), StringComparison.OrdinalIgnoreCase)) is not null)
            {
                return query;
            }


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
        public List<Filter> GetCleanedUpFilters(List<Filter> filters)
        {
            filters =
                [
                 .. filters.Where(filter =>
                            !string.IsNullOrEmpty(filter.Value) && !string.IsNullOrEmpty(filter.KeyName)
                        ),
                    ];

            filters = [.. filters.DistinctBy(x => (x.KeyName, x.Value))];

            return filters;
        }

        public List<Filter> GetFiltersFromQuery(EntityQuery query, List<Filter> filters)
        {
            query ??= GetEntityQuery(query: query);
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

            return GetCleanedUpFilters(filters);
        }
        public List<Filter> CreateFilters(string? rowKey = null, EntityQuery? query = null)
        {
            var user = configurationManagerService.GetEasySMSUser(headerHandler.Claims);
            Type type = typeof(TModel);

            List<Filter> filters = [];
            if (query is not null)
            {
                var rowKeyQuery = query.Query?.FirstOrDefault(x => !string.IsNullOrEmpty(x.Sort) && x.Sort.Equals(nameof(EntityBase<TKey>.RowKey), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(rowKeyQuery?.Filter))
                {
                    return [GetRowKeyFilters(rowKeyQuery.Filter)];
                }
            }
            if (!string.IsNullOrEmpty(rowKey))
            {
                filters.AddRange([
                    new()
                        {
                        KeyName = nameof(EntityBase<TKey>.RowKey),
                        Value = rowKey,
                        IsComparatorSupported = true,
                        IsKeyQueryable = true,
                        }]);
                return filters;
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
                    return GetFiltersFromQuery(query, filters);
                default:
                    return GetFiltersFromQuery(query, filters);
            }
        }
    }
}
