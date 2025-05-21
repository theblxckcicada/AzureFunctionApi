namespace EasySMS.API.Common.Models
{
    public enum Headers
    {
        Filter,
        BlobContainerName,
        TableName,
        DocumentFilter,
        EasySMSHttpTrigger,
        ApiKey,
        Id,
    }
    public enum HttpRoute
    {
        xero,
        xero_callback,
        xero_auth,
        auth,
        connect_mobile_dlr,
        connect_mobile_replies
    }

    public enum AppAuthorization
    {
        Bearer,
        Microsoft,
        Windows,
        roles,
        claims,
        ADMIN,
        aud,
        firstname,
        given_name,
        family_name,
        sub,
        city,
        extension_ContactNumber,
        extension_ReplyToEmail,
        emails,
        streetAddress,
        country,
        name,

    }

    public enum Sorting
    {
        filter,
        PageSize,
        PageIndex,
        sort,
    }

    public enum AppBlobContainerName
    {
        FleetMatch,
    }

    public enum AppTableName
    {
        Account,
        Contact,
        ContactField,
        Group,
        GroupContact,
        Template,
        Reply,
        SMS,
        Token,
        Order,
        User,
        ApiKey,
        Statistics,
        TokenAutoDrawn,
        Notification
    }

    public enum AzureTableStorageSystemProperty
    {
        PartitionKey,
        RowKey,
        ETag,
        Timestamp,
        ADMIN,
    }

    public enum EnvironmentVariable
    {
        TableStorageBaseUrl,
        TableStorageAccountName,
        TableStorageAccountKey,
        ClientAppAudience,
    }

    public enum DataSort
    {
        asc,
        desc,
    }

    public enum FilterOperator
    {
        and,
        or,
    }

    public enum FilterComparator
    {
        eq, // equals
        ne, // not equals
        gt, // greater than
        lt, // less than
        ge, // greater than equals to
        le // less than equals to
        ,
    }

    public enum FilterComparatorNotSupported
    {
        sw, // starts with
    }

    public enum HttpTriggerMethod
    {
        GET,
        POST,
        PUT,
        DELETE,
    }

    public enum Industry
    {
        InformationTechnology,
        Finance,
        Healthcare,
        Education,
        Retail,
        Manufacturing,
        RealEstate,
        Hospitality,
        Automotive,
        Agriculture,
        Construction,
        Entertainment,
        Energy,
        Telecommunications,
        Transportation,
        Aerospace,
        ConsumerElectronics,
        Pharmaceuticals,
        FoodAndBeverage,
        Gaming,
        Fashion,
        Sports,
        EnvironmentalServices,
        Advertising,
        LegalServices,
        Consulting,
        Government,
        NonProfit,
        Architecture,
        Mining,
    }
}
