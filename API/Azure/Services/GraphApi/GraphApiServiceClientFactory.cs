using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace DMIX.API.Azure.Services.GraphApi
{
    public record GraphApiServiceClientSettings
    {
        public string AppClientId { get; init; } = string.Empty;
        public string AppClientSecret { get; init; } = string.Empty;
        public string AppTenantId { get; init; } = string.Empty;
    }

    public class GraphApiServiceClientFactory(IOptions<GraphApiServiceClientSettings> options)
    {
        private readonly GraphApiServiceClientSettings settings = options.Value;

        public GraphServiceClient Build()
        {
            return new GraphServiceClient(
                new ClientSecretCredential(
                    settings.AppTenantId,
                    settings.AppClientId,
                    settings.AppClientSecret
                )
            );
        }
    }
}
