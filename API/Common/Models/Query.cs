namespace DMIX.API.Common.Models
{
    public record Query
    {
        public int PageSize
        {
            get; set;
        }
        public int PageIndex
        {
            get; set;
        }
        public string Sort
        {
            get; set;
        }
        public string? SortDirection
        {
            get; set;
        }
    }

    public record ColumnQuery
    {
        public string Filter
        {
            get; set;
        }
        public string Column
        {
            get; set;
        }
    }
}
