using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace DMIX.API.Azure.Services.BlobStorage
{
    public record BlobServiceClientSettings
    {
        public string BlobStorageBaseUrl
        {
            get; init;
        }
        public string BlobStorageAccountName
        {
            get; init;
        }
        public string BlobStorageAccountKey
        {
            get; init;
        }
    }

    public class BlobServiceClientFactory(IOptions<BlobServiceClientSettings> options)
    {
        private readonly BlobServiceClientSettings settings = options.Value;

        public BlobServiceClient Build()
        {
            return new BlobServiceClient(
                new Uri(settings.BlobStorageBaseUrl),
                new StorageSharedKeyCredential(
                    settings.BlobStorageAccountName,
                    settings.BlobStorageAccountKey
                )
            );
        }
    }
}
