using System.Collections.Specialized;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Common;
using EasySMS.API.Common.Models;
using EasySMS.API.DependencyInjection;
using EasySMS.API.Functions.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace EasySMS.API.Functions.RequestValidation
{
    public interface IHeaderHandler
    {
        Task<AppHeader> GetAppHeaderAsync(
            HttpRequestData req,
            CancellationToken cancellationToken = default
        );
    }

    public class HeaderHandler(
        IConfigurationManagerService configurationManagerService,
        IEntityTypeProvider entityTypeProvider
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
            // check for query parameters
            var apiKey = req.Query[nameof(Headers.ApiKey)];
            var accountId = req.Query[nameof(Headers.Id)];

            // Validate the id ( which is the account's rowkey )
            var isIdValid = accountId.IsValidUUID();
            if (!isIdValid)
            {
                claims = await GetValidatedClaimsAsync(
                    req.Headers,
                    apiKey,
                    accountId,
                    cancellationToken
                );
            }

            if (!httpTriggerMethods.Contains(req.Method))
            {
                throw new BadHttpRequestException(Error.RequestNotSupported(req.Method));
            }

            var (blobName, tableName) = GetBlobTable(req.Headers);

            var filters = GetFilters(req.Headers);

            var sort = GetSort(req.Query);

            return new AppHeader
            {
                TableName = tableName,
                Filters = filters,
                Sort = sort,
                BlobName = blobName,
                Claims = claims,
                ApiKey = apiKey,
                AccountId = accountId,
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

        private (string blobName, string tableName) GetBlobTable(HttpHeadersCollection headers)
        {
            var blobName = headers.Contains(nameof(Headers.BlobContainerName))
                ? headers.GetValues(nameof(Headers.BlobContainerName))?.FirstOrDefault()?.Trim()
                : default;

            var tableName = headers.Contains(nameof(Headers.TableName))
                ? headers.GetValues(nameof(Headers.TableName))?.FirstOrDefault()?.Trim()
                : default;

            if (string.IsNullOrEmpty(tableName) && string.IsNullOrEmpty(blobName))
            {
                throw new BadHttpRequestException(Error.HeaderKeyNotSupplied());
            }

            if (tableName is null || string.IsNullOrEmpty(tableName))
            {
                throw new BadHttpRequestException(Error.HeaderKeyNotSupplied());
            }

            var entityType = entityTypeProvider.GetByName(tableName) ?? throw new BadHttpRequestException(Error.HeaderKeyNotSupplied(tableName));
            return (blobName ?? string.Empty, tableName ?? string.Empty);
        }

        private static List<Filter> GetFilters(HttpHeadersCollection headers)
        {
            var filterHeader = headers.Contains(nameof(Filter))
                ? headers.GetValues(nameof(Filter))?.FirstOrDefault()?.Trim()
                : default;

            if (string.IsNullOrEmpty(filterHeader))
            {
                return [];
            }

            return [.. JsonConvert.DeserializeObject<Filter[]>(filterHeader) ?? []];
        }

        private static Sort GetSort(NameValueCollection query)
        {
            var PageSize = int.Parse(query[nameof(Sorting.PageSize)] ?? "10");
            var sortDirection = query[nameof(Sorting.sort)] ?? nameof(DataSort.asc);
            var filter = string.IsNullOrEmpty(query[nameof(Sorting.filter)])
                ? nameof(AzureTableStorageSystemProperty.RowKey)
                : query[nameof(Sorting.filter)];
            var PageIndex = int.Parse(query[nameof(Sorting.PageIndex)] ?? "0");

            return new()
            {
                FilterValue = filter ?? string.Empty,
                SortDirection = sortDirection,
                PageSize = PageSize,
                PageIndex = PageIndex,
            };
        }
    }
}
