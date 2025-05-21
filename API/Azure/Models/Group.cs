namespace EasySMS.API.Azure.Models
{
    public record Group : EntityBase
    {
        public string AccountName
        {
            get; set;
        }
        public string AccountRowKey
        {
            get; set;
        }
        public string Name
        {
            set; get;
        }
        public Contact[]? Contacts
        {
            set; get;
        }
    }

    public record GroupContact : EntityBase
    {
        public string ContactRowKey
        {
            set; get;
        }
    }
}
