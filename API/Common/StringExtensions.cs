using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EasySMS.API.Common
{
    public static class StringExtensions
    {
        private static readonly Regex UuidRegex =
            new(
                @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
                RegexOptions.Compiled
            );



        public static string SplitPascalCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return Regex.Replace(input, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        }


        public static bool IsValidUUID(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return UuidRegex.IsMatch(value);
        }

        public static bool IsSouthAfricanNumber(string phoneNumber)
        {
            // Define the regex for South African numbers
            var pattern = @"^(\+27|0)(0)?[6-8][0-9]{8}$";

            // Check if the phone number matches the pattern
            return Regex.IsMatch(phoneNumber, pattern);
        }

        public static List<string> SplitMessage(string message, int chunkSize)
        {
            List<string> chunks = [];

            for (var i = 0; i < message.Length; i += chunkSize)
            {
                // Ensure we don't go out of bounds
                var length = Math.Min(chunkSize, message.Length - i);
                var chunk = message.Substring(i, length);
                chunks.Add(chunk);
            }

            return chunks;
        }

        public static string ComputeHmacSha256(string data, string key)
        {
            // Convert the input strings to byte arrays
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            // Create an instance of the HMACSHA256 class with the key
            using var hmac = new HMACSHA256(keyBytes);
            // Compute the hash
            var hashBytes = hmac.ComputeHash(dataBytes);

            return Convert.ToBase64String(hashBytes);
        }

        public static string Encode(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
