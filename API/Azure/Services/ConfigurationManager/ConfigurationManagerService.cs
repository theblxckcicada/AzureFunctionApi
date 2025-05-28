using DMIX.API.Common.Models;
using DMIX.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DMIX.API.Azure.Services.ConfigurationManager
{
    public interface IConfigurationManagerService
    {
        Task<bool> ValidateJwtTokenAsync(string token, CancellationToken cancellationToken);
        Task<TokenValidationResult> GetTokenAsync(
            string token,
            CancellationToken cancellationToken
        );
        Task<OpenIdConnectConfiguration> GetConfigurationManagerAsync(
            string issuerName,
            CancellationToken cancellationToken
        );

        EasySMSUser GetEasySMSUser(IDictionary<string, object> claims);
    }

    public class ConfigurationManagerService(
        JsonWebTokenHandler tokenHandler,
        Dictionary<string, IConfigurationManager<OpenIdConnectConfiguration>> configurationManagers,
        IConfigurationManager<OpenIdConnectConfiguration> service
    ) : IConfigurationManagerService
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationManagerAsync(
            string issuerName,
            CancellationToken cancellationToken
        )
        {
            return await configurationManagers[issuerName].GetConfigurationAsync(cancellationToken);
        }

        public async Task<TokenValidationResult> GetTokenAsync(
            string token,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var microsoftValidationParameters = await service.GetTokenValidationParametersAsync(
                    await GetConfigurationManagerAsync(
                        nameof(AppAuthorization.Microsoft),
                        cancellationToken
                    ),
                    cancellationToken
                );

                var validatedToken = await tokenHandler.ValidateTokenAsync(
                    token,
                    microsoftValidationParameters
                );

                return validatedToken;
            }
            catch (SecurityTokenException ex)
            {
                throw new SecurityTokenException(ex.Message);
            }
        }

        public async Task<bool> ValidateJwtTokenAsync(
            string token,
            CancellationToken cancellationToken
        )
        {
            try
            {
                return (await GetTokenAsync(token, cancellationToken)).IsValid;
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new HttpRequestException(ex.Message);
            }
        }

        public EasySMSUser GetEasySMSUser(IDictionary<string, object> claims)
        {
            if (claims is null)
            {

                throw new BadHttpRequestException("Invalid Request");
            }
            var firstName =
                claims
                    .FirstOrDefault(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.given_name),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value as string;
            var lastName =
                claims
                    .FirstOrDefault(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.family_name),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value as string;
            var emailAddress = claims
                 .FirstOrDefault(claim =>
                     claim.Key.Equals(nameof(AppAuthorization.emails), StringComparison.OrdinalIgnoreCase)
                 ).Value switch
            {
                List<string> list => list,
                IEnumerable<string> enumerable => enumerable.ToList(),
                string str => [str],
                _ => null
            };

            var userRole =
                claims
                    .FirstOrDefault(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.extension_UserRole),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value as string;


            var business =
                claims
                    .FirstOrDefault(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.extension_Business),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value as string;

            var userId =
                claims
                    .Where(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.sub),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .FirstOrDefault()
                    .Value as string;

            Enum.TryParse<UserRole>(userRole, true, out var UserRole);
            return new()
            {
                FirstName = firstName!,
                LastName = lastName!,
                UserId = userId!,
                EmailAddress = emailAddress?.FirstOrDefault()!,
                Business = business!,
                UserRole = UserRole!,

            };
        }
    }
}
