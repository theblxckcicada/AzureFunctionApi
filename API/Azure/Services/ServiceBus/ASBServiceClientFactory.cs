using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace DMIX.API.Azure.Services.ServiceBus
{
    public record ASBServiceClientSettings
    {
        public string ASBConnectionString
        {
            get; init;
        }
    }

    public class ASBServiceClientFactory(IOptions<ASBServiceClientSettings> options)
    {
        private readonly ASBServiceClientSettings settings = options.Value;

        public ServiceBusClient Build()
        {
            return new ServiceBusClient(
                settings.ASBConnectionString,
                new ServiceBusClientOptions()
                {
                    TransportType = ServiceBusTransportType.AmqpWebSockets
                }
            );
        }
    }
}
