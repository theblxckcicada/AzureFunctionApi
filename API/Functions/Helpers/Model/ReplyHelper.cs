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
    public static class ReplyHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleReplyGetRequestAsync<T>(
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
            where T : Reply, new()
        {
            var results = await tableStorageService.GetAsync<Reply>(
                tableName,
                filters,
                sort,
                cancellationToken
            );
            //NOTE: No need to group the replies by Sequence because that's already for us in the Connect Mobile service

            return await req.CreateOkResponseAsync(
                new
                {
                    entity = results
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

        [Obsolete]
        public static async Task<HttpResponseData> HandleGetLatestRepliesRequestAsync<T>(
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
            where T : Reply, new()
        {
            // TODO: get latest replies
            List<Filter> _filters = [];
            foreach (var filter in filters)
            {
                switch (filter.KeyName)
                {
                    case "LatestReplies":
                        break;
                    default:
                        _filters.Add(filter);
                        break;
                }
            }

            /* call mobile helper to get latest replies
            ConnectMobileService service = new ConnectMobileService();
            ConnectMobileHelper.HandleMessageRequestAsync(service, tableStorageService);
            */

            var results =
                await tableStorageService.GetAsync<Reply>(
                    tableName,
                    _filters,
                    sort,
                    cancellationToken
                )
            ;

            return await req.CreateOkResponseAsync(
                new
                {
                    entity = results
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
