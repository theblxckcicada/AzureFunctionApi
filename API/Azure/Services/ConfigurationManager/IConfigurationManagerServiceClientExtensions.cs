using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DMIX.API.Azure.Services.ConfigurationManager
{
    public static class IConfigurationManagerServiceClientExtensions
    {
        public static async Task<TokenValidationParameters> GetTokenValidationParametersAsync(
            this IConfigurationManager<OpenIdConnectConfiguration> serviceClient,
            OpenIdConnectConfiguration? openIdConfig,
            CancellationToken cancellationToken = default
        )
        {
            openIdConfig ??= await serviceClient.GetConfigurationAsync(cancellationToken);
            // Always make sure table exists
            return new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidIssuer = openIdConfig.Issuer,
                IssuerSigningKeys = openIdConfig.SigningKeys
            };
        }
    }
}
