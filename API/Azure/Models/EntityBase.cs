using Azure;
using Azure.Data.Tables;

namespace EasySMS.API.Azure.Models
{
    public abstract record AccountEntityBase : EntityBase
    {
        public string AccountRowKey
        {
            get; set;
        }
        public string AccountName
        {
            get; set;
        }
    }

    public abstract record EntityBase : ITableEntity
    {
        public virtual string PartitionKey
        {
            get; set;
        }
        public virtual string RowKey
        {
            get; set;
        }
        public string UserId
        {
            get; set;
        }
        DateTimeOffset? ITableEntity.Timestamp
        {
            get; set;
        }
        ETag ITableEntity.ETag
        {
            get; set;
        }
        public string CreatedBy
        {
            get; set;
        }
        public string ModifiedBy
        {
            get; set;
        }
        public DateTime CreatedDate
        {
            get; set;
        }
        public DateTime ModifiedDate
        {
            get; set;
        }
    }

    public record EasySMSUser
    {
        public string UserId
        {
            get; set;
        }
        public string FirstName
        {
            get; set;
        }
        public string LastName
        {
            get; set;
        }
        public string ContactNumber
        {
            get; set;
        }
        public string StreetAddress
        {
            get; set;
        }
        public string City
        {
            get; set;
        }
        public string Country
        {
            get; set;
        }
        public string ReplyToEmail
        {
            get; set;
        }
        public string EmailAddress
        {
            get; set;
        }
    }
}
