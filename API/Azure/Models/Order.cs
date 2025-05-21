using System.Text.Json.Serialization;
using EasySMS.API.Functions.Converters;

namespace EasySMS.API.Azure.Models
{
    public record Order : AccountEntityBase
    {
        public int Quantity
        {
            get; set;
        }
        public string InvoiceId
        {
            get; set;
        }
        public string InvoiceNumber
        {
            get; set;
        }
        public string InvoiceUrl
        {
            get; set;
        }

        [JsonConverter(typeof(EnumJsonConverter<OrderStatus>))]
        public OrderStatus Status
        {
            get; set;
        }
        public decimal PricePerToken
        {
            get; set;
        }
        public decimal TotalAmount
        {
            get; set;
        }
    }

    public enum OrderStatus
    {
        Draft,
        New,
        RequestingInvoice,
        InvoiceUpdated,
        PaymentOutstanding,
        AutoDrawnDown,
        TokenUsed,
        TokenCredit,
        Paid,
    }
}
