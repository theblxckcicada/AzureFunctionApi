using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common;
using EasySMS.API.Common.Models;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class SMSHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleSMSTextSentAsync<T>(
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader,
            IAzureTableStorageService tableStorageService,
            IASBService ASBService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            EntityAuditor entityAuditor,
            BulkEntity<T> bulkEntity,
            bool isClientAuthorized = false,
            CancellationToken cancellationToken = default
        )
            where T : SMS, new()
        {
            // Manage SMSes being sent
            List<T> messages = [];

            // Group entities by ContactNumber
            var groupedEntities = bulkEntity
                .Entities.GroupBy(entity => entity.ContactNumber)
                .ToList();
            var lockObject = new object();
            foreach (var groupedEntity in groupedEntities)
            {
                foreach (var message in groupedEntity)
                {
                    // split the message into chunks
                    var splitMessages = StringExtensions.SplitMessage(message.Message, 160);
                    var sequenceKey = Guid.NewGuid().ToString(); // create a sequence key for the message
                    var newMessages = splitMessages
                        .Select(
                            (msg, index) =>
                            {
                                // create a new message
                                T m =
                                    new()
                                    {
                                        ContactNumber = message.ContactNumber,
                                        ContactName = message.ContactName,
                                        ContactRowKey = message.ContactRowKey,
                                        GroupName = message.GroupName,
                                        GroupRowKey = message.GroupRowKey,
                                        AccountRowKey = message.AccountRowKey,
                                        AccountName = message.AccountName,
                                        ScheduledDateTime =
                                            message.ScheduledDateTime < DateTime.Now
                                                ? DateTime.Now
                                                : message.ScheduledDateTime,
                                        Message = msg,
                                        Sequence = index + 1,
                                        SequenceKey = sequenceKey,
                                        Status = MessageStatus.PENDING
                                    };
                                return m;
                            }
                        )
                        .ToList();
                    lock (lockObject) // Ensure thread safety
                    {
                        messages.AddRange(newMessages);
                    }
                }
            }

            // Update bulkEntity.Entities with the modified entities
            bulkEntity.Entities = [.. messages];
            _ = Helper.CreateEntityRowKey(bulkEntity);

            // Create orders and handle token auto drawn
            await OrderHelper.HandleOrderAsync(
                tableStorageService,
                configurationManagerService,
                sort,
                filters,
                appHeader,
                entityAuditor,
                bulkEntity,
                isClientAuthorized,
                cancellationToken
            );

            // Send SMS
            var entities = await BulkHelper.UpsertBulkEntities<T, T>(
                tableStorageService,
                entityAuditor,
                appHeader,
                tableName,
                filters,
                sort,
                bulkEntity,
                isClientAuthorized,
                ASBService,
                cancellationToken
            );

            // return results
            return await req.CreateOkResponseAsync(entities, cancellationToken);
        }

        [Obsolete]
        public static async Task<HttpResponseData> HandleSMSGetRequestAsync<T>(
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader,
            IAzureTableStorageService tableStorageService,
            IASBService ASBService,
            string tableName,
            List<Filter> filters,
            Sort sort,
            EntityAuditor entityAuditor,
            BulkEntity<T> bulkEntity,
            bool isClientAuthorized = false,
            CancellationToken cancellationToken = default
        )
            where T : SMS, new()
        {
            var results = (
                await tableStorageService.GetAsync<SMS>(tableName, filters, sort, cancellationToken)
            ).Where(sms => sms.Status == MessageStatus.SENT);
            // group the smses by sequence
            var mergedSMSList = new List<SMS>();

            var groupedSMSes = results.GroupBy(x => x.SequenceKey);

            foreach (var groupedSMS in groupedSMSes)
            {
                var smses = groupedSMS.OrderBy(x => x.Sequence).ToList();
                var sms = smses.FirstOrDefault();
                var mergedMessage = string.Join("", smses.Select(s => s.Message));

                // var mergedSMS = new SMS { SequenceKey = groupedSMS.Key, Message = mergedMessage };
                sms.Message = mergedMessage;

                mergedSMSList.Add(sms);
            }

            return await req.CreateOkResponseAsync(
                new
                {
                    entity = mergedSMSList
                        .OrderByDescending(x =>
                            DateTime.Parse(x.ScheduledDateTime.ToShortDateString())
                        )
                        .Skip(count: sort.PageIndex * sort.PageSize)
                        .Take(sort.PageSize)
                        .ToList(),
                    query = sort,
                    total = mergedSMSList.Count,
                    filters,
                },
                cancellationToken: cancellationToken
            );
        }
    }
}
