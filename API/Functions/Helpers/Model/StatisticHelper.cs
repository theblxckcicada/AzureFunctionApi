using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common;
using EasySMS.API.Common.Models;
using EasySMS.API.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class StatisticHelper
    {

        [Obsolete]
        public static async Task<HttpResponseData> HandleStatisticGetRequestAsync<T>(
            HttpRequestData req,
            IAzureTableStorageService tableStorageService,
            string tableName,
            List<Filter> filters,
            Sort sort,

            CancellationToken cancellationToken = default
        )
            where T : Statistics, new()
        {

            return await req.CreateOkResponseAsync(new
            {
                entity = await GetStatisticsAsync<T>(tableStorageService, tableName, filters, sort, cancellationToken: cancellationToken)
            },
            cancellationToken);
        }

        [Obsolete]
        public static async Task<List<Statistics>> GetStatisticsAsync<T>(
            IAzureTableStorageService tableStorageService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            DateTime? statementDate = null,
            CancellationToken cancellationToken = default
        )
            where T : Statistics, new()
        {
            // Initialize the Statistics
            List<Statistics> results = [];
            Statistics statistics =
                new()
                {
                    SentMessageMonthStatistics = [],
                    PendingMessageMonthStatistics = [],
                    PurchasedTokens = [],
                    AccountRowKey = "",
                    AccountName = ""
                };

            sort = new()
            {
                PageIndex = 0,
                PageSize = 10,
                FilterValue = nameof(Statistics.RowKey)
            };
            // Query SMSes from table storage
            var smses = await tableStorageService.GetAsync<SMS>(
                nameof(SMS),
                filters,
                sort,
                cancellationToken
            );
            if (statementDate is not null)
            {
                var now = DateTime.Now;
                var previousMonth = now.AddMonths(-1);

                var filteredSmses = smses
                    .Where(x =>
                    {
                        var date = DateTime.Parse(x.ScheduledDateTime.ToString());
                        return date.Month == previousMonth.Month && date.Year == previousMonth.Year;
                    })
                    .ToList();
                smses = [.. filteredSmses];
            }


            // query account
            var accountRowKeyFilter = filters
                .FirstOrDefault(filter =>
                    filter.KeyName.Equals(
                        nameof(SMS.AccountRowKey),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                ?.Value;
            var account = (
                await tableStorageService.GetAsync<Account>(
                    nameof(Account),
                    [
                        new()
                        {
                            KeyName = nameof(Account.PartitionKey),
                            Value = filters
                                .FirstOrDefault(filter =>
                                    filter.KeyName.Equals(
                                        nameof(Account.PartitionKey),
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                ?.Value,
                            IsComparatorSupported = true,
                            IsKeyQueryable = true
                        }
                    ],
                    sort,
                    cancellationToken
                )
            ).FirstOrDefault(acc =>
                string.IsNullOrEmpty(accountRowKeyFilter)
                    ? acc.Type == AccountType.Main
                    : acc.RowKey.Equals(accountRowKeyFilter, StringComparison.OrdinalIgnoreCase)
            );

            //  Get Sent Messages
            smses =
            [
                .. smses.Where(x =>
                    x.AccountRowKey.Equals(account.RowKey, StringComparison.OrdinalIgnoreCase)
                )
            ];
            var sentMessages = smses.Where(x => x.Status == MessageStatus.SENT);
            var pendingMessages = smses.Where(x => x.Status == MessageStatus.PENDING);

            // group smses by month
            var groupedMonthSMSes = sentMessages.GroupBy(sms => new
            {
                sms.ScheduledDateTime.Year,
                sms.ScheduledDateTime.Month
            });
            // Handle Sent messages
            foreach (var groupedMonthSMS in groupedMonthSMSes)
            {
                var localMessages = groupedMonthSMS
                    .Where(sms => StringExtensions.IsSouthAfricanNumber(sms.ContactNumber))
                    .ToList();
                var internationalMessages = groupedMonthSMS
                    .Where(sms => !StringExtensions.IsSouthAfricanNumber(sms.ContactNumber))
                    .ToList();

                // get the Cost
                var date = DateOnly.FromDateTime(
                    groupedMonthSMS.FirstOrDefault().ScheduledDateTime
                );
                statistics.SentMessageMonthStatistics =
                [
                    .. statistics.SentMessageMonthStatistics,
                    (
                        new()
                        {
                            Cost =
                                (
                                    internationalMessages.Count * Helper.InternationalMessageCost
                                    + localMessages.Count
                                ) * account.TokenPrice,
                            Date = date,
                            LocalMessagesNo = localMessages.Count,
                            InternationalMessagesNo = internationalMessages.Count,
                        }
                    )
                ];
                statistics.SentMessageMonthStatistics =
                [
                    .. statistics.SentMessageMonthStatistics.OrderByDescending(x => x.Date)
                ];
            }

            // Handle pending messages
            groupedMonthSMSes = pendingMessages.GroupBy(sms => new
            {
                sms.ScheduledDateTime.Year,
                sms.ScheduledDateTime.Month
            });
            foreach (var groupedMonthSMS in groupedMonthSMSes)
            {
                var localMessages = groupedMonthSMS
                    .Where(sms => StringExtensions.IsSouthAfricanNumber(sms.ContactNumber))
                    .ToList();
                var internationalMessages = groupedMonthSMS
                    .Where(sms => !StringExtensions.IsSouthAfricanNumber(sms.ContactNumber))
                    .ToList();

                // get the Cost
                var date = DateOnly.FromDateTime(
                    groupedMonthSMS.FirstOrDefault().ScheduledDateTime
                );
                statistics.PendingMessageMonthStatistics =
                [
                    .. statistics.PendingMessageMonthStatistics,
                    (
                        new()
                        {
                            Cost =
                                (
                                    internationalMessages.Count * Helper.InternationalMessageCost
                                    + localMessages.Count
                                ) * account.TokenPrice,
                            Date = date,
                            LocalMessagesNo = localMessages.Count,
                            InternationalMessagesNo = internationalMessages.Count,
                        }
                    )
                ];
                statistics.PendingMessageMonthStatistics =
                [
                    .. statistics.PendingMessageMonthStatistics.OrderByDescending(x => x.Date)
                ];
            }

            // Query Tokens
            var orders = (
                await tableStorageService.GetAsync<Order>(
                    nameof(Order),
                    filters,
                    sort,
                    cancellationToken
                )
            )
                .Where(order => order.Status == OrderStatus.Paid)
                .ToList();

            // group by Month
            var groupedOrders = orders.GroupBy(order => new
            {
                order.CreatedDate.Year,
                order.CreatedDate.Month
            });

            // handle tokens
            foreach (var groupedOrder in groupedOrders)
            {
                var date = DateOnly.FromDateTime(groupedOrder.FirstOrDefault().CreatedDate);
                var totalTokens = groupedOrder.Sum(order => order.Quantity);
                statistics.PurchasedTokens =
                [
                    .. statistics.PurchasedTokens,
                    new() { Date = date, Quantity = totalTokens }
                ];
            }

            // update Account Name and Account RowKey
            statistics.AccountRowKey = smses.FirstOrDefault()?.AccountRowKey;
            statistics.AccountName = smses.FirstOrDefault()?.AccountName;
            results.Add(statistics);
            return results;
        }
    }
}
