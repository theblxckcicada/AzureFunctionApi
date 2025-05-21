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

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class TokenHelper
    {
        [Obsolete]
        public static async Task HandleTokenReminderBackgroundJobAsync(IAzureTableStorageService tableStorageService, List<Account> accounts, CancellationToken cancellationToken)
        {
            // for every account query the background job and update it 
            List<Task> tasks = [];
            foreach (var account in accounts)
            {
                var job = (await tableStorageService.GetAsync<BackgroundJob>(nameof(BackgroundJob), [
                    new() { IsComparatorSupported = true, IsKeyQueryable = true, KeyName = nameof(Account.PartitionKey), Value = account.RowKey }], new()
                    {
                        PageIndex = 0,
                        PageSize = 10,
                        FilterValue = nameof(Account.RowKey)
                    }, cancellationToken)).FirstOrDefault();

                tasks.Add(BulkHelper.UpsertEntitiesAsync<BackgroundJob, BackgroundJob>(tableStorageService, [new BackgroundJob() {
                        JobName = job.JobName,
                        LastRunDateTime = job?.LastRunDateTime == default ? DateTime.UtcNow : DateTime.SpecifyKind(job.LastRunDateTime, DateTimeKind.Utc),
                        PartitionKey = account.RowKey,
                        RowKey = job?.RowKey??string.Empty,
                        Frequency = account.TokenReminderFrequency,
                        FrequencyValue  = account.TokenReminderFrequencyValue
                    }], nameof(BackgroundJob), account.RowKey, CancellationToken.None));


            }
            await Task.WhenAll(tasks);
        }


        [Obsolete]
        public static async Task<HttpResponseData> HandleTokenAutoDrawnGetRequestAsync<T>(
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
            where T : TokenAutoDrawn, new()
        {
            return await req.CreateOkResponseAsync(
                await GetTokenAutoDrawnAsync(
                    tableStorageService,
                    configurationManagerService,
                    appHeader,
                    bulkEntity,
                    cancellationToken
                ),
                cancellationToken: cancellationToken
            );
        }

        public static async Task<List<T>> GetTokenAutoDrawnAsync<T>(
            IAzureTableStorageService tableStorageService,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader,
            BulkEntity<T> bulkEntity,
            CancellationToken cancellationToken = default
        )
            where T : TokenAutoDrawn, new()
        {
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);

            // Declare variables
            Sort sort =
                new()
                {
                    PageIndex = 0,
                    PageSize = 10,
                    FilterValue = nameof(TokenAutoDrawn.RowKey)
                };

            List<Filter> filters = [];
            // Loop through the entities
            foreach (var tokenAutoDrawn in bulkEntity.Entities)
            {
                // declare primary account
                Account? primaryAccount = null;

                // Create filters to query the account
                filters.Add(
                    new()
                    {
                        KeyName = nameof(TokenAutoDrawn.RowKey),
                        Value = tokenAutoDrawn.AccountRowKey,
                        IsKeyQueryable = true,
                        IsComparatorSupported = true
                    }
                );
                // query the account
                var account =
                    (
                        await tableStorageService.GetAsync<Account>(
                            nameof(Account),
                            filters,
                            sort,
                            cancellationToken
                        )
                    ).FirstOrDefault()
                    ?? throw new BadHttpRequestException("Account Does not exist");

                // first check if the account is not a main account
                if (account.RowKey.Equals(account.ParentRowKey, StringComparison.OrdinalIgnoreCase))
                {
                    primaryAccount = account;
                }
                else
                {
                    // use the account to query the primary account
                    filters.Clear();
                    filters.Add(
                        new()
                        {
                            KeyName = nameof(TokenAutoDrawn.RowKey),
                            Value = account.ParentRowKey,
                            IsKeyQueryable = true,
                            IsComparatorSupported = true
                        }
                    );

                    primaryAccount = (
                        await tableStorageService.GetAsync<Account>(
                            nameof(Account),
                            filters,
                            sort,
                            cancellationToken
                        )
                    ).FirstOrDefault();
                }

                // check if the account can auto draw from the primary account
                decimal drawAmount = 0;
                drawAmount =
                    tokenAutoDrawn.TokenCreditNeeded - account.TokenDrawnDownAmount > 0
                        ? account.TokenDrawnDownAmount
                            * (tokenAutoDrawn.TokenCreditNeeded / account.TokenDrawnDownAmount)
                        : account.TokenDrawnDownAmount;

                // query the tokens
                filters.Clear();
                filters.Add(
                    new()
                    {
                        KeyName = nameof(TokenAutoDrawn.AccountRowKey),
                        IsKeyQueryable = true,
                        Value = primaryAccount.RowKey,
                        IsComparatorSupported = true
                    }
                );
                var token = (
                    await tableStorageService.GetAsync<Token>(
                        nameof(Token),
                        filters,
                        sort,
                        cancellationToken
                    )
                ).FirstOrDefault();

                if (token is null || token.Quantity < drawAmount)
                {
                    tokenAutoDrawn.ErrorMessage =
                        $"You only have enough tokens to send  {tokenAutoDrawn.AccountCredit} messages, please adjust your message or the number of contacts and try again";
                }
            }

            return bulkEntity.Entities;
        }
    }
}
