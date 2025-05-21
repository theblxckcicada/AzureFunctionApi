using Azure.Data.Tables;
using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Functions.Helpers;

namespace EasySMS.API.Common;

public static class IntegrationHelper
{
    private static readonly string Account = "accounts";
    private static readonly string Contact = "contacts";
    private static readonly string ContactField = "customize";
    private static readonly string Group = "groups";
    private static readonly string Token = "tokens";
    private static readonly string Order = "orders";
    private static readonly string SMS = "sms";
    private static readonly string Template = "templates";




    [Obsolete]
    public static async Task QueueUpsertTasks<T>(
                  IEnumerable<T> entities,
                  IAzureTableStorageService tableStorageService,

                    CancellationToken cancellationToken = default
                  ) where T : class, ITableEntity, new()
    {
        List<Task> tasks = [];
        foreach (var group in entities.GroupBy(e => e.PartitionKey))
        {
            tasks.Add(
                BulkHelper.UpsertEntitiesAsync<T, T>(
                    tableStorageService,
                    [.. group],
                    typeof(T).Name,
                    group.Key,
                    cancellationToken
                )
            );
        }

        await Task.WhenAll(tasks);
    }

    [Obsolete]
    public static async Task HandleSentMessages(IAzureTableStorageService tableStorageService, IEnumerable<SMS> entities, CancellationToken cancellationToken = default
                  )
    {
        List<Task> tasks = [];
        foreach (var entity in entities)
        {
            tasks.Add(LogHelper.CreateLogAsync(tableStorageService, $"Message for {entity.ContactName} {entity.ContactNumber} is delivered", "Connect Mobile Service", false, cancellationToken));
        }
        await Task.WhenAll(tasks);

    }

    [Obsolete]
    public static async Task QueueDeleteTasks<T>(
                  IEnumerable<T> entities,
                  IAzureTableStorageService tableStorageService,

                    CancellationToken cancellationToken = default
                  ) where T : class, ITableEntity, new()
    {
        List<Task> tasks = [];
        foreach (var group in entities.GroupBy(e => e.PartitionKey))
        {
            tasks.Add(
               BulkHelper.DeleteBulkEntities<T, T>(null, tableStorageService, typeof(T).Name, [new() { IsComparatorSupported = true, IsKeyQueryable = true, KeyName = nameof(EntityBase.PartitionKey), Value = group.Key, }], new()
               {
                   PageIndex = 0,
                   PageSize = 10,
                   FilterValue = nameof(EntityBase.PartitionKey),
               }, new()
               {
                   Entities = [.. entities]
               }
                , cancellationToken: cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    [Obsolete]
    public static async Task SendNotificationsAsync(IAzureTableStorageService tableStorageService, List<Notification> notifications, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];
        var groupedNotifications = notifications
             .Where(notification => !string.IsNullOrEmpty(notification.Message))
             .GroupBy(notification => notification.PartitionKey);
        foreach (var grouped in groupedNotifications)
        {
            // update the accounts
            tasks.Add(
                BulkHelper.UpsertEntitiesAsync<Notification, Notification>(
                    tableStorageService,
                    [.. grouped],
                    nameof(Notification),
                    grouped.FirstOrDefault()!.PartitionKey,
                    cancellationToken
                )
            );
        }

        await Task.WhenAll(tasks);
    }

    [Obsolete]
    public static async Task HandleSendMessageError(IAzureTableStorageService tableStorageService, List<SMS> messages,
        List<Notification> notifications, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];

        // set the message 
        foreach (var message in messages)
        {
            message.Status = MessageStatus.Error;
            message.ModifiedBy = nameof(BackgroundJob);
            message.ModifiedDate = DateTime.UtcNow;

            // add the notification message 
            notifications.Add(new()
            {
                AccountName = message.AccountName,
                AccountRowKey = message.AccountRowKey,
                Message = $"Failed to send message: {message}",
                Route = SMS,
                PartitionKey = message.PartitionKey,
                Status = NotificationStatus.UNREAD,
                UserId = message.UserId,
                CreatedBy = "Easy SMS  Service",
                ModifiedBy = "Easy SMS  Service",
                CreatedDate = new DateTime(),
                ModifiedDate = new DateTime()

            });
            tasks.Add(LogHelper.CreateLogAsync(tableStorageService,
                    $"Error sending message with ID: {message.RowKey} - {errorMessage} ",
                    "Connect Mobile Helper",
                    true,
            cancellationToken
        ));
        }

        // send to table storage 
        tasks = [.. tasks, QueueUpsertTasks(messages, tableStorageService, cancellationToken)];
        tasks = [.. tasks, SendNotificationsAsync(tableStorageService, notifications, cancellationToken)];

        await Task.WhenAll(tasks);
    }
    public static void InsufficientAutoDrawCredit(
        Account account,
        List<Notification> notifications
    )
    {
        var errorMessage =
            $"Primary account '{account.Name}' has insufficient token credits to auto-credit sub-accounts. Please purchase more credits.";
        notifications.Add(
            new()
            {
                AccountName = account.Name,
                AccountRowKey = account.RowKey,
                Message = errorMessage,
                Route = Account,
                PartitionKey = account.PartitionKey,
                Status = NotificationStatus.UNREAD,
                UserId = account.UserId,
                CreatedBy = "Easy SMS  Service",
                ModifiedBy = "Easy SMS  Service",
                CreatedDate = new DateTime(),
                ModifiedDate = new DateTime()
            }
        );
    }

    public static void SuccessAutoAccountCredit(
        Account parentAccount,
        Account account,
        List<Notification> notifications
    )
    {
        var message =
            $"Primary account '{parentAccount.Name}' automatically credited {account.TokenDrawnDownAmount} tokens to '{account.Name}'.";

        notifications.Add(
            new()
            {
                AccountName = account.Name,
                AccountRowKey = account.RowKey,
                Message = message,
                Route = Account,
                PartitionKey = account.PartitionKey,
                Status = NotificationStatus.UNREAD,
                UserId = account.UserId,
                CreatedBy = "Easy SMS  Service",
                ModifiedBy = "Easy SMS  Service",
                CreatedDate = new DateTime(),
                ModifiedDate = new DateTime()
            }
        );
    }

    public static void InvoicePaidSuccess(Order order, List<Notification> notifications)
    {
        var message =
            $" Order Paid: Account '{order.AccountName}' has been successfully credited with {order.Quantity} tokens.";
        notifications.Add(
            new()
            {
                AccountName = order.AccountName,
                AccountRowKey = order.AccountRowKey,
                Message = message,
                Route = Order,
                PartitionKey = order.PartitionKey,
                Status = NotificationStatus.UNREAD,
                UserId = order.UserId,
                CreatedBy = "Easy SMS  Service",
                ModifiedBy = "Easy SMS  Service",
                CreatedDate = new DateTime(),
                ModifiedDate = new DateTime()
            }
        );
    }

    public static void OrderInvoiceCreated(Order order, List<Notification> notifications)
    {
        var message =
            $"Invoice Pending: An invoice for {order.Quantity} tokens has been created for account '{order.AccountName}' and is awaiting payment.";
        notifications.Add(
            new()
            {
                AccountName = order.AccountName,
                AccountRowKey = order.AccountRowKey,
                Message = message,
                Route = Order,
                InvoiceUrl = order.InvoiceUrl,
                PartitionKey = order.PartitionKey,
                Status = NotificationStatus.UNREAD,
                UserId = order.UserId,
                CreatedBy = "Easy SMS  Service",
                ModifiedBy = "Easy SMS  Service",
                CreatedDate = new DateTime(),
                ModifiedDate = new DateTime()
            }
        );
    }

    public static void AccountMessageHistoryCleaned(Account account, int count, List<Notification> notifications)
    {
        var message = $"Account '{account.Name}' message history cleaned up ({count}) deleted ";
        notifications.Add(
        new()
        {
            AccountName = account.Name,
            AccountRowKey = account.RowKey,
            Message = message,
            Route = SMS,
            PartitionKey = account.PartitionKey,
            Status = NotificationStatus.UNREAD,
            UserId = account.UserId,
            CreatedBy = "Easy SMS  Service",
            ModifiedBy = "Easy SMS  Service",
            CreatedDate = new DateTime(),
            ModifiedDate = new DateTime()
        }
    );
    }

    public static void AccountMonthlySummaryEmailSent(Account account, List<Notification> notifications)
    {
        var message = $"Monthly Messaging Report for Account '{account.Name}' has been sent via email";
        notifications.Add(
        new()
        {
            AccountName = account.Name,
            AccountRowKey = account.RowKey,
            Message = message,
            Route = Account,
            PartitionKey = account.PartitionKey,
            Status = NotificationStatus.UNREAD,
            UserId = account.UserId,
            CreatedBy = "Easy SMS  Service",
            ModifiedBy = "Easy SMS  Service",
            CreatedDate = new DateTime(),
            ModifiedDate = new DateTime()
        }
    );
    }
}
