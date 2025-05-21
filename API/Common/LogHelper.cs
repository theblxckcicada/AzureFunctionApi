using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Functions.Helpers;

namespace EasySMS.API.Common
{
    public static class LogHelper
    {
        [Obsolete]
        public static async Task CreateLogAsync(IAzureTableStorageService tableStorageService, string message, string source, bool isError = true, CancellationToken cancellationToken = default)
        {
            _ = await BulkHelper.UpsertEntitiesAsync<Logger, Logger>(
                 tableStorageService,
                 [new() {
                     EasyLogType = isError?EasyLogType.Error:EasyLogType.Information,
                     Message  = message,
                     Source = source,

                 }],
                 nameof(Logger),
                 nameof(Logger),
                 cancellationToken

            );
        }
    }
}
