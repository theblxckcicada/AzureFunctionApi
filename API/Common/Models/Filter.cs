namespace DMIX.API.Common.Models
{
    public record Filter
    {
        public bool IsKeyQueryable
        {
            get; init;
        }
        public bool IsComparatorSupported
        {
            get; init;
        }
        public required string KeyName
        {
            get; init;
        }
        public required string Value
        {
            get; init;
        }
        public FilterOperator Operator
        {
            get; init;
        }
        public FilterComparator Comparator
        {
            get; init;
        }
        public FilterComparatorNotSupported ComparatorNotSupported
        {
            get; init;
        }

        public static readonly IEqualityComparer<Filter> EqualityComparer =
            new GenericEqualityComparer<Filter>(
                (x, y) =>
                    x is null && y is null
                    ||
                        string.Equals(x?.KeyName, y?.KeyName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x?.Value, y?.Value, StringComparison.OrdinalIgnoreCase)
                    ,
                obj => $"{obj.KeyName}|{obj.Value}".GetHashCode()
            );
    }
}
