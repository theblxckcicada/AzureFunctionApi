using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EasySMS.API.Azure.Services.ConfigurationManager
{
    public record ConfigurationManagerServiceClientSettings
    {
        public string Microsoft { get; init; } = string.Empty;
    }

    public class ConfigurationManagerServiceClientFactory(
        IOptions<ConfigurationManagerServiceClientSettings> options
    )
    {
        private readonly ConfigurationManagerServiceClientSettings settings = options.Value;

        public Dictionary<string, IConfigurationManager<OpenIdConnectConfiguration>> Build()
        {
            return new Dictionary<string, IConfigurationManager<OpenIdConnectConfiguration>>(
                BuildConfigurationManagers(settings)
            );
        }

        private static IEnumerable<
            KeyValuePair<string, IConfigurationManager<OpenIdConnectConfiguration>>
        > BuildConfigurationManagers(ConfigurationManagerServiceClientSettings settings)
        {
            yield return new KeyValuePair<
                string,
                IConfigurationManager<OpenIdConnectConfiguration>
            >(
                nameof(settings.Microsoft),
                new ConfigurationManager<OpenIdConnectConfiguration>(
                    settings.Microsoft,
                    new OpenIdConnectConfigurationRetriever()
                )
            );
        }
    }
}
