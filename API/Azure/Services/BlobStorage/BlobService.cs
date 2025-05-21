using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EasySMS.API.Common.Models;

namespace EasySMS.API.Azure.Services.BlobStorage
{
    public interface IAzureBlobStorageService
    {
        Task<List<BlobItem>> GetAsync(
            List<Filter> filters,
            CancellationToken cancellationToken = default
        );
        Task<BlobContentInfo> InsertAsync<BlobItem>(
            string blobPath,
            Stream entity,
            DocumentFilter documentFilter,
            CancellationToken cancellationToken = default
        );
        Task<bool> DeleteAsync(
            string blobPath,
            DocumentFilter documentFilter,
            CancellationToken cancellationToken = default
        );
    }

    public class AzureBlobStorageService(BlobServiceClient serviceClient) : IAzureBlobStorageService
    {
        private readonly BlobServiceClient serviceClient = serviceClient;

        public async Task<bool> DeleteAsync(
            string blobPath,
            DocumentFilter documentFilter,
            CancellationToken cancellationToken
        )
        {
            var serviceClient = await this.serviceClient.WithStorageAsync(
                nameof(AppBlobContainerName.FleetMatch).ToLower(),
                cancellationToken
            );
            var containerClient = serviceClient.GetBlobContainerClient(
                nameof(AppBlobContainerName.FleetMatch).ToLower()
            );
            return await containerClient.DeleteBlobIfExistsAsync(
                blobPath + documentFilter.Name,
                cancellationToken: cancellationToken
            );
            // var blobClient = containerClient.GetBlobClient(blobPath + documentFilter.Name);
        }

        public async Task<List<BlobItem>> GetAsync(
            List<Filter> filters,
            CancellationToken cancellationToken
        )
        {
            var blobPath = filters[0].Value;
            var serviceClient = await this.serviceClient.WithStorageAsync(
                nameof(AppBlobContainerName.FleetMatch).ToLower(),
                cancellationToken
            );
            var containerClient = serviceClient.GetBlobContainerClient(
                nameof(AppBlobContainerName.FleetMatch).ToLower()
            );
            var blobItems = containerClient.GetBlobsAsync(
                prefix: blobPath,
                cancellationToken: cancellationToken
            );
            List<BlobItem> blobItemList = [];
            await foreach (var blobItem in blobItems)
            {
                blobItemList.Add(blobItem);
            }

            return blobItemList;
        }

        public async Task<BlobContentInfo> InsertAsync<BlobItem>(
            string blobPath,
            Stream entity,
            DocumentFilter documentFilter,
            CancellationToken cancellationToken
        )
        {
            var serviceClient = await this.serviceClient.WithStorageAsync(
                nameof(AppBlobContainerName.FleetMatch).ToLower(),
                cancellationToken
            );
            var containerClient = serviceClient.GetBlobContainerClient(
                nameof(AppBlobContainerName.FleetMatch).ToLower()
            );

            var blobClient = containerClient.GetBlobClient(blobPath + documentFilter.Name);

            _ = await blobClient.UploadAsync(new MemoryStream([]), true, cancellationToken);
            var result = await blobClient.UploadAsync(
                entity,
                true,
                cancellationToken
            );

            return result;
        }
    }
}
