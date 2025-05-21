using EasySMS.API.Common.Models;

namespace EasySMS.API.Common
{
    public static class IEnumerableFilterExtensions
    {
        public static Filter? GetForKey(this IEnumerable<Filter> filters, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return filters.FirstOrDefault(filter =>
                string.Equals(key, filter.KeyName, StringComparison.OrdinalIgnoreCase)
            );
        }

        public static string? GetPartitionKeyValue(this IEnumerable<Filter> filters)
        {
            return filters.GetForKey(nameof(AzureTableStorageSystemProperty.PartitionKey))?.Value;
        }

        public static IEnumerable<Filter> Sanitize(this IEnumerable<Filter> filters)
        {
            foreach (var filter in filters)
            {
                if (
                    string.Equals(
                        nameof(AzureTableStorageSystemProperty.PartitionKey),
                        filter.KeyName,
                        StringComparison.OrdinalIgnoreCase
                    ) && string.IsNullOrEmpty(filter.Value)
                )
                {
                    yield return filter with
                    {
                        IsComparatorSupported = false
                    };
                }
                else
                {
                    yield return filter;
                }
            }
        }

        public static IEnumerable<Filter> AppendNew(
            this IEnumerable<Filter> value,
            IEnumerable<Filter> filters
        )
        {
            return value.AppendNew(filters, Filter.EqualityComparer);
        }

        public static IEnumerable<Filter> AppendNew(
            this IEnumerable<Filter> value,
            IEnumerable<Filter> filters,
            IEqualityComparer<Filter> comparer
        )
        {
            return value.Concat(filters.Except(value, comparer));
        }
    }
}
