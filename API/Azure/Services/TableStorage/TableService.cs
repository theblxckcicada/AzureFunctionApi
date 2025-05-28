using System.Reflection;
using Azure;
using Azure.Data.Tables;
using DMIX.API.Common.Models;
using DMIX.API.Models;
using Newtonsoft.Json;

namespace DMIX.API.Azure.Services.TableStorage
{
    public interface IAzureTableStorageService<TModel, TKey>
        where TModel : EntityBase<TKey>
    {

        Task<List<TModel>> GetAsync(
            string tableName,
            List<Filter> filters,
            EntityQuery query,
            CancellationToken cancellationToken = default
        );
        Task<TModel> InsertAsync(
            string tableName,
            TModel entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        );

        Task<TModel> UpdateAsync(
            string tableName,
            TModel entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        );
        Task<TModel> DeleteAsync(
            string tableName,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        );

    }

    public class AzureTableStorageService<TModel, TKey>(
        TableServiceClient serviceClient
    ) : IAzureTableStorageService<TModel, TKey>
        where TModel : EntityBase<TKey>
    {
        private readonly TableServiceClient serviceClient = serviceClient;

        private static Filter GetFilterItem(List<Filter> filter, string keyName)
        {
            var result = JsonConvert.SerializeObject(
                from item in filter
                where item.KeyName == keyName
                select new
                {
                    item.KeyName,
                    item.Value,
                    item.Comparator,
                    item.Operator,
                }
            );

            result = result[1..^1];
            if (result == string.Empty)
            {
                return new Filter { KeyName = string.Empty, Value = string.Empty };
            }
            return JsonConvert.DeserializeObject<Filter>(result);
        }

        private static string GetODataQuery(List<Filter> filter)
        {
            var result = string.Empty;

            var filters = filter.Where(item => item.IsComparatorSupported && item.IsKeyQueryable);
            var count = 1;
            foreach (var item in filters)
            {
                switch (count)
                {
                    case 1:
                        result = $"{item.KeyName} {item.Comparator} '{item.Value}'";
                        break;
                    default:
                        result +=
                            $" {item.Operator} {item.KeyName} {item.Comparator} '{item.Value}'";
                        break;
                }
                count++;
            }

            return result;
        }

        private static List<TModel> GetFilteredEntities(
            List<TModel> entities,
            List<Filter> filter
        )
        {
            var entity = JsonConvert.DeserializeObject<List<TModel>>(
                JsonConvert.SerializeObject(entities)
            );
            var properties = typeof(TModel).GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            var filters = filter.Where(item => !item.IsComparatorSupported && item.IsKeyQueryable);

            var count = 1;
            foreach (var item in filters)
            {
                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(item.KeyName, StringComparison.OrdinalIgnoreCase)
                );

                if (property != null)
                {
                    var propertyType =
                        Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (propertyType.IsEnum)
                    {
                        if (
                            Enum.TryParse(
                                propertyType,
                                item.Value,
                                ignoreCase: true,
                                out var parsedEnumValue
                            )
                        )
                        {
                            entity =
                            [
                                .. entity.Where(x => property.GetValue(x).Equals(parsedEnumValue))
                            ];
                        }
                    }
                    else if (propertyType == typeof(string))
                    {
                        entity =
                        [
                            .. entity.Where(x =>
                            {
                                var propertyValue =
                                    property.GetValue(x)?.ToString() ?? string.Empty;
                                return propertyValue.Contains(
                                    item.Value,
                                    StringComparison.OrdinalIgnoreCase
                                );
                            })
                        ];
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        entity =
                        [
                            .. entity.Where(x =>
                                DateTime
                                    .Parse(property.GetValue(x).ToString())
                                    .Equals(DateTime.Parse(item.Value))
                            )
                        ];
                    }
                    else if (propertyType == typeof(int))
                    {
                        entity =
                        [
                            .. entity.Where(x =>
                                int.Parse(property.GetValue(x).ToString())
                                    .Equals(int.Parse(item.Value))
                            )
                        ];
                    }
                    else if (propertyType == typeof(double))
                    {
                        entity =
                        [
                            .. entity.Where(x =>
                                double.Parse(property.GetValue(x).ToString())
                                    .Equals(double.Parse(item.Value))
                            )
                        ];
                    }
                    // Add additional type-specific filtering logic as needed
                }
                count++;
            }

            return entity;
        }

        private static List<TModel> GetSortedEntities(List<TModel> entities, EntityQuery query)
        {
            if (!query.Query.Any())
                return entities;

            IOrderedEnumerable<TModel>? sortedEntities = null;

            foreach (var sort in query.Query)
            {
                if (string.IsNullOrWhiteSpace(sort.Sort))
                    continue;

                var property = typeof(TModel).GetProperty(sort.Sort);
                if (property == null)
                    continue;

                bool ascending = sort.SortDirection?.Equals(nameof(DataSort.asc), StringComparison.OrdinalIgnoreCase) ?? true;

                if (sortedEntities == null)
                {
                    sortedEntities = ascending
                        ? entities.OrderBy(x => property.GetValue(x, null))
                        : entities.OrderByDescending(x => property.GetValue(x, null));
                }
                else
                {
                    sortedEntities = ascending
                        ? sortedEntities.ThenBy(x => property.GetValue(x, null))
                        : sortedEntities.ThenByDescending(x => property.GetValue(x, null));
                }
            }

            return sortedEntities?.ToList() ?? entities;
        }


        private async Task<TModel> GetRowAsync(
            string tableName,
            TModel entity,
            CancellationToken cancellationToken = default
        )

        {
            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);
            return await tableClient.GetEntityAsync<TModel>(
                entity.PartitionKey,
                entity.RowKey,
                cancellationToken: cancellationToken
            );
        }

        public async Task<List<TModel>> GetAsync(
            string tableName,
            List<Filter> filters,
            EntityQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);
            List<TModel> results = [];

            // If no RowKey provide, try to query
            var rowKey = GetFilterItem(
                filters,
                nameof(AzureTableStorageSystemProperty.RowKey)
            )?.Value;
            var partitionKey = GetFilterItem(
                filters,
                nameof(AzureTableStorageSystemProperty.PartitionKey)
            )?.Value;
            if (!string.IsNullOrEmpty(rowKey) && !string.IsNullOrEmpty(partitionKey))
            {
                var result = await tableClient.GetEntityAsync<TModel>(
                    GetFilterItem(
                        filters,
                        nameof(AzureTableStorageSystemProperty.PartitionKey)
                    )?.Value,
                    GetFilterItem(filters, nameof(AzureTableStorageSystemProperty.RowKey))?.Value,
                    cancellationToken: cancellationToken
                );

                results.Add(result);
            }
            else
            {
                // TODO: ADD maxPerPage and Select {Property columns}
                var oDataQuery = GetODataQuery(filters);

                var queryResultsFilter = tableClient.QueryAsync<TModel>(
                    filter: oDataQuery,
                    cancellationToken: cancellationToken
                );
                await foreach (var result in queryResultsFilter)
                {
                    results.Add(result);
                }
            }

            // Apply the filter query
            var filteredResults = GetFilteredEntities(results, filters);
            var sortedEntities = GetSortedEntities(filteredResults, query);
            return sortedEntities;
        }

        public async Task<TModel> InsertAsync(
            string tableName,
            TModel entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        )

        {
            entity.PartitionKey = GetFilterItem(
                filters,
                nameof(AzureTableStorageSystemProperty.PartitionKey)
            ).Value;

            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);

            _ = await tableClient.AddEntityAsync(entity, cancellationToken);
            return await tableClient.GetEntityAsync<TModel>(
                entity.PartitionKey,
                entity.RowKey,
                cancellationToken: cancellationToken
            );
        }

        public async Task<TModel> UpdateAsync(
            string tableName,
            TModel entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        )

        {
            entity.PartitionKey = GetFilterItem(
                filters,
                nameof(AzureTableStorageSystemProperty.PartitionKey)
            ).Value;
            entity.RowKey = GetFilterItem(
                filters,
                nameof(AzureTableStorageSystemProperty.RowKey)
            ).Value;
            entity.ETag = new ETag(
                GetFilterItem(filters, nameof(AzureTableStorageSystemProperty.ETag)).Value
            );

            var tableEntity = await GetRowAsync(tableName, entity, cancellationToken);
            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);

            _ = await tableClient.UpdateEntityAsync(
                entity,
                tableEntity.ETag,
                cancellationToken: cancellationToken
            );
            return await tableClient.GetEntityAsync<TModel>(
                entity.PartitionKey,
                entity.RowKey,
                cancellationToken: cancellationToken
            );
        }

        public async Task<TModel> DeleteAsync(
            string tableName,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        )
        {
            var partitionKey = GetFilterItem(
                filters,
                nameof(AzureTableStorageSystemProperty.PartitionKey)
            ).Value;
            var rowKey = GetFilterItem(
                filters,
                nameof(AzureTableStorageSystemProperty.RowKey)
            ).Value;

            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);
            var tableEntity = await tableClient.GetEntityAsync<TModel>(
                partitionKey,
                rowKey,
                cancellationToken: cancellationToken
            );

            _ = await tableClient.DeleteEntityAsync(
                partitionKey,
                rowKey,
                cancellationToken: cancellationToken
            );
            return tableEntity;
        }
    }
}
