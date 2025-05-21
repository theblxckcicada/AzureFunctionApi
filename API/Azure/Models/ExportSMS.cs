namespace EasySMS.API.Azure.Models
{
    public record ExportSMS : EntityBase
    {
        public string ContactNumber
        {
            set; get;
        }
        public string ContactName
        {
            set; get;
        }
        public string ContactRowKey
        {
            set; get;
        }
        public string GroupRowKey
        {
            set; get;
        }
        public string GroupName
        {
            set; get;
        }
        public string Message
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
        public MessageStatus Status
        {
            get; set;
        }
        public string ScheduledDateTime
        {
            set; get;
        }
        public string CreatedDate
        {
            set; get;
        }
        public string ModifiedDate
        {
            set; get;
        }
    }
}
