using EasySMS.API.Azure.Services.TableStorage;
using Microsoft.Extensions.Logging;

namespace EasySMS.API.Common
{
    public class TableStorageLogger(string categoryName, IAzureTableStorageService tableStorageService) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        [Obsolete]
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string message = formatter(state, exception);

            _ = LogHelper.CreateLogAsync(
                tableStorageService,
                message,
                categoryName,
                exception != null,
                CancellationToken.None
            );
        }


    }

}
