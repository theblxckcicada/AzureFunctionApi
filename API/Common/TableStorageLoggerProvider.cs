using EasySMS.API.Azure.Services.TableStorage;
using Microsoft.Extensions.Logging;

namespace EasySMS.API.Common
{
    public class TableStorageLoggerProvider(IAzureTableStorageService tableStorageService) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new TableStorageLogger(categoryName, tableStorageService);
        }

        public void Dispose()
        {
        }
    }

}
