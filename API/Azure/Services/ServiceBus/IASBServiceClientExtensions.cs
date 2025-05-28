using Azure.Messaging.ServiceBus;

namespace DMIX.API.Azure.Services.ServiceBus
{
    public static class IASBServiceClientExtensions
    {
        public static ServiceBusSender CreateSender(
            this ServiceBusClient serviceClient,
            CancellationToken cancellationToken = default
        )
        {
            return serviceClient.CreateSender("ifassetout");
        }
    }
}
