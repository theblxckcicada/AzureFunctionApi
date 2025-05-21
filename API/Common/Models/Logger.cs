using EasySMS.API.Azure.Models;

namespace EasySMS.API.Common.Models
{
    public record Logger : EntityBase
    {
        public EasyLogType EasyLogType
        {
            get; set;
        }
        public string Message
        {
            get; set;
        }
        public string Source
        {
            get; set;
        }
    }

    public enum EasyLogType
    {
        Information,
        Error

    }
}
