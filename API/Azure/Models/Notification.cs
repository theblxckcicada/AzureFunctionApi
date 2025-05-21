using System.Text.Json.Serialization;
using EasySMS.API.Functions.Converters;

namespace EasySMS.API.Azure.Models;

public enum NotificationStatus
{
    UNREAD,
    READ,
}

public record Notification : EntityBase
{
    public string Message
    {
        get; set;
    }
    public string AccountName
    {
        get; set;
    }
    public string AccountRowKey
    {
        get; set;
    }
    public string Route
    {
        get; set;
    }
    public string? InvoiceUrl
    {
        get; set;
    }

    [JsonConverter(converterType: typeof(EnumJsonConverter<NotificationStatus>))]
    public NotificationStatus Status
    {
        get; set;
    }
}
