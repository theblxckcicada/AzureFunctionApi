using EasySMS.API.Azure.Models;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Common;
using EasySMS.API.Common.Models;
using Microsoft.Azure.Functions.Worker.Http;

namespace EasySMS.API.Functions.RequestValidation
{
    [Obsolete("Use IEntityRequestValidator<> and IEntityFilterProvider<> with mediator instead")]
    public static class RoleAndPermissions
    {
        public static List<Filter> ValidateRolesAndPermissions(
            HttpRequestData req,
            IConfigurationManagerService configurationManagerService,
            AppHeader appHeader
        )
        { // get user id
            var user = configurationManagerService.GetEasySMSUser(appHeader.Claims);
            var tableName = appHeader.TableName;
            var filters = appHeader.Filters;

            // add user id to filters as partition key or user id

            if (tableName.Equals(nameof(GroupContact)))
            {
                filters =
                [
                    .. filters.Where(f =>
                        !f.KeyName.Equals(
                            nameof(EntityBase.UserId),
                            StringComparison.OrdinalIgnoreCase
                        )
                    ),
                ];
                filters.Add(
                    new()
                    {
                        KeyName = nameof(EntityBase.UserId),
                        Value = user.UserId,
                        IsComparatorSupported = false,
                        IsKeyQueryable = true,
                    }
                );
            }
            else
            {
                if (tableName.Equals(nameof(Account)))
                {
                    // put the user id as partition key
                    var filter = filters.FirstOrDefault(f =>
                        f.KeyName.Equals(
                            nameof(EntityBase.PartitionKey),
                            StringComparison.OrdinalIgnoreCase
                        ) && !f.Value.IsValidUUID()
                    );
                    if (filter is not null)
                    {
                        filters =
                        [
                            .. filters.Where(filter =>
                                !filter.KeyName.Equals(
                                    nameof(EntityBase.PartitionKey),
                                    StringComparison.OrdinalIgnoreCase
                                )
                            ),
                        ];
                    }
                }
                else
                {
                    // put the user id as partition key
                    var filter = filters.FirstOrDefault(f =>
                        f.KeyName.Equals(
                            nameof(EntityBase.PartitionKey),
                            StringComparison.OrdinalIgnoreCase
                        ) && f.Value.Equals(user.UserId, StringComparison.OrdinalIgnoreCase)
                    );
                    filters =
                    [
                        .. filters.Where(filter =>
                            !filter.KeyName.Equals(
                                nameof(EntityBase.PartitionKey),
                                StringComparison.OrdinalIgnoreCase
                            )
                        ),
                    ];

                    // check if filter exists

                    filters.Add(
                        filter
                            ?? new()
                            {
                                KeyName = nameof(EntityBase.PartitionKey),
                                Value = user.UserId,
                                IsComparatorSupported = true,
                                IsKeyQueryable = true,
                            }
                    );
                }
            }

            // get unique and valid filters
            filters =
            [
                .. filters.Where(filter =>
                    !string.IsNullOrEmpty(filter.Value) && !string.IsNullOrEmpty(filter.KeyName)
                ),
            ];

            filters = [.. filters.DistinctBy(x => (x.KeyName, x.Value))];

            // if the filters are empty use the partition key
            if (filters.Count <= 0)
            {
                filters.Add(
                    new()
                    {
                        KeyName = nameof(EntityBase.PartitionKey),
                        Value = user.UserId,
                        IsComparatorSupported = true,
                        IsKeyQueryable = true,
                    }
                );
            }

            return filters;
        }
    }
}
