using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common;
using EasySMS.API.Common.Models;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class OrderHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleOrderRequestAsync<T>(
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
            where T : Order, new()
        {
            return await req.CreateOkResponseAsync(cancellationToken);
        }

        [Obsolete]
        public static async Task HandleOrderAsync<T>(
            IAzureTableStorageService tableStorageService,
            IConfigurationManagerService configurationManagerService,
            Sort sort,
            List<Filter> baseFilters,
            AppHeader appHeader,
            EntityAuditor entityAuditor,
            BulkEntity<T> bulkEntity,
            bool isClientAuthorized = false,
            CancellationToken cancellationToken = default
        )
            where T : SMS, new()
        {
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);

            // Declare variables
            sort = new()
            {
                PageIndex = 0,
                PageSize = 10,
                FilterValue = nameof(AccountEntityBase.RowKey)
            };

            List<Filter> filters = [];
            List<Task> tasks = [];
            // group entities by Account Row key
            var groupedEntities = bulkEntity.Entities.GroupBy(entity => entity.AccountRowKey);
            // Loop through the entities
            foreach (var entities in groupedEntities)
            {
                if (entities.ToList() is List<T> groupedEntity)
                { // variable
                    var entity = entities.FirstOrDefault();
                    // declare primary account
                    Account? primaryAccount = null;

                    // Create filters to query the account
                    filters.Add(
                        new()
                        {
                            KeyName = nameof(entity.RowKey),
                            Value =
                                entity.AccountRowKey
                                ?? baseFilters
                                    .FirstOrDefault(filter =>
                                        filter.KeyName.Equals(
                                            nameof(SMS.AccountRowKey),
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                    )
                                    ?.Value
                                ?? baseFilters
                                    .FirstOrDefault(filter =>
                                        filter.KeyName.Equals(
                                            nameof(Account.PartitionKey),
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                    )
                                    ?.Value
                                ?? string.Empty,
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

                    //
                    foreach (var ent in groupedEntity)
                    {
                        ent.AccountName = account.Name;
                        ent.AccountRowKey = account.RowKey;
                    }
                    // query the account tokens
                    filters.Clear();
                    filters.Add(
                        new()
                        {
                            KeyName = nameof(entity.AccountRowKey),
                            IsKeyQueryable = true,
                            Value = account.RowKey,
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
                    // first check if the account is not a main account
                    if (
                        account.RowKey.Equals(
                            account.ParentRowKey,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        primaryAccount = account;
                    }
                    else if (
                        account.TokenAutoDrawDown == IBinary.Yes
                        && account.TokenDrawnDownAmount > 0
                    )
                    {
                        // use the account to query the primary account
                        filters.Clear();
                        filters.Add(
                            new()
                            {
                                KeyName = nameof(entity.RowKey),
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
                    else
                    {
                        throw new BadHttpRequestException(
                            $"You only have enough tokens to send {token?.Quantity ?? 0} messages"
                        );
                    }

                    // Get Local South African and International SMS texts being sent
                    var groupedLocalSMSTexts = groupedEntity
                        .Where(e => StringExtensions.IsSouthAfricanNumber(e.ContactNumber))
                        .ToList();

                    var groupedInternationalSMSTexts = groupedEntity
                        .Where(e => !StringExtensions.IsSouthAfricanNumber(e.ContactNumber))
                        .ToList();

                    // determine token costs
                    var localTokenCost = groupedLocalSMSTexts.Count * 1;
                    var internationalTokenCost =
                        groupedInternationalSMSTexts.Count * Helper.InternationalMessageCost;
                    var totalTokens = localTokenCost + internationalTokenCost;

                    Token? primaryToken = null;
                    var drawAmount = 0;
                    BulkEntity<Order> orders = new()
                    {
                        Entities = []
                    };
                    BulkEntity<Token> tokens = new()
                    {
                        Entities = []
                    };
                    Order? order = null;
                    if (
                        (token?.Quantity ?? 0) < totalTokens
                        && !primaryAccount.RowKey.Equals(account.RowKey, StringComparison.Ordinal)
                    )
                    {
                        // query the primary account tokens
                        primaryToken = (
                            await tableStorageService.GetAsync<Token>(
                                nameof(Token),
                                [
                                    new()
                                    {
                                        KeyName = nameof(entity.AccountRowKey),
                                        IsKeyQueryable = true,
                                        Value = primaryAccount.RowKey,
                                        IsComparatorSupported = true
                                    }
                                ],
                                sort,
                                cancellationToken
                            )
                        ).FirstOrDefault();

                        // check how many tokens you need

                        var neededTokenCredit = totalTokens - (token?.Quantity ?? 0);
                        var autoDrawDownAmount = account.TokenDrawnDownAmount; // Assuming this is 200

                        if (neededTokenCredit > 0)
                        {
                            var drawCount = (int)
                                Math.Ceiling((double)neededTokenCredit / autoDrawDownAmount);
                            drawAmount = drawCount * autoDrawDownAmount;
                        }
                        else
                        {
                            drawAmount = 0; // No draw needed if no credit is required
                        }

                        if (primaryToken is null || primaryToken.Quantity < drawAmount)
                        {
                            throw new BadHttpRequestException(
                                $"You only have enough tokens to send {token?.Quantity ?? primaryToken?.Quantity ?? 0} messages"
                            );
                        }

                        // create an order auto draw from the primary account
                        order = new()
                        {
                            AccountName = account.Name,
                            AccountRowKey = account.RowKey,
                            PricePerToken = account.TokenPrice,
                            Status = OrderStatus.AutoDrawnDown,
                            Quantity = drawAmount,
                            TotalAmount = drawAmount * account.TokenPrice,
                            PartitionKey = user.UserId
                        };
                        Order order1 =
                            new()
                            {
                                AccountName = account.Name,
                                AccountRowKey = account.RowKey,
                                PricePerToken = account.TokenPrice,
                                Status = OrderStatus.TokenUsed,
                                Quantity = totalTokens,
                                TotalAmount = totalTokens * account.TokenPrice,
                                PartitionKey = user.UserId
                            };

                        orders = new()
                        {
                            Entities = [order, order1]
                        };
                        tasks.Add(
                            BulkHelper.UpsertBulkEntities<Order, Order>(
                                tableStorageService,
                                entityAuditor,
                                appHeader,
                                nameof(Order),
                                [
                                    new()
                                    {
                                        KeyName = nameof(entity.PartitionKey),
                                        IsKeyQueryable = true,
                                        Value = user.UserId,
                                        IsComparatorSupported = true
                                    }
                                ],
                                sort,
                                orders,
                                isClientAuthorized,
                                cancellationToken: cancellationToken
                            )
                        );

                        // add the auto drawn amount to the token
                        token ??= new()
                        {
                            AccountName = account.Name,
                            AccountRowKey = account.RowKey,
                            UserId = user.UserId,
                            Quantity = 0,
                            ExpiryDate = DateTime.Now.AddYears(1),
                            PartitionKey = user.UserId
                        };
                        token.Quantity += drawAmount;
                        tokens = new()
                        {
                            Entities = [token]
                        };
                        tasks.Add(
                            BulkHelper.UpsertBulkEntities<Token, Token>(
                                tableStorageService,
                                entityAuditor,
                                appHeader,
                                nameof(Token),
                                [
                                    new()
                                    {
                                        KeyName = nameof(entity.PartitionKey),
                                        IsKeyQueryable = true,
                                        Value = user.UserId,
                                        IsComparatorSupported = true
                                    }
                                ],
                                sort,
                                tokens,
                                isClientAuthorized,
                                cancellationToken: cancellationToken
                            )
                        );

                        // take out the total token quantity
                        token.Quantity -= totalTokens;
                        tokens = new()
                        {
                            Entities = [token]
                        };
                        tasks.Add(
                            BulkHelper.UpsertBulkEntities<Token, Token>(
                                tableStorageService,
                                entityAuditor,
                                appHeader,
                                nameof(Token),
                                [
                                    new()
                                    {
                                        KeyName = nameof(entity.PartitionKey),
                                        IsKeyQueryable = true,
                                        Value = user.UserId,
                                        IsComparatorSupported = true
                                    }
                                ],
                                sort,
                                tokens,
                                isClientAuthorized,
                                cancellationToken: cancellationToken
                            )
                        );

                        if (!account.RowKey.Equals(primaryAccount.RowKey, StringComparison.Ordinal))
                        {
                            Order primaryOrder =
                                new()
                                {
                                    AccountName = primaryAccount.Name,
                                    AccountRowKey = primaryAccount.RowKey,
                                    PricePerToken = primaryAccount.TokenPrice,
                                    Status = OrderStatus.AutoDrawnDown,
                                    Quantity = drawAmount,
                                    TotalAmount = drawAmount,
                                    PartitionKey = user.UserId
                                };
                            orders = new()
                            {
                                Entities = [primaryOrder]
                            };
                            tasks.Add(
                                BulkHelper.UpsertBulkEntities<Order, Order>(
                                    tableStorageService,
                                    entityAuditor,
                                    appHeader,
                                    nameof(Order),
                                    [
                                        new()
                                        {
                                            KeyName = nameof(entity.PartitionKey),
                                            IsKeyQueryable = true,
                                            Value = user.UserId,
                                            IsComparatorSupported = true
                                        }
                                    ],
                                    sort,
                                    orders,
                                    isClientAuthorized,
                                    cancellationToken: cancellationToken
                                )
                            );
                            primaryToken.Quantity -= drawAmount;

                            tokens = new()
                            {
                                Entities = [primaryToken]
                            };
                            tasks.Add(
                                BulkHelper.UpsertBulkEntities<Token, Token>(
                                    tableStorageService,
                                    entityAuditor,
                                    appHeader,
                                    nameof(Token),
                                    [
                                        new()
                                        {
                                            KeyName = nameof(entity.PartitionKey),
                                            IsKeyQueryable = true,
                                            Value = user.UserId,
                                            IsComparatorSupported = true
                                        }
                                    ],
                                    sort,
                                    tokens,
                                    isClientAuthorized,
                                    cancellationToken: cancellationToken
                                )
                            );
                        }
                    }
                    else
                    {
                        order = new()
                        {
                            AccountName = account.Name,
                            AccountRowKey = account.RowKey,
                            PricePerToken = account.TokenPrice,
                            Status = OrderStatus.TokenUsed,
                            Quantity = totalTokens,
                            TotalAmount = totalTokens * account.TokenPrice,
                            PartitionKey = user.UserId
                        };
                        orders = new()
                        {
                            Entities = [order]
                        };
                        tasks.Add(
                            BulkHelper.UpsertBulkEntities<Order, Order>(
                                tableStorageService,
                                entityAuditor,
                                appHeader,
                                nameof(Order),
                                [
                                    new()
                                    {
                                        KeyName = nameof(entity.PartitionKey),
                                        IsKeyQueryable = true,
                                        Value = user.UserId,
                                        IsComparatorSupported = true
                                    }
                                ],
                                sort,
                                orders,
                                isClientAuthorized,
                                cancellationToken: cancellationToken
                            )
                        );

                        token.Quantity -= totalTokens;
                        tokens = new()
                        {
                            Entities = [token]
                        };
                        tasks.Add(
                            BulkHelper.UpsertBulkEntities<Token, Token>(
                                tableStorageService,
                                entityAuditor,
                                appHeader,
                                nameof(Token),
                                [
                                    new()
                                    {
                                        KeyName = nameof(entity.PartitionKey),
                                        IsKeyQueryable = true,
                                        Value = user.UserId,
                                        IsComparatorSupported = true
                                    }
                                ],
                                sort,
                                tokens,
                                isClientAuthorized,
                                cancellationToken: cancellationToken
                            )
                        );
                    }
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}
