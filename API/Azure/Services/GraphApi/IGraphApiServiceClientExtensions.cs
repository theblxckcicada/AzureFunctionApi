using Microsoft.Graph;

namespace DMIX.API.Azure.Services.GraphApi
{
    public static class IGraphApiServiceClientExtensions
    {
        public static GraphServiceClient GetGraphServiceClientAsync(
            this GraphServiceClient clientSecretCredential,
            CancellationToken cancellationToken = default
        )
        {
            // Always make sure table exists
            return clientSecretCredential;
        }
    }
}
