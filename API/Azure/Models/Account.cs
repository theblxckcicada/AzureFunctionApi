using System.Text.Json.Serialization;
using DMIX.API.Common;
using DMIX.API.Models;

namespace DMIX.API.Azure.Models;

public enum OTPTemplate
{
    Default,
}

public enum TokenReminder
{
    All,
    Email,
    SMS,
    None,
}

public enum AccountType
{
    Main,
    User,
    Integration,
}

public enum AccountStatus
{
    Active,
    InActive,
}

public enum IBinary
{
    No,
    Yes,
}
public enum Frequency
{
    Minutes, Hours,
    Days
}

public record Account : EntityBase<Guid>
{
    public string ParentRowKey
    {
        get; set;
    }
    public string Name
    {
        get; set;
    }
    public string EmailAddress
    {
        get; set;
    }
    public string Country
    {
        get; set;
    }

    public int Code
    {
        get; set;
    }

    [JsonConverter(typeof(EnumJsonConverter<AccountType>))]
    public AccountType Type
    {
        get; set;
    }
    public string IntegrationExternalId
    {
        get; set;
    }
    public string IntegrationSecret
    {
        get; set;
    }
    public int Version
    {
        get; set;
    }
    public string XeroContactId
    {
        get; set;
    }

    // Main details
    public string BusinessName
    {
        get; set;
    }
    public string BusinessRegistration
    {
        get; set;
    }
    public string BusinessTAXNumber
    {
        get; set;
    }
    public string BusinessAddress
    {
        get; set;
    }
    public string ContactName
    {
        get; set;
    }
    public string ContactEmail
    {
        get; set;
    }
    public int KeepMessageHistoryMonthCount
    {
        get; set;
    }

    [JsonConverter(typeof(EnumJsonConverter<AccountStatus>))]
    public AccountStatus Status
    {
        get; set;
    }

    [JsonConverter(typeof(EnumJsonConverter<IBinary>))]
    public IBinary TokenAutoDrawDown
    {
        get; set;
    }
    public int TokenDrawnDownAmount
    {
        get; set;
    }
    public decimal TokenPrice
    {
        get; set;
    }
    public decimal TaxRate
    {
        get; set;
    }

    // Notifications
    public string NotificationEmail
    {
        get; set;
    }

    [JsonConverter(typeof(EnumJsonConverter<IBinary>))]
    public IBinary EmailMonthlyStatement
    {
        get; set;
    }

    [JsonConverter(typeof(EnumJsonConverter<TokenReminder>))]
    public TokenReminder TokenTopUpReminder
    {
        get; set;
    }

    [JsonConverter(typeof(EnumJsonConverter<Frequency>))]
    public Frequency TokenReminderFrequency { get; set; } = Frequency.Hours;
    public int TokenReminderFrequencyValue { get; set; } = 4;
    public int TokenTopUp
    {
        get; set;
    }
    public int OTPLength
    {
        get; set;
    }
    public int OTPExpiry
    {
        get; set;
    }
    public string OTPTemplate
    {
        get; set;
    }

    public string? ErrorMessage
    {
        get; set;
    }


}
