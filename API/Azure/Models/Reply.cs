namespace EasySMS.API.Azure.Models
{
    public record Reply : EntityBase
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
        public string SourceContactNumber
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
        public string SequenceKey
        {
            set; get;
        }
    }
}
