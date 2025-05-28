using DMIX.API.Common.Models;
using Newtonsoft.Json;

namespace DMIX.API.Auth
{
    public static class ClaimsExtensions
    {
        public static string? GetName(this IDictionary<string, object> claims)
        {
            return claims
                    .FirstOrDefault(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.name),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value as string;
        }

        public static IEnumerable<string> GetRoles(this IDictionary<string, object> claims)
        {
            var claimValue =
                claims
                    .FirstOrDefault(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.roles),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value as string;

            if (claimValue is null)
            {
                return [];
            }

            // TODO: Why are we serializing the string value? Surely it is already serialized?
            //return JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(claimValue)) ?? [];
            return JsonConvert.DeserializeObject<string[]>(claimValue) ?? [];
        }

        public static (bool isAdmin, string? region) GetRegion(
            this IDictionary<string, object> claims
        )
        {
            var roles = claims.GetRoles();

            var isAdmin = roles.Any(role =>
                string.Equals(
                    nameof(AzureTableStorageSystemProperty.ADMIN),
                    role,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            // The first non "ADMIN" role is the region
            var region = roles.FirstOrDefault(role =>
                !string.Equals(
                    nameof(AzureTableStorageSystemProperty.ADMIN),
                    role,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            return (isAdmin, region);
        }

        public static string? GetClientAppAudience(this IDictionary<string, object> claims)
        {
            return claims
                    .FirstOrDefault(claim =>
                        claim.Key.Equals(
                            nameof(AppAuthorization.aud),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Value as string;
        }
    }
}
