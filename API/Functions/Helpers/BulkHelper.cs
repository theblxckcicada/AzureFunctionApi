using Azure.Data.Tables;
using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers
{
    public static class BulkHelper
    {
        [Obsolete]
        public static async Task<List<T>> UpsertEntitiesAsync<T, U>(
            IAzureTableStorageService tableStorageService,
            List<T> entities,
            string tableName,
            string partitionKey,
            CancellationToken cancellationToken = default
        )
            where T : class, ITableEntity, new()
            where U : class, ITableEntity, new()
        {
            Filter filter =
                new()
                {
                    IsComparatorSupported = true,
                    IsKeyQueryable = true,
                    KeyName = nameof(AzureTableStorageSystemProperty.PartitionKey),
                    Value = partitionKey,
                };

            List<T> addEntities = [];
            List<T> updateEntities = [];
            foreach (var ent in entities)
            { // update entity partition key
                ent.PartitionKey = partitionKey;
                if (string.IsNullOrEmpty(ent.RowKey) || !Helper.IsValidUUID(ent.RowKey))
                {
                    addEntities.Add(ent);
                }
                else
                {
                    updateEntities.Add(ent);
                }
            }

            var addResults = await tableStorageService.BatchFuncAsync<T, U>(
                tableName,
                addEntities,
                [filter],
                TableTransactionActionType.Add,
                cancellationToken
            );
            var updateResults = await tableStorageService.BatchFuncAsync<T, U>(
                tableName,
                updateEntities,
                [filter],
                TableTransactionActionType.UpdateReplace,
                cancellationToken,
                false
            );

            List<T> returnedEntities = [.. addResults, .. updateResults];
            return returnedEntities;
        }

        [Obsolete]
        public static async Task<BulkEntity<T>> UpsertBulkEntities<T, U>(
            IAzureTableStorageService tableStorageService,
            EntityAuditor entityAuditor,
            AppHeader appHeader,
            string tableName,
            List<Filter> filters,
            Sort sort,
            BulkEntity<T> bulkEntities,
            bool IsClientAppAudienceValid = false,
            IASBService asbService = null,
            CancellationToken cancellationToken = default
        )
            where T : EntityBase, new()
            where U : EntityBase, new()
        {
            _ = Helper.UpdateModel(bulkEntities, entityAuditor, appHeader);
            var partitionKey = filters
                .FirstOrDefault(filter =>
                    filter.KeyName.Equals(
                        nameof(AzureTableStorageSystemProperty.PartitionKey),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                ?.Value;

            if (!string.IsNullOrEmpty(partitionKey))
            {
                List<T> returnedEntities = [];
                // TableEntity result;

                var entities = bulkEntities.Entities.ToList();
                // Validate Entity
                if (!ModelValidation.ValidateEntities(entities, out var validationMsg))
                {
                    throw new BadHttpRequestException(validationMsg);
                }

                List<T> ASBAssetsListQueue = [];
                if (IsClientAppAudienceValid)
                {
                    var groupedEntities = entities.GroupBy(entity => entity.PartitionKey).ToList();
                    foreach (var groupedEntity in groupedEntities)
                    {
                        partitionKey = groupedEntity.FirstOrDefault().PartitionKey;
                        var returnedResults = await UpsertEntitiesAsync<T, U>(
                            tableStorageService,
                            [.. groupedEntity],
                            tableName,
                            partitionKey,
                            cancellationToken
                        );
                        ASBAssetsListQueue = [.. ASBAssetsListQueue, .. returnedResults];
                    }
                }
                else if (string.IsNullOrEmpty(partitionKey))
                {
                    var groupedEntities = entities.GroupBy(entity => entity.PartitionKey).ToList();
                    foreach (var groupedEntity in groupedEntities)
                    {
                        partitionKey = groupedEntity.FirstOrDefault().PartitionKey;
                        var returnedResults = await UpsertEntitiesAsync<T, U>(
                            tableStorageService,
                            [.. groupedEntity],
                            tableName,
                            partitionKey,
                            cancellationToken
                        );
                        ASBAssetsListQueue = [.. ASBAssetsListQueue, .. returnedResults];
                    }
                }
                else
                {
                    var returnedResults = await UpsertEntitiesAsync<T, U>(
                        tableStorageService,
                        entities,
                        tableName,
                        partitionKey,
                        cancellationToken
                    );
                    ASBAssetsListQueue = [.. ASBAssetsListQueue, .. returnedResults];
                }

                if (asbService is not null)
                {
                    await asbService.SendBatchMessages(ASBAssetsListQueue);
                }

                return new BulkEntity<T>() { Entities = [.. ASBAssetsListQueue] };
            }
            else
            {
                throw new BadHttpRequestException("Partition Key can not be null or empty");
            }
        }

        [Obsolete]
        public static async Task<BulkEntity<T>> DeleteBulkEntities<T, U>(
            HttpRequestData? req,
            IAzureTableStorageService tableStorageService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            BulkEntity<T> bulkEntities,
            IASBService asbService = null,
            CancellationToken cancellationToken = default
        )
            where T : class, ITableEntity, new()
            where U : class, ITableEntity, new()
        {
            List<T> returnedEntities = [];
            T? entity = null;

            List<T> ASBAssetsListQueue = [];
            var entities = bulkEntities
                .Entities.Where(entity =>
                    !string.IsNullOrEmpty(entity.RowKey) && Helper.IsValidUUID(entity.RowKey)
                )
                .ToList();
            // Validate Entity
            if (!ModelValidation.ValidateEntities(entities, out var validationMsg))
            {
                throw new BadHttpRequestException(validationMsg);
            }
            string? partitionKey = null;

            var groupedEntities = entities.GroupBy(entity => entity.PartitionKey).ToList();
            foreach (var groupedEntity in groupedEntities)
            {
                partitionKey = groupedEntity.FirstOrDefault().PartitionKey;
                filters =
                [
                    new()
                    {
                        IsComparatorSupported = true,
                        IsKeyQueryable = true,
                        KeyName = nameof(AzureTableStorageSystemProperty.PartitionKey),
                        Value = partitionKey,
                    },
                ];

                var deletedEntities = await tableStorageService.BatchFuncAsync<T, U>(
                    tableName,
                    [.. entities],
                    filters,
                    TableTransactionActionType.Delete,
                    cancellationToken,
                    false
                );

                ASBAssetsListQueue = [.. ASBAssetsListQueue, .. deletedEntities];
            }

            if (asbService is not null)
            {
                await asbService.SendBatchMessages(ASBAssetsListQueue);
            }
            returnedEntities = [.. ASBAssetsListQueue];

            return new BulkEntity<T>() { Entities = [.. returnedEntities] };
        }
    }
}
