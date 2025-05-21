using Azure.Storage.Blobs;

namespace EasySMS.API.Azure.Services.BlobStorage
{
    public static class IBlobStorageServiceClientExtensions
    {
        public static async Task<BlobServiceClient> WithStorageAsync(
            this BlobServiceClient serviceClient,
            string blobContainerName,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                _ = await serviceClient.CreateBlobContainerAsync(
                    blobContainerName,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception) { }

            return serviceClient;
        }
    }
}
