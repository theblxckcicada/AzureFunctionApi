using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace DMIX.API.Http
{
    public static class HttpRequestDataExtensions
    {
        public static async Task<HttpResponseData> CreateResponseAsync<T>(
            this HttpRequestData req,
            HttpStatusCode statusCode,
            T instance,
            CancellationToken cancellationToken = default
        )
        {
            var httpResponse = req.CreateResponse(statusCode);
            var code = httpResponse.StatusCode;
            if (instance != null)
            {
                await httpResponse.WriteAsJsonAsync(instance, cancellationToken);
                httpResponse.StatusCode = code;
            }

            return httpResponse;
        }

        public static async Task<HttpResponseData> CreateBadRequestResponseAsync(
            this HttpRequestData req,
            string errorMessage,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest,
            CancellationToken cancellationToken = default
        )
        {
            return await req.CreateResponseAsync<dynamic>(
                statusCode,
                new
                {
                    ErrorMessage = errorMessage
                },
                cancellationToken
            );
        }

        public static async Task<HttpResponseData> CreateOkResponseAsync<T>(
            this HttpRequestData req,
            T instance,
            CancellationToken cancellationToken = default
        )
        {
            return await req.CreateResponseAsync(
                HttpStatusCode.OK,
                instance,
                cancellationToken
            );
        }
    }
}
