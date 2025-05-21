using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Functions.Converters;
using EasySMS.API.Handlers;
using EasySMS.API.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers.Model
{
    public static class HistoryHelper
    {
        [Obsolete]
        public static async Task<HttpResponseData> HandleHistoryGetRequestAsync<T>(
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
            var startDate = DateTime.Now;
            var endDate = DateTime.Now;
            var statusFilter = MessageStatus.SENT;
            var hasReply = false;
            var exportToExcel = false;
            List<Filter> _filters = [];
            foreach (var filter in filters)
            {
                switch (filter.KeyName)
                {
                    case "StartDate":
                        startDate = DateTime.Parse(filter.Value);
                        break;
                    case "EndDate":
                        endDate = DateTime.Parse(filter.Value);
                        break;
                    case "Status":
                        if (filter.Value == "SENT")
                        {
                            statusFilter = MessageStatus.SENT;
                        }
                        else if (filter.Value == "PENDING")
                        {
                            statusFilter = MessageStatus.PENDING;
                        }
                        break;
                    case "HasReply":
                        hasReply = true;
                        break;
                    case "ExportToExcel":
                        exportToExcel = true;
                        break;
                    default:
                        _filters.Add(filter);
                        break;
                }
            }

            // get smses
            var sms_results = (
                await tableStorageService.GetAsync<SMS>(
                    nameof(AppTableName.SMS),
                    _filters,
                    sort,
                    cancellationToken
                )
            )
                .Where(sms => sms.Status == statusFilter)
                .ToList();

            // filter smses by date
            if (startDate.Day != endDate.Day)
            {
                sms_results =
                [
                    .. sms_results.Where(sms =>
                        sms.CreatedDate >= startDate && sms.CreatedDate <= endDate
                    )
                ];
            }

            // group the smses by sequence
            var mergedSMSList = new List<SMS>();

            var groupedSMSes = sms_results.GroupBy(x => x.SequenceKey);

            foreach (var groupedSMS in groupedSMSes)
            {
                var smses = groupedSMS.OrderBy(x => x.Sequence).ToList();
                var sms = smses.FirstOrDefault();
                var mergedMessage = string.Join("", smses.Select(s => s.Message));

                // var mergedSMS = new SMS { SequenceKey = groupedSMS.Key, Message = mergedMessage };
                sms.Message = mergedMessage;

                mergedSMSList.Add(sms);
            }

            // filter smses by replies
            if (hasReply)
            {
                /**
                NOTE: Use sequence key to query for replies in the table storage,
                NOTE: the reason being that a long message is split into multiple smses and will have different row keys.
                NOTE: But the sequence key remains the same in each as a unique identifier for the text
                **/
                /* List<Reply> replies = new List<Reply>();
                foreach (var sms in sms_results)
                {
                    Filter seq = new()
                    {
                        KeyName = nameof(sms.SequenceKey),
                        Value = sms.SequenceKey,
                        IsComparatorSupported = true,
                        IsKeyQueryable = true,
                    };
                    _filters.Add(seq);
                    var reply_result = (await tableStorageService.GetAsync<Reply>(nameof(AppTableName.Reply), _filters, sort, cancellationToken)).ToList();
                    string mergedReply = "";
                    var reply = reply_result.FirstOrDefault();
                    foreach (var groupedReply in reply_result)
                    {
                        mergedReply = string.Join("", reply_result.Select(r => r.Message));
                        reply.Message = mergedReply;
                    }
                    replies.Add(reply);
                }
                List<SMS> smses = ReplyConvertor.Convert(replies);
                mergedSMSList.AddRange(smses); */
            }

            // export to excel
            if (exportToExcel)
            {
                var smses = ModelConvertor.ConvertModels<SMS, ExportSMS>(mergedSMSList);
                return await ModelConvertor.DownloadExcelFileAsync(req, smses);
            }
            else
            {
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
}
