using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class ContactHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleContactAsync<T>(
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader,
            IAzureTableStorageService tableStorageService,
            IASBService ASBService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            EntityAuditor entityAuditor,
            BulkEntity<T> bulkEntity,
            bool isClientAuthorized = false,
            CancellationToken cancellationToken = default
        )
            where T : Contact, new()
        {
            // handle contact mobile number by replacing the zero at the beginning with the country code
            foreach (var contact in bulkEntity.Entities)
            {
                if (
                    !string.IsNullOrEmpty(contact.MobileNumber)
                    && contact.MobileNumber.StartsWith("0")
                    && !string.IsNullOrEmpty(contact.CountryCode)
                )
                {
                    contact.MobileNumber = contact.CountryCode + contact.MobileNumber[1..];
                }
                // Query the table storage to check if the contact exist
                var existingContact = (
                    await tableStorageService.GetAsync<Contact>(
                        tableName,
                        [
                            new()
                            {
                                IsComparatorSupported = false,
                                IsKeyQueryable = true,
                                KeyName = nameof(Contact.MobileNumber),
                                Value = contact.MobileNumber,
                            },
                            new()
                            {
                                IsComparatorSupported = true,
                                IsKeyQueryable = true,
                                KeyName = nameof(Contact.PartitionKey),
                                Value = contact.PartitionKey,
                            },
                        ],
                        sort,
                        cancellationToken
                    )
                ).FirstOrDefault();
                if (existingContact is not null)
                {
                    contact.RowKey = existingContact.RowKey;
                }
            }
            // Distinct by Mobile number first
            bulkEntity.Entities = [.. bulkEntity.Entities.DistinctBy(x => x.MobileNumber)];

            // Get Entities with RowKey
            var entitiesWithRowKey = bulkEntity
                .Entities.Where(x => !string.IsNullOrEmpty(x.RowKey))
                .DistinctBy(x => x.RowKey)
                .ToList();

            var entitiesWithoutRowKey = bulkEntity
                .Entities.Where(x => string.IsNullOrEmpty(x.RowKey))
                .ToList();

            bulkEntity.Entities = [.. entitiesWithoutRowKey, .. entitiesWithRowKey];
            // send the bulk entities to the table storage
            var entities = await BulkHelper.UpsertBulkEntities<T, T>(
                tableStorageService,
                entityAuditor,
                appHeader,
                tableName,
                filters,
                sort,
                bulkEntity,
                isClientAuthorized,
                ASBService,
                cancellationToken
            );

            return await req.CreateOkResponseAsync(entities, cancellationToken);
        }
    }
}
