using System.Reflection;
using EasySMS.API.Azure.Models;

namespace EasySMS.API.Functions.Converters;

public static class ReplyConvertor
{
    public static List<SMS> Convert(List<Reply> replies)
    {
        List<SMS> dest = [];

        foreach (var reply in replies)
        {
            var sms = ConvertReplyToSMS(reply);
            dest.Add(sms);
        }
        return dest;
    }

    public static SMS ConvertReplyToSMS(Reply reply)
    {
        ArgumentNullException.ThrowIfNull(reply);

        SMS destination = new();

        var replyProperties = typeof(Reply).GetProperties(
            BindingFlags.Public | BindingFlags.Instance
        );
        var smsProperties = typeof(SMS).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var smsPropertyMap = smsProperties.ToDictionary(p => p.Name, p => p);

        foreach (var replyProperty in replyProperties)
        {
            // Check if the destination has a matching property
            if (smsPropertyMap.TryGetValue(replyProperty.Name, out var smsProperty))
            {
                // Check if the property types are compatible
                if (
                    smsProperty.PropertyType.IsAssignableFrom(replyProperty.PropertyType)
                    && smsProperty.CanWrite
                )
                {
                    // Copy the value from the source to the destination
                    var value = replyProperty.GetValue(reply);
                    smsProperty.SetValue(destination, value);
                }
            }
        }
        return destination;
    }
}
