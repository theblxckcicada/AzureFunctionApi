using System.Reflection;
using Azure.Data.Tables;
using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Common;
using EasySMS.API.Common.Models;
using EasySMS.API.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.Helpers
{
    public static class Helper
    {
        public static readonly int InternationalMessageCost = 5;

        public static BulkEntity<T> CreateEntityRowKey<T>(BulkEntity<T> bulkEntity)
            where T : EntityBase, new()
        {
            foreach (var entity in bulkEntity.Entities)
            {
                entity.RowKey = entity.RowKey.IsValidUUID()
                    ? entity.RowKey
                    : Guid.NewGuid().ToString();
            }

            return bulkEntity;
        }

        public static List<T> ConvertDateTimePropertiesToUtc<T>(List<T> objects)
        {
            foreach (var obj in objects)
            {
                var properties = obj.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property =>
                        property.PropertyType == typeof(DateTime) && property.CanWrite
                    )
                    .ToList();

                foreach (var property in properties)
                {
                    _ = DateTime.TryParse(
                        ((DateTime)property.GetValue(obj)).ToString(),
                        out var originalValue
                    );

                    // Convert to UTC if necessary
                    var utcValue = originalValue == default ? DateTime.Now : originalValue;
                    property.SetValue(obj, utcValue.ToUniversalTime());
                }
            }
            return objects;
        }

        public static BulkEntity<T> UpdateModel<T>(
            BulkEntity<T> bulkEntities,
            EntityAuditor entityAuditor,
            AppHeader appHeader
        )
            where T : EntityBase, new()
        {
            return entityAuditor.WriteAuditData(bulkEntities, appHeader.Claims);
        }

        public static T UpdateEntity<T>(
            T entity,
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader
        )
            where T : EntityBase, new()
        {
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);

            if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
            {
                throw new BadHttpRequestException("Invalid user account");
            }
            if (
                req.Method.Equals(
                    nameof(HttpTriggerMethod.POST),
                    StringComparison.OrdinalIgnoreCase
                ) || string.IsNullOrEmpty(entity.RowKey)
            )
            //  req.Method.Equals(nameof(HttpTriggerMethod.GET), StringComparison.OrdinalIgnoreCase))
            {
                entity.CreatedBy = $"{user.FirstName} {user.LastName}";
                entity.CreatedDate = DateTime.Now.ToUniversalTime();
                entity.ModifiedBy = $"{user.FirstName} {user.LastName}";
                entity.ModifiedDate = DateTime.Now.ToUniversalTime();
                entity.UserId = user.UserId;
            }
            if (
                req.Method.Equals(nameof(HttpTriggerMethod.PUT), StringComparison.OrdinalIgnoreCase)
            )
            {
                entity.CreatedBy = string.IsNullOrEmpty(entity.CreatedBy)
                    ? $"{user.FirstName} {user.LastName}"
                    : entity.CreatedBy;
                entity.CreatedDate =
                    entity.CreatedDate == default
                        ? DateTime.Now.ToUniversalTime()
                        : entity.CreatedDate.ToUniversalTime();
                entity.ModifiedBy = $"{user.FirstName} {user.LastName}";
                entity.ModifiedDate = DateTime.Now.ToUniversalTime();
                entity.UserId = user.UserId;
            }
            return entity;
        }

        public static Dictionary<string, Type> GetListProperties(object model)
        {
            var properties = model.GetType().GetProperties();
            var listProperties = new Dictionary<string, Type>();

            foreach (var property in properties)
            {
                if (
                    property.PropertyType.IsGenericType
                    && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                )
                {
                    var listType = property.PropertyType.GetGenericArguments()[0];
                    listProperties.Add(property.Name, listType);
                }
            }

            return listProperties;
        }

        public static Dictionary<string, (Type type, List<object> subItems)> AddSubEntities<T>(
            List<T> models
        )
            where T : class, ITableEntity, new()
        {
            var entities = new Dictionary<string, (Type type, List<object> subItems)>();
            foreach (var model in models)
            {
                var properties = model.GetType().GetProperties();
                var listProperties = new Dictionary<string, (Type type, List<object> subItems)>();

                foreach (var property in properties)
                {
                    if (
                        property.PropertyType.IsGenericType
                        && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        var listType = property.PropertyType.GetGenericArguments()[0];
                        var listValues =
                            (IEnumerable<object>)property.GetValue(model)
                            ?? Enumerable.Empty<object>();
                        var listEntities = new List<object>();
                        foreach (var item in listValues)
                        {
                            if (item is ITableEntity tableEntityItem)
                            {
                                tableEntityItem.PartitionKey = model.RowKey;
                                if (entities.ContainsKey(listType.Name))
                                {
                                    entities[listType.Name].subItems.Add(tableEntityItem);
                                }
                                else
                                {
                                    entities.Add(listType.Name, (listType, [tableEntityItem]));
                                }
                            }
                        }
                    }
                }
            }
            return entities;
        }

        [Obsolete("Use StringExtensions.IsValidUUID() extension method instead")]
        public static bool IsValidUUID(string uuid)
        {
            return uuid.IsValidUUID();
        }

        public static bool hasCustomFilter(List<Filter> filters, string[] checkFor)
        {
            foreach (var filter in filters)
            {
                if (checkFor.Any(filter.KeyName.Contains))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
