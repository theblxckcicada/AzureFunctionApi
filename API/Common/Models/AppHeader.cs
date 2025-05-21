namespace EasySMS.API.Common.Models
{
    public record AppHeader
    {
        public string BlobName
        {
            get; set;
        }
        public string TableName
        {
            get; set;
        }
        public List<Filter> Filters
        {
            get; set;
        }
        public Sort Sort
        {
            get; set;
        }
        public string ApiKey
        {
            get; set;
        }
        public string AccountId
        {
            get; set;
        }
        public IDictionary<string, object> Claims
        {
            get; set;
        }
    }
}
