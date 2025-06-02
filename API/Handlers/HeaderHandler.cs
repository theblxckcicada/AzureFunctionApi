using DMIX.API.Azure.Services.ConfigurationManager;
using DMIX.API.Common.Models;
using DMIX.API.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace DMIX.API.Handlers
{
    public interface IHeaderHandler
    {
        Task<IDictionary<string, object>> GetClaims(
            HttpRequestData req,
            CancellationToken cancellationToken = default
        );
        public IDictionary<string, object> Claims
        {
            get; set;
        }
    }

    public class HeaderHandler(
        IConfigurationManagerService configurationManagerService
    ) : IHeaderHandler
    {
        public IDictionary<string, object> Claims
        {
            get; set;
        } = new Dictionary<string, object>();
        private static readonly List<string> httpTriggerMethods =
        [
            .. Enum.GetNames(typeof(HttpTriggerMethod)),
        ];

        public async Task<IDictionary<string, object>> GetClaims(
            HttpRequestData req,
            CancellationToken cancellationToken = default
        )
        {
            var claims = await GetValidatedClaimsAsync(req.Headers, cancellationToken);

            if (!httpTriggerMethods.Contains(req.Method))
            {
                throw new BadHttpRequestException(Error.RequestNotSupported(req.Method));
            }


            return claims;
        }

        private async Task<IDictionary<string, object>> GetValidatedClaimsAsync(
            HttpHeadersCollection headers,
            CancellationToken cancellationToken
        )
        {
            const string AuthHeaderName = "X-Authorization";

            var authorizationTokenHeader = headers.Contains(AuthHeaderName)
                ? headers.GetValues(AuthHeaderName)?.FirstOrDefault()?.Trim()
                : default;



            // Validate the Token
            var authToken = authorizationTokenHeader
                ?.Replace(nameof(AppAuthorization.Bearer), string.Empty)
                .Trim();

            if (string.IsNullOrEmpty(authToken))
            {
                throw new BadHttpRequestException(Error.AuthorizationTokenNotSupplied());
            }

            var isTokenValid = await configurationManagerService.ValidateJwtTokenAsync(
                authToken,
                cancellationToken
            );

            //TODO: Uncomment this after implementing B2C
            if (!isTokenValid)
            {
                throw new BadHttpRequestException(Error.InvalidAuthorizationToken());
            }

            return (
                await configurationManagerService.GetTokenAsync(authToken, cancellationToken)
            ).Claims;
        }

    }
}
