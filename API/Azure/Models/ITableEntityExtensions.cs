using Azure.Data.Tables;

namespace EasySMS.API.Azure.Models
{
    public static class ITableEntityExtensions
    {
        public static bool IsTransient(this ITableEntity entity) => entity.Timestamp is null;
    }
}
