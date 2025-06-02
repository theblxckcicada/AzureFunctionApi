using Azure;
using Azure.Data.Tables;
using System.ComponentModel;

namespace DMIX.API.Models
{
    public abstract record AccountEntityBase<TKey> : EntityBase<TKey>
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



    public abstract record EntityBase<TKey> : ITableEntity

    {
        public virtual string PartitionKey
        {
            get; set;
        }

        // Internal backing property for typed key
        public virtual TKey Key { get; set; } = default!;

        // ITableEntity requires string RowKey, so we map to/from Key
        public virtual string RowKey
        {
            get => Key?.ToString() ?? string.Empty;
            set => Key = ConvertFromString(value);
        }

        public virtual DateTimeOffset? Timestamp
        {
            get; set;
        }
        public virtual ETag ETag
        {
            get; set;
        }

        // You can override this if TKey is not a string or Guid etc.

        protected virtual TKey ConvertFromString(string value)
        {
            var converter = TypeDescriptor.GetConverter(typeof(TKey));
            return (TKey)converter.ConvertFromInvariantString(value);
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

        public string EmailAddress
        {
            get; set;
        }
        public string? Business
        {
            get; set;
        }
        public UserRole UserRole
        {
            get; set;
        }
    }
    public enum UserRole
    {
        Customer,
        Owner,
        Staff,
        HairStylist,
        Barber,
        NailTechnician,
        SpaTherapist
    }

}
