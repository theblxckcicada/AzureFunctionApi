using Azure.Data.Tables;

namespace DMIX.API.Azure.Services.TableStorage
{
    public static class ITableServiceClientExtensions
    {
        public static async Task<TableServiceClient> WithTableAsync(
            this TableServiceClient serviceClient,
            string tableName,
            CancellationToken cancellationToken = default
        )
        {
            // Always make sure table exists
            _ = await serviceClient.CreateTableIfNotExistsAsync(tableName, cancellationToken);
            return serviceClient;
        }
    }
}
