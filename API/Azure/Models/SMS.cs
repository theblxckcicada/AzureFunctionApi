namespace EasySMS.API.Azure.Models
{
    public record SMS : AccountEntityBase
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
        public string SequenceKey
        {
            set; get;
        }
        public int Sequence
        {
            set; get;
        }
        public MessageStatus Status
        {
            get; set;
        }
        public DateTime ScheduledDateTime
        {
            set; get;
        }
        public string SMSRowKey
        {
            set; get;
        }
        public string? ErrorMessage
        {
            set; get;
        }
    }

    public record TokenAutoDrawn : AccountEntityBase
    {
        public int TokenCreditNeeded
        {
            get; set;
        }
        public int AccountCredit
        {
            get; set;
        }
        public int TokenAutoDrawAmount
        {
            get; set;
        }
        public string ErrorMessage
        {
            get; set;
        }
    }

    public enum MessageStatus
    {
        PENDING,
        SENT,
        Error
    }
}
