namespace DMIX.API.Common.Models
{
    public record Sort
    {
        public string SortDirection { get; set; } = nameof(DataSort.asc);
        public int PageSize { get; set; } = 10;
        public int PageIndex { get; set; } = 0;
        public string FilterValue { get; set; } = string.Empty;
    }
}
