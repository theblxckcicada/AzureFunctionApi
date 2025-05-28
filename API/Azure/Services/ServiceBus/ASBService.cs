using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace DMIX.API.Azure.Services.ServiceBus
{
    public interface IASBService
    {
        Task SendBatchMessages<T>(List<T> entities);
        Task SendMessage<T>(T entity);
        Task CloseAsync(ServiceBusSender sender, ServiceBusClient serviceClient);
    }

    public class ASBService(ServiceBusClient serviceClient) : IASBService
    {
        public async Task SendBatchMessages<T>(List<T> entities)
        {
            var sender = serviceClient.CreateSender();
            using var messageBatch = await sender.CreateMessageBatchAsync();

            foreach (var entity in entities)
            {
                // try adding a message to the batch
                if (
                    !messageBatch.TryAddMessage(
                        new ServiceBusMessage(JsonConvert.SerializeObject(entity))
                    )
                )
                {
                    // if it is too large for the batch
                    throw new Exception($"Entity is too large to fit in the batch.");
                }
            }

            try
            {
                // Use the producer client to send the batch of messages to the Service Bus queue
                await sender.SendMessagesAsync(messageBatch);
            }
            finally
            {
                await CloseAsync(sender, serviceClient);
            }
        }

        public async Task SendMessage<T>(T entity)
        {
            var sender = serviceClient.CreateSender();

            try
            {
                // Use the producer client to send the batch of messages to the Service Bus queue
                await sender.SendMessageAsync(
                    new ServiceBusMessage(JsonConvert.SerializeObject(entity))
                );
            }
            finally
            {
                await CloseAsync(sender, serviceClient);
            }
        }

        public async Task CloseAsync(ServiceBusSender sender, ServiceBusClient serviceClient)
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await sender.DisposeAsync();
            await serviceClient.DisposeAsync();
        }
    }
}
