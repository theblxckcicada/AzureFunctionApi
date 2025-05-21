using Microsoft.Extensions.Options;

namespace EasySMS.API.Auth
{
    public record AuthSettings
    {
        public string? ClientAppAudience
        {
            get; init;
        }
    }

    public interface IAuthorizer
    {
        bool IsClientAuthorized(IDictionary<string, object> claims);
    }

    public class Authorizer(IOptions<AuthSettings> options) : IAuthorizer
    {
        private readonly AuthSettings settings = options.Value;

        public bool IsClientAuthorized(IDictionary<string, object> claims)
        {
            var audience = claims.GetClientAppAudience();
            if (string.IsNullOrEmpty(audience))
            {
                return false;
            }

            return audience.Equals(settings.ClientAppAudience, StringComparison.Ordinal);
        }
    }
}
