using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.GraphApi;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class APIKeyHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleApiKeyRequestAsync<T>(
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader,
            IAzureTableStorageService tableStorageService,
            IGraphApiService graphApiService,
            IASBService ASBService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            EntityAuditor entityAuditor,
            BulkEntity<T> bulkEntity,
            bool isClientAuthorized = false,
            CancellationToken cancellationToken = default
        )
            where T : Account, new()
        {
            // get a user
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);
            // Query the accounts and sort by value then take the last one
            if (req.Method.Equals(nameof(HttpTriggerMethod.PUT), StringComparison.Ordinal))
            {
                // Name name = configurationManagerService.GetUserNames(appHeader.Claims);
                foreach (var entity in bulkEntity.Entities)
                {
                    entity.Version++;
                    entity.IntegrationSecret = "";
                    entity.Country = user.Country;
                }
            }
            if (req.Method.Equals(nameof(HttpTriggerMethod.POST), StringComparison.Ordinal))
            {
                // Name name = configurationManagerService.GetUserNames(appHeader.Claims);
                foreach (var entity in bulkEntity.Entities)
                {
                    entity.EmailAddress = user.EmailAddress;
                }
            }

            // send the bulk entities to the table storage
            var entities = await BulkHelper.UpsertBulkEntities<T, T>(
                tableStorageService,
                entityAuditor,
                appHeader,
                nameof(Account),
                filters,
                sort,
                bulkEntity,
                isClientAuthorized,
                ASBService,
                cancellationToken
            );

            // update the background jobs 
            await TokenHelper.HandleTokenReminderBackgroundJobAsync(tableStorageService, [.. entities.Entities], cancellationToken);

            // return the value to the user
            foreach (var entity in entities.Entities)
            {
                if (entity.Type == AccountType.Integration)
                {
                    // Generate api key
                    var apiKey = ApiKeyGenerator.GenerateApiKey(
                        entity.RowKey,
                        entity.Code,
                        entity.Version
                    );
                    entity.IntegrationSecret = apiKey;
                }
            }

            return await req.CreateOkResponseAsync(entities, cancellationToken);
        }
    }
}
