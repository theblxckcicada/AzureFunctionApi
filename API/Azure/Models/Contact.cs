using System.Text.Json.Serialization;
using EasySMS.API.Functions.Converters;

namespace EasySMS.API.Azure.Models
{
    public record Contact : EntityBase
    {
        public string MobileNumber
        {
            set; get;
        }
        public string CountryCode
        {
            set; get;
        }
        public string Title
        {
            set; get;
        }
        public string FirstName
        {
            set; get;
        }
        public string LastName
        {
            set; get;
        }
        public string MiddleName
        {
            set; get;
        }
        public string AccountName
        {
            get; set;
        }
        public string AccountRowKey
        {
            get; set;
        }

        [JsonConverter(typeof(EnumJsonConverter<OptedOut>))]
        public OptedOut OptedOut
        {
            set; get;
        }
        public string? Ext1
        {
            get; set;
        }
        public string? Ext2
        {
            get; set;
        }
        public string? Ext3
        {
            get; set;
        }
        public string? Ext4
        {
            get; set;
        }
        public string? Ext5
        {
            get; set;
        }
        public string? Ext6
        {
            get; set;
        }
        public string? Ext7
        {
            get; set;
        }
        public string? Ext8
        {
            get; set;
        }
        public string? Ext9
        {
            get; set;
        }
        public string? Ext10
        {
            get; set;
        }
        public string? Ext11
        {
            get; set;
        }
        public string? Ext12
        {
            get; set;
        }
        public string? Ext13
        {
            get; set;
        }
        public string? Ext14
        {
            get; set;
        }
        public string? Ext15
        {
            get; set;
        }
    }

    public enum OptedOut
    {
        No,
        Yes,
    }
}
