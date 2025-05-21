using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.GraphApi;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Functions.Helpers.Model;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using EasySMSV2.Shared.Messaging.ConnectMobile.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers
{
    public static class ProcessFunction
    {
        [Obsolete]
        public static async Task<HttpResponseData> ProcessEntityAsync<T>(
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader,
            IAzureTableStorageService tableStorageService,
            IGraphApiService graphApiService,
            IConnectMobileService connectMobileService,
            IASBService ASBService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            EntityAuditor entityAuditor,
            bool isClientAuthorized = false,
            CancellationToken cancellationToken = default
        )
            where T : EntityBase, new()
        {
            var errorMessage = string.Empty;

            BulkEntity<T> bulkEntity = new();

            if (!req.Method.Equals(nameof(HttpTriggerMethod.GET), StringComparison.Ordinal))
            {
                bulkEntity = await req.ReadFromJsonAsync<BulkEntity<T>>(
                    cancellationToken: cancellationToken
                );
            }

            switch (req.Method)
            {
                case nameof(HttpTriggerMethod.GET):
                    // check if the get request is for TokenDrawn
                    if (bulkEntity is BulkEntity<TokenAutoDrawn>)
                    {
                        return await req.CreateBadRequestResponseAsync(
                            "Method not supported for this model",
                            System.Net.HttpStatusCode.BadRequest,
                            cancellationToken
                        );
                    }

                    // check if the get request is for Statistics
                    if (bulkEntity is BulkEntity<Statistics> statisticEnt)
                    {
                        return await StatisticHelper.HandleStatisticGetRequestAsync<Statistics>(
                            req,
                            tableStorageService,
                            tableName,
                            filters,
                            sort,
                            cancellationToken: cancellationToken
                        );
                    }

                    //check if the get request is for Reply
                    if (bulkEntity is BulkEntity<Reply> replyEnt)
                    {
                        string[] _filters = ["LatestReplies"];
                        if (Helper.hasCustomFilter(filters, _filters))
                        {
                            // get latest replies
                            return await ReplyHelper.HandleGetLatestRepliesRequestAsync(
                                req,
                                configurationManagerService,
                                appHeader,
                                tableStorageService,
                                ASBService,
                                tableName,
                                filters,
                                sort,
                                entityAuditor,
                                replyEnt,
                                isClientAuthorized,
                                cancellationToken
                            );
                        }
                        // get replies
                        return await ReplyHelper.HandleReplyGetRequestAsync(
                            req,
                            configurationManagerService,
                            appHeader,
                            tableStorageService,
                            ASBService,
                            tableName,
                            filters,
                            sort,
                            entityAuditor,
                            replyEnt,
                            isClientAuthorized,
                            cancellationToken
                        );
                    }

                    // check if the get request is for SMS
                    if (bulkEntity is BulkEntity<SMS> smsEnt)
                    {
                        string[] _filters =
                        [
                            "StartDate",
                            "EndDate",
                            "ExportToExcel",
                            "Status",
                            "HasReply"
                        ];
                        if (Helper.hasCustomFilter(filters, _filters))
                        {
                            // get history
                            return await HistoryHelper.HandleHistoryGetRequestAsync(
                                req,
                                configurationManagerService,
                                appHeader,
                                tableStorageService,
                                ASBService,
                                tableName,
                                filters,
                                sort,
                                entityAuditor,
                                smsEnt,
                                isClientAuthorized,
                                cancellationToken
                            );
                        }
                        // get smses
                        return await SMSHelper.HandleSMSGetRequestAsync(
                            req,
                            configurationManagerService,
                            appHeader,
                            tableStorageService,
                            ASBService,
                            tableName,
                            filters,
                            sort,
                            entityAuditor,
                            smsEnt,
                            isClientAuthorized,
                            cancellationToken
                        );
                    }

                    // Handle Account Requests
                    if (bulkEntity is BulkEntity<Account> accEntity)
                    {
                        return await AccountHelper.HandleAccountGetRequestAsync(
                            req,
                            configurationManagerService,
                            appHeader,
                            tableStorageService,
                            ASBService,
                            tableName,
                            filters,
                            sort,
                            entityAuditor,
                            accEntity,
                            isClientAuthorized,
                            cancellationToken
                        );
                    }
                    // Handle Easy Notifications Requests
                    if (bulkEntity is BulkEntity<Notification> easyEntity)
                    {
                        var easyNotifications = (
                            await tableStorageService.GetAsync<Notification>(
                                tableName,
                                filters,
                                sort,
                                cancellationToken
                            )
                        )
                            .OrderBy(x => x.Status)
                            .ThenBy(x => DateTime.Parse(x.CreatedDate.ToString()).Date)
                            .ToList();

                        await req.CreateOkResponseAsync(
                            new
                            {
                                entity = easyNotifications
                                    .Skip(count: sort.PageIndex * sort.PageSize)
                                    .Take(sort.PageSize)
                                    .ToList(),
                                query = sort,
                                total = easyNotifications.Count,
                                filters,
                            },
                            cancellationToken: cancellationToken
                        );
                    }

                    // handle any other Get request
                    var entityResults = await tableStorageService.GetAsync<T>(
                        tableName,
                        filters,
                        sort,
                        cancellationToken
                    );
                    return await req.CreateOkResponseAsync(
                        new
                        {
                            entity = entityResults
                                .Skip(count: sort.PageIndex * sort.PageSize)
                                .Take(sort.PageSize)
                                .ToList(),
                            query = sort,
                            total = entityResults.Count,
                            filters,
                        },
                        cancellationToken: cancellationToken
                    );
                case nameof(HttpTriggerMethod.POST):
                case nameof(HttpTriggerMethod.PUT):

                    // check if the get request is for TokenDrawn
                    if (bulkEntity is BulkEntity<TokenAutoDrawn> tokenAutoDrawnEntity)
                    {
                        return await TokenHelper.HandleTokenAutoDrawnGetRequestAsync(
                            req,
                            configurationManagerService,
                            appHeader,
                            tableStorageService,
                            ASBService,
                            tableName,
                            filters,
                            sort,
                            entityAuditor,
                            tokenAutoDrawnEntity,
                            isClientAuthorized,
                            cancellationToken
                        );
                    }

                    // Create the RowKey if it's post
                    bulkEntity = Helper.CreateEntityRowKey(bulkEntity);

                    // handle SMSes being sent
                    if (bulkEntity is BulkEntity<SMS> smsEntity)
                    {
                        if (
                            req.Method.Equals(
                                nameof(HttpTriggerMethod.PUT),
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            throw new BadHttpRequestException("You can not update a Text message");
                        }
                        var result = await SMSHelper.HandleSMSTextSentAsync(
                            req,
                            configurationManagerService,
                            appHeader,
                            tableStorageService,
                            ASBService,
                            tableName,
                            filters,
                            sort,
                            entityAuditor,
                            smsEntity,
                            isClientAuthorized,
                            cancellationToken
                        );
                        return result;
                    }
                    if (bulkEntity is BulkEntity<Contact> contactEntity)
                    {
                        return await ContactHelper.HandleContactAsync(
                            req,
                            configurationManagerService,
                            appHeader,
                            tableStorageService,
                            ASBService,
                            tableName,
                            filters,
                            sort,
                            entityAuditor,
                            contactEntity,
                            isClientAuthorized,
                            cancellationToken
                        );
                    }

                    if (bulkEntity is BulkEntity<Account> accountEntity)
                    { // handle Api Key request
                        if (
                            tableName.Equals(
                                nameof(AppTableName.ApiKey),
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            return await APIKeyHelper.HandleApiKeyRequestAsync(
                                req,
                                configurationManagerService,
                                appHeader,
                                tableStorageService,
                                graphApiService,
                                ASBService,
                                tableName,
                                filters,
                                sort,
                                entityAuditor,
                                accountEntity,
                                isClientAuthorized,
                                cancellationToken
                            );
                        }
                        return await AccountHelper.HandleAccountAsync(
                            req,
                            configurationManagerService,
                            appHeader,
                            tableStorageService,
                            ASBService,
                            tableName,
                            filters,
                            sort,
                            entityAuditor,
                            accountEntity,
                            isClientAuthorized,
                            cancellationToken
                        );
                    }

                    // throw an error if trying to add replies or statistics to the table storage
                    if (bulkEntity is BulkEntity<Reply> || bulkEntity is BulkEntity<Statistics>)
                    {
                        throw new BadHttpRequestException(
                            "You can not make any changes to the entity"
                        );
                    }

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

                    // return results
                    return await req.CreateOkResponseAsync(entities, cancellationToken);

                case nameof(HttpTriggerMethod.DELETE):

                    // throw an error if trying to delete replies or statistics from table storage

                    if (
                        bulkEntity is BulkEntity<Reply>
                        || bulkEntity is BulkEntity<Notification>
                        || bulkEntity is BulkEntity<Statistics>
                    )
                    {
                        throw new BadHttpRequestException("You can not delete the entity  ");
                    }

                    if (
                       bulkEntity is BulkEntity<Group> groupEntity)
                    {

                        return await GroupHelper.HandleGroupDeleteAsync(
                          req,
                          configurationManagerService,
                          appHeader,
                          tableStorageService,
                          ASBService,
                          tableName,
                          filters,
                          sort,
                          entityAuditor,
                          groupEntity,
                          isClientAuthorized,
                          cancellationToken
                      );
                    }
                    return await req.CreateOkResponseAsync(
                        await BulkHelper.DeleteBulkEntities<T, T>(
                            req,
                            tableStorageService,
                            tableName,
                            filters,
                            sort,
                            bulkEntity,
                            ASBService,
                            cancellationToken
                        ),
                        cancellationToken
                    );
                default:
                    errorMessage = Error.FailedToProcessRequest(req.Method, tableName);
                    break;
            }

            throw new BadHttpRequestException(errorMessage);
        }
    }
}
