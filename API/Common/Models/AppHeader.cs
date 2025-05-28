namespace DMIX.API.Common.Models
{
    public record AppHeader
    {
        public IDictionary<string, object> Claims
        {
            get; set;
        }
    }
}
