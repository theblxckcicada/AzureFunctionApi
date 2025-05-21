namespace EasySMS.API.Azure.Models
{
    public record BackgroundJob : EntityBase
    {
        public string JobName
        {
            get; set;
        }
        public DateTime LastRunDateTime
        {
            get; set;
        }
        public Frequency Frequency { get; set; } = Frequency.Minutes;
        public int FrequencyValue { get; set; } = 5;

    }
}
