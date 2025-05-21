using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Functions.Helpers;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Filter = EasySMS.API.Common.Models.Filter;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class GroupHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleGroupDeleteAsync<T>(
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
            where T : Group, new()
        {

            // get user 
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);

            // query the group contact data 
            List<Task> tasks = [];
            foreach (var group in bulkEntity.Entities)
            {

                var groupContact =
                    await tableStorageService.GetAsync<GroupContact>(
                         nameof(GroupContact),
                         [new Filter() { IsKeyQueryable = true, IsComparatorSupported = true, KeyName = nameof(GroupContact.PartitionKey), Value = group.RowKey }],
                         sort,
                         cancellationToken
                        );
                tasks.Add(BulkHelper.DeleteBulkEntities<GroupContact, GroupContact>(req, tableStorageService, nameof(GroupContact), [], new()
                {
                }, new()
                {
                    Entities = [.. groupContact]
                }, cancellationToken: cancellationToken));
                tasks.Add(BulkHelper.DeleteBulkEntities<Group, Group>(req, tableStorageService, nameof(Group), [], new()
                {
                }, new()
                {
                    Entities = [group]
                }, cancellationToken: cancellationToken));

            }
            await Task.WhenAll(tasks);
            return await req.CreateOkResponseAsync(bulkEntity.Entities, cancellationToken);
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
