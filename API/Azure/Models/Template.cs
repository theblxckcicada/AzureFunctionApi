namespace EasySMS.API.Azure.Models
{
    public record Template : EntityBase
    {
        public string SMSMessage
        {
            set; get;
        }
        public string AccountRowKey
        {
            get; set;
        }
        public string AccountName
        {
            get; set;
        }
    }
}
