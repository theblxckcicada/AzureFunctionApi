using System.Reflection;
using Azure;
using Azure.Data.Tables;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Common.Models;
using EasySMS.API.Functions.Helpers;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace EasySMS.API.Azure.Services.TableStorage
{
    public interface IAzureTableStorageService
    {
        Task<List<T>> GetAsync<T>(
            string tableName,
            List<Filter> filters,
            Sort sort,
            CancellationToken cancellationToken = default
        );
        Task<TableEntity> InsertAsync<TEntity>(
            string tableName,
            TEntity entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        )
            where TEntity : class, ITableEntity, new();
        Task<TableEntity> UpdateAsync<TEntity>(
            string tableName,
            TEntity entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        )
            where TEntity : class, ITableEntity, new();
        Task<TableEntity> DeleteAsync(
            string tableName,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        );
        Task<List<TEntity>> BatchFuncAsync<TEntity, U>(
            string tableName,
            List<TEntity> entities,
            List<Filter> filters,
            TableTransactionActionType type,
            CancellationToken cancellationToken = default,
            bool CreateRowKey = true
        )
            where TEntity : class, ITableEntity, new()
            where U : class, ITableEntity, new();

        // List<List<TableTransactionAction>> CreateBatches<TEntity>(List<TEntity> entities, List<Filter> filters, TableTransactionActionType type, int batchSize = 100, bool CreateRowKey = true)
        //   where TEntity : class, ITableEntity, new();

        Task SubmitBatchesAsync(
            List<List<TableTransactionAction>> batches,
            TableClient tableClient
        );
        public List<T> ConvertModel<T, U>(List<U> entities)
            where T : class, ITableEntity, new()
            where U : class, ITableEntity, new();
        Task<List<T>> HandleSubItemsAsyncTransactionsAsync<T>(
            List<T> entities,
            List<Filter> filters,
            KeyValuePair<string, (Type type, List<object> subItems)> item,
            CancellationToken cancellationToken = default
        )
            where T : class, ITableEntity, new();
    }

    public class AzureTableStorageService(
        TableServiceClient serviceClient,
        IConfigurationManagerService configurationManagerService
    ) : IAzureTableStorageService
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

        private static List<T> GetFilteredEntities<T>(
            List<TableEntity> entities,
            List<Filter> filter
        )
        {
            var entity = JsonConvert.DeserializeObject<List<T>>(
                JsonConvert.SerializeObject(entities)
            );
            var properties = typeof(T).GetProperties(
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

        private static List<T> GetSortedEntities<T>(List<T> entities, Sort sort)
        {
            var property = typeof(T).GetProperty(sort.FilterValue);
            entities = sort.SortDirection.Equals(
                nameof(DataSort.asc),
                StringComparison.OrdinalIgnoreCase
            )
                ? [.. entities.OrderBy(x => property.GetValue(x, null))]
                : [.. entities.OrderByDescending(x => property.GetValue(x, null))];

            return entities;
        }

        private async Task<TableEntity> GetRowAsync<TEntity>(
            string tableName,
            TEntity entity,
            CancellationToken cancellationToken = default
        )
            where TEntity : class, ITableEntity, new()
        {
            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);
            return await tableClient.GetEntityAsync<TableEntity>(
                entity.PartitionKey,
                entity.RowKey,
                cancellationToken: cancellationToken
            );
        }

        public async Task<List<T>> GetAsync<T>(
            string tableName,
            List<Filter> filters,
            Sort sort,
            CancellationToken cancellationToken = default
        )
        {
            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);
            List<TableEntity> results = [];

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
                var result = await tableClient.GetEntityAsync<TableEntity>(
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

                var queryResultsFilter = tableClient.QueryAsync<TableEntity>(
                    filter: oDataQuery,
                    cancellationToken: cancellationToken
                );
                await foreach (var result in queryResultsFilter)
                {
                    results.Add(result);
                }
            }

            // Apply the filter query
            var filteredResults = GetFilteredEntities<T>(results, filters);
            var sortedEntities = GetSortedEntities(filteredResults, sort);
            return sortedEntities;
        }

        public async Task<TableEntity> InsertAsync<TEntity>(
            string tableName,
            TEntity entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        )
            where TEntity : class, ITableEntity, new()
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
            return await tableClient.GetEntityAsync<TableEntity>(
                entity.PartitionKey,
                entity.RowKey,
                cancellationToken: cancellationToken
            );
        }

        public async Task<TableEntity> UpdateAsync<TEntity>(
            string tableName,
            TEntity entity,
            List<Filter> filters,
            CancellationToken cancellationToken = default
        )
            where TEntity : class, ITableEntity, new()
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
            return await tableClient.GetEntityAsync<TableEntity>(
                entity.PartitionKey,
                entity.RowKey,
                cancellationToken: cancellationToken
            );
        }

        public async Task<TableEntity> DeleteAsync(
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
            var tableEntity = await tableClient.GetEntityAsync<TableEntity>(
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

        public async Task<List<TEntity>> BatchFuncAsync<TEntity, U>(
            string tableName,
            List<TEntity> entities,
            List<Filter> filters,
            TableTransactionActionType type,
            CancellationToken cancellationToken = default,
            bool CreateRowKey = true
        )
            where TEntity : class, ITableEntity, new()
            where U : class, ITableEntity, new()
        {
            var serviceClient = await this.serviceClient.WithTableAsync(
                tableName,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(tableName);

            foreach (var entity in entities)
            {
                entity.PartitionKey = GetFilterItem(
                    filters,
                    nameof(AzureTableStorageSystemProperty.PartitionKey)
                ).Value;
                if (CreateRowKey)
                {
                    entity.RowKey = Guid.NewGuid().ToString();
                }
            }

            // Get sub items
            var subItems = Helper.AddSubEntities(entities);

            //convert entities to those that have no lists
            var convertedEntities = ConvertModel<U, TEntity>(entities);
            convertedEntities = Helper.ConvertDateTimePropertiesToUtc(convertedEntities);

            // submit batches
            var batches = CreateBatches(
                convertedEntities,
                filters,
                type,
                100,
                false
            );
            await SubmitBatchesAsync(batches, tableClient);

            return await Task.FromResult(entities);
        }

        public async Task SubmitBatchesAsync(
            List<List<TableTransactionAction>> batches,
            TableClient tableClient
        )
        {
            await Parallel.ForEachAsync(
                batches,
                async (batch, cancellationToken) =>
                {
                    _ = await tableClient.SubmitTransactionAsync(batch, cancellationToken);
                }
            );
        }

        public static List<List<TableTransactionAction>> CreateBatches<TEntity>(
            List<TEntity> entities,
            List<Filter> filters,
            TableTransactionActionType type,
            int batchSize = 100,
            bool CreateRowKey = true
        )
            where TEntity : class, ITableEntity, new()
        {
            List<List<TableTransactionAction>> batches = [];
            List<TableTransactionAction> currentBatch = [];

            foreach (var entity in entities)
            {
                if (CreateRowKey)
                {
                    entity.RowKey = Guid.NewGuid().ToString();
                }
                // Create a sample TableTransactionAction object (replace with your actual object)
                TableTransactionAction transactionAction = new(type, entity);
                currentBatch.Add(transactionAction);

                if (currentBatch.Count == batchSize)
                {
                    batches.Add(currentBatch);
                    currentBatch = [];
                }
            }

            // Add the remaining elements to the last batch
            if (currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        private static List<T> GetSubItems<T>(
            KeyValuePair<string, (Type type, List<object> subItems)> item
        )
            where T : class, ITableEntity, new()
        {
            List<T> entities = [];
            foreach (var itemValue in item.Value.subItems)
            {
                if (itemValue is ITableEntity tableValue)
                {
                    entities.Add((T)tableValue);
                }
            }
            return entities;
        }

        [Obsolete]
        public async Task<List<T>> HandleSubItemsAsyncTransactionsAsync<T>(
            List<T> entities,
            List<Filter> filters,
            KeyValuePair<string, (Type type, List<object> subItems)> item,
            CancellationToken cancellationToken = default
        )
            where T : class, ITableEntity, new()
        {
            List<List<TableTransactionAction>> batches = [];

            // get entities with row keys
            var subWithRowKey = entities
                .Where(entity =>
                    !string.IsNullOrEmpty(entity.RowKey) && Helper.IsValidUUID(entity.RowKey)
                )
                .ToList();
            var subWithoutRowKey = entities
                .Where(entity =>
                    string.IsNullOrEmpty(entity.RowKey) || !Helper.IsValidUUID(entity.RowKey)
                )
                .ToList();

            subWithoutRowKey = Helper.ConvertDateTimePropertiesToUtc(subWithoutRowKey);
            subWithRowKey = Helper.ConvertDateTimePropertiesToUtc(subWithRowKey);

            var result = CreateBatches(
                subWithoutRowKey,
                filters,
                TableTransactionActionType.Add,
                100,
                true
            );
            batches = [.. batches, .. result];

            result = CreateBatches(
                subWithRowKey,
                filters,
                TableTransactionActionType.UpsertReplace,
                100,
                false
            );
            batches = [.. batches, .. result];

            // submit batches
            var serviceClient = await this.serviceClient.WithTableAsync(
                item.Key,
                cancellationToken
            );
            var tableClient = serviceClient.GetTableClient(item.Key);
            await SubmitBatchesAsync(batches, tableClient);
            return entities;
        }

        public async Task<List<U>> AddAndUpdateSubItem<T, U>(
            string tableName,
            TableEntity entity,
            List<U> subItems,
            CancellationToken cancellationToken = default
        )
            where T : class
            where U : class, ITableEntity, new()
        {
            List<TableEntity> items = [];
            // For every item insert it into the table storage
            List<Filter> itemFilters =
            [
                new Filter()
                {
                    KeyName = nameof(AzureTableStorageSystemProperty.PartitionKey),
                    Value = entity.RowKey,
                    IsKeyQueryable = true,
                    IsComparatorSupported = true,
                },
            ];

            foreach (var item in subItems)
            {
                if (item.RowKey is not null)
                {
                    List<Filter> updateItemFilters = [.. itemFilters.Take(itemFilters.Count)];
                    updateItemFilters.Add(
                        new Filter()
                        {
                            KeyName = nameof(AzureTableStorageSystemProperty.RowKey),
                            Value = item.RowKey,
                            IsKeyQueryable = true,
                            IsComparatorSupported = true,
                        }
                    );
                    var updatedSubEntity = await UpdateAsync(
                        tableName,
                        item,
                        updateItemFilters,
                        cancellationToken
                    );
                    items.Add(updatedSubEntity);
                }
                else
                {
                    var addedSubEntity = await InsertAsync(
                        tableName,
                        item,
                        itemFilters,
                        cancellationToken
                    );
                    items.Add(addedSubEntity);
                }
            }

            return JsonConvert.DeserializeObject<List<U>>(JsonConvert.SerializeObject(items));
        }

        public async Task<List<T>> PopulateSubItems<T, U>(
            string tableName,
            List<T> entities,
            string propertyName,
            CancellationToken cancellationToken
        )
            where T : class, ITableEntity, new()
            where U : class, ITableEntity, new()
        {
            Sort itemSort =
                new()
                {
                    FilterValue = nameof(AzureTableStorageSystemProperty.RowKey),
                    SortDirection = nameof(DataSort.asc),
                    PageSize = 100,
                    PageIndex = 0,
                };
            foreach (var entity in entities)
            {
                List<Filter> itemFilters =
                [
                    new Filter()
                    {
                        KeyName = nameof(AzureTableStorageSystemProperty.PartitionKey),
                        Value = entity.RowKey,
                        IsKeyQueryable = true,
                        IsComparatorSupported = true,
                    },
                ];

                // For every item insert it into the table storage
                var propertyInfo = entity
                    .GetType()
                    .GetProperty(
                        propertyName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                    );

                var productData = await GetAsync<U>(
                    tableName,
                    itemFilters,
                    itemSort,
                    cancellationToken
                );
                if (propertyInfo is not null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(entity, productData);
                }
                else
                {
                    throw new BadHttpRequestException(
                        $"Could not retrieve {propertyName} entities"
                    );
                }
            }

            return entities;
        }

        public List<T> ConvertModel<T, U>(List<U> entities)
            where T : class, ITableEntity, new()
            where U : class, ITableEntity, new()
        {
            List<T> items = [];
            foreach (var entity in entities)
            {
                T item = new();

                // Get all properties of Businessitem and item
                var uProperties = typeof(U).GetProperties();
                var tProperties = typeof(T).GetProperties();

                // Copy properties from Businessitem to item
                foreach (var uProperty in uProperties)
                {
                    var tProperty = tProperties.FirstOrDefault(p =>
                        p.Name == uProperty.Name
                    );

                    if (tProperty != null && tProperty.CanWrite)
                    {
                        var value = uProperty.GetValue(entity);
                        tProperty.SetValue(item, value);
                    }
                }
                items.Add(item);
            }
            return items;
        }
    }
}
