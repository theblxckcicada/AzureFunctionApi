using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Filter = EasySMS.API.Common.Models.Filter;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class AccountHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleAccountAsync<T>(
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
            where T : Account, new()
        {

            // get user 
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);
            // Query the accounts and sort by value then take the last one
            if (req.Method.Equals(nameof(HttpTriggerMethod.POST), StringComparison.Ordinal))
            {
                List<Filter> filtersList = [new Filter() { KeyName = "", Value = "" }];

                var lastAccountCode = (
                    await tableStorageService.GetAsync<Account>(
                        tableName,
                        filtersList,
                        sort,
                        cancellationToken
                    )
                )
                    .OrderBy(account => account.Code)
                    .Select(account => account.Code)
                    .LastOrDefault();
                // EasySMSUser user = configurationManagerService.GetEasySMSUser(appHeader.Claims);

                // Increment to the accounts
                foreach (var account in bulkEntity.Entities)
                {
                    // set account code
                    lastAccountCode++;
                    account.Code = lastAccountCode;
                    account.EmailAddress = user.EmailAddress;
                    account.Country = user.Country;

                    // update parent rowkey
                    if (account.Type == AccountType.Main)
                    {
                        account.ParentRowKey = account.RowKey;
                    }
                    else
                    { // get the parent account
                        if (
                            !string.Equals(
                                account.ParentRowKey,
                                account.PartitionKey,
                                StringComparison.Ordinal
                            )
                        )
                        {
                            var mainAccount =
                                (
                                    await tableStorageService.GetAsync<Account>(
                                        tableName,
                                        [
                                            new()
                                            {
                                                KeyName = nameof(
                                                    AzureTableStorageSystemProperty.RowKey
                                                ),
                                                Value = account.ParentRowKey,
                                                IsComparatorSupported = true,
                                                IsKeyQueryable = true,
                                            },
                                        ],
                                        sort,
                                        cancellationToken
                                    )
                                ).FirstOrDefault()
                                ?? throw new BadHttpRequestException(
                                    "Parent Account RowKey is invalid"
                                );
                        }
                    }
                }
            }

            // update parent row key
            foreach (var account in bulkEntity.Entities)
            {
                // make sure the integration secret is not added to the table storage
                account.IntegrationSecret = "";
                account.Country = user.Country;
                // update parent row key
                if (account.Type == AccountType.Main)
                {
                    account.ParentRowKey = account.RowKey;
                }
            }

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



        [Obsolete]
        public static async Task<HttpResponseData> HandleAccountGetRequestAsync<T>(
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
            where T : Account, new()
        {
            var results = await tableStorageService.GetAsync<Account>(
                tableName,
                filters,
                sort,
                cancellationToken
            );

            var groupedAccounts = results.GroupBy(x => x.Type);

            foreach (var groupedAccount in groupedAccounts)
            {
                // get the first account
                var account = groupedAccount.FirstOrDefault();
                if (
                    account.Type == AccountType.Integration
                    && !string.IsNullOrEmpty(account.UserId)
                )
                {
                    foreach (var acc in groupedAccount)
                    {
                        // Generate api key
                        var apiKey = ApiKeyGenerator.GenerateApiKey(
                            acc.RowKey,
                            acc.Code,
                            acc.Version
                        );
                        acc.IntegrationSecret = apiKey;
                    }
                }
            }

            return await req.CreateOkResponseAsync(
                new
                {
                    entity = groupedAccounts
                        .SelectMany(g => g)
                        .Skip(count: sort.PageIndex * sort.PageSize)
                        .Take(sort.PageSize)
                        .ToList(),
                    query = sort,
                    total = results.Count,
                    filters,
                },
                cancellationToken: cancellationToken
            );
        }
    }
}
