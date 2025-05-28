using Azure.Data.Tables;
using Microsoft.Extensions.Options;

namespace DMIX.API.Azure.Services.TableStorage
{
    public record TableServiceClientSettings
    {
        public string TableStorageBaseUrl
        {
            get; init;
        }
        public string TableStorageAccountName
        {
            get; init;
        }
        public string TableStorageAccountKey
        {
            get; init;
        }
    }

    public class TableServiceClientFactory(IOptions<TableServiceClientSettings> options)
    {
        private readonly TableServiceClientSettings settings = options.Value;

        public TableServiceClient Build()
        {
            return new TableServiceClient(
                new Uri(settings.TableStorageBaseUrl),
                new TableSharedKeyCredential(
                    settings.TableStorageAccountName,
                    settings.TableStorageAccountKey
                )
            );
        }
    }
}
