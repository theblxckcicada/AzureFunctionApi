namespace EasySMS.API.Azure.Models
{
    public record Token : EntityBase
    {
        public int Quantity
        {
            get; set;
        }
        public string AccountRowKey
        {
            get; set;
        }
        public string AccountName
        {
            get; set;
        }

        public DateTime ExpiryDate
        {
            get; set;
        }
    }
}
