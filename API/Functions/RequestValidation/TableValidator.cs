using EasySMS.API.Auth;
using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.BlobStorage;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.GraphApi;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Functions.Helpers;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using EasySMSV2.Shared.Messaging.ConnectMobile.Services;
using Microsoft.Azure.Functions.Worker.Http;
using Group = EasySMS.API.Azure.Models.Group;

namespace EasySMS.API.Functions.RequestValidation
{
    public static class TableValidator
    {
        private delegate Task<HttpResponseData> ProcessEntityDelegate<T>(
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader,
            IAzureTableStorageService tableStorageService,
            IGraphApiService graphApiService,
            IConnectMobileService connectMobileService,
            IASBService asbService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            EntityAuditor entityAuditor,
            bool isClientAuthorized,
            CancellationToken cancellationToken
        );

        [Obsolete]
        private static readonly Dictionary<
            AppTableName,
            ProcessEntityDelegate<object>
        > ProcessEntityMap =
            new()
            {
                { AppTableName.ApiKey, ProcessFunction.ProcessEntityAsync<Account> },
                { AppTableName.Account, ProcessFunction.ProcessEntityAsync<Account> },
                { AppTableName.Contact, ProcessFunction.ProcessEntityAsync<Contact> },
                { AppTableName.ContactField, ProcessFunction.ProcessEntityAsync<ContactField> },
                { AppTableName.Group, ProcessFunction.ProcessEntityAsync<Group> },
                { AppTableName.GroupContact, ProcessFunction.ProcessEntityAsync<GroupContact> },
                { AppTableName.Template, ProcessFunction.ProcessEntityAsync<Template> },
                { AppTableName.SMS, ProcessFunction.ProcessEntityAsync<SMS> },
                { AppTableName.Token, ProcessFunction.ProcessEntityAsync<Token> },
                { AppTableName.Order, ProcessFunction.ProcessEntityAsync<Order> },
                { AppTableName.Statistics, ProcessFunction.ProcessEntityAsync<Statistics> },
                { AppTableName.TokenAutoDrawn, ProcessFunction.ProcessEntityAsync<TokenAutoDrawn> },
                { AppTableName.Reply, ProcessFunction.ProcessEntityAsync<Reply> },
                {
                    AppTableName.Notification,
                    ProcessFunction.ProcessEntityAsync<Notification>
                },
            };

        [Obsolete]
        public static async Task<HttpResponseData> ValidateAsync(
            HttpRequestData req,
            IAzureTableStorageService tableStorageService,
            AppHeader appHeader,
            IAuthorizer authorizer,
            IConfigurationManagerService configurationManagerService,
            IGraphApiService graphApiService,
            IConnectMobileService connectMobileService,
            IAzureBlobStorageService blobStorageService,
            IASBService asbService,
            EntityAuditor entityAuditor,
            CancellationToken cancellationToken = default
        )
        {
            var tableName = appHeader.TableName;
            var sort = appHeader.Sort;
            var filters = appHeader.Filters;
            var blobName = appHeader.BlobName;
            var isClientAuthorized = false;

            if (Enum.TryParse(tableName, out AppTableName appTableNameEnums))
            {
                if (ProcessEntityMap.TryGetValue(appTableNameEnums, out var processEntity))
                {
                    return await processEntity(
                        req,
                        configurationManagerService,
                        appHeader,
                        tableStorageService,
                        graphApiService,
                        connectMobileService,
                        asbService,
                        tableName,
                        filters,
                        sort,
                        entityAuditor,
                        isClientAuthorized,
                        cancellationToken
                    );
                }
            }

            var errorMessage = Error.MessageNotSupported();
            return await req.CreateBadRequestResponseAsync(
                errorMessage,
                System.Net.HttpStatusCode.BadRequest,
                cancellationToken
            );
        }
    }
}
