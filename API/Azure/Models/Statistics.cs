namespace EasySMS.API.Azure.Models
{

    public record Statistics : AccountEntityBase
    {
        public SentMessageMonthStatistics[] SentMessageMonthStatistics
        {
            get; set;
        }
        public PendingMessageMonthStatistics[] PendingMessageMonthStatistics
        {
            get; set;
        }
        public PurchasedToken[] PurchasedTokens
        {
            get; set;
        }
    }

    public record PurchasedToken
    {
        public DateOnly Date
        {
            get; set;
        }
        public decimal Quantity
        {
            get; set;
        }
    }

    public record SentMessageMonthStatistics
    {
        public DateOnly Date
        {
            get; set;
        }
        public decimal Cost
        {
            get; set;
        }
        public int LocalMessagesNo
        {
            get; set;
        }
        public int InternationalMessagesNo
        {
            get; set;
        }
    }

    public record PendingMessageMonthStatistics : SentMessageMonthStatistics
    {
        public DateOnly Date
        {
            get; set;
        }
        public int LocalMessagesNo
        {
            get; set;
        }
        public int InternationalMessagesNo
        {
            get; set;
        }
    }
}
