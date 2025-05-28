using DMIX.API.Azure.Services.ConfigurationManager;
using DMIX.API.Common.Models;
using DMIX.API.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace DMIX.API.Handlers
{
    public interface IHeaderHandler
    {
        Task<AppHeader> GetAppHeaderAsync(
            HttpRequestData req,
            CancellationToken cancellationToken = default
        );
    }

    public class HeaderHandler(
        IConfigurationManagerService configurationManagerService
    ) : IHeaderHandler
    {
        private static readonly List<string> httpTriggerMethods =
        [
            .. Enum.GetNames(typeof(HttpTriggerMethod)),
        ];

        public async Task<AppHeader> GetAppHeaderAsync(
            HttpRequestData req,
            CancellationToken cancellationToken = default
        )
        {
            // var claims = await GetValidatedClaimsAsync(req.Headers, cancellationToken);
            IDictionary<string, object>? claims = null;



            if (!httpTriggerMethods.Contains(req.Method))
            {
                throw new BadHttpRequestException(Error.RequestNotSupported(req.Method));
            }


            return new AppHeader
            {
                Claims = claims,
            };
        }

        private async Task<IDictionary<string, object>> GetValidatedClaimsAsync(
            HttpHeadersCollection headers,
            string apiKey,
            string accountId,
            CancellationToken cancellationToken
        )
        {
            const string AuthHeaderName = "X-Authorization";

            var authorizationTokenHeader = headers.Contains(AuthHeaderName)
                ? headers.GetValues(AuthHeaderName)?.FirstOrDefault()?.Trim()
                : default;

            //TODO: Uncomment this after implementing B2C
            if (string.IsNullOrEmpty(authorizationTokenHeader))
            {
                if (string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(accountId))
                {
                    throw new BadHttpRequestException(
                        Error.AuthorizationTokenNotSupplied(),
                        StatusCodes.Status401Unauthorized
                    );
                }
                throw new BadHttpRequestException(Error.AuthorizationKeysNotSupplied());
            }

            // Validate the Token
            var authToken = authorizationTokenHeader
                ?.Replace(nameof(AppAuthorization.Bearer), string.Empty)
                .Trim();

            var isTokenValid = await configurationManagerService.ValidateJwtTokenAsync(
                authToken,
                cancellationToken
            );

            //TODO: Uncomment this after implementing B2C
            if (!isTokenValid)
            {
                if (string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(accountId))
                {
                    throw new BadHttpRequestException(Error.InvalidAuthorizationToken());
                }
                throw new BadHttpRequestException(Error.AuthorizationKeysNotSupplied());
            }

            return (
                await configurationManagerService.GetTokenAsync(authToken, cancellationToken)
            ).Claims;
        }

    }
}
