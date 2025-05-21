using Azure.Data.Tables;

namespace EasySMS.API.Azure.Models
{
    public record BulkEntity<T>
        where T : ITableEntity
    {
        public List<T> Entities { get; set; } = [];
    }
}
