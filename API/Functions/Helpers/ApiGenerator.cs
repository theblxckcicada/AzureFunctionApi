using System.Security.Cryptography;
using System.Text;

namespace EasySMS.API.Functions.Helpers;

public static class ApiKeyGenerator
{
    public static string GenerateApiKey(string id, int code, int version)
    {
        const int length = 32;
        // Combine the input string and integers into a single string
        var combinedInput = $"{id}{code + version}";

        // Use SHA256 to hash the combined input
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combinedInput));

        // Convert the hash to a base64 string
        var base64Hash = Convert.ToBase64String(hashBytes);

        // Make the string URL-safe by replacing '+' and '/'
        var urlSafeSecret = base64Hash.Replace("+", "-").Replace("/", "_");

        // Truncate or pad the string to the desired length
        if (urlSafeSecret.Length > length)
        {
            return urlSafeSecret.Substring(0, length);
        }
        else
        {
            // If the string is too short, pad it with additional characters
            return urlSafeSecret.PadRight(length, '0');
        }
    }

    public static string GetRandomLetters(int length = 2)
    {
        Random random = new();
        if (length < 2 || length > 3)
        {
            throw new ArgumentException("Length must be 2 or 3.");
        }

        var randomLetters = "";
        for (var i = 0; i < length; i++)
        {
            randomLetters += (char)('a' + random.Next(0, 26));
        }

        return randomLetters.ToUpper();
    }

    public static string ScrambleString(string input)
    {
        Random random = new();
        // Convert the string to a character array
        var characters = input.ToCharArray();

        // Shuffle the characters using Fisher-Yates algorithm
        for (var i = characters.Length - 1; i > 0; i--)
        {
            var j = random.Next(0, i + 1); // Random index between 0 and i
            // Swap characters[i] and characters[j]
            var temp = characters[i];
            characters[i] = characters[j];
            characters[j] = temp;
        }

        // Convert the shuffled array back to a string
        return new string(characters);
    }

    public static string GetMd5Hash(string input)
    {
        // Create an MD5 hash object
        // Convert the input string to a byte array and compute the hash
        var data = MD5.HashData(Encoding.UTF8.GetBytes(input));

        // Create a StringBuilder to collect the bytes and create a string

        StringBuilder sBuilder = new();

        // Loop through each byte of the hashed data and format it as a hexadecimal string

        for (var i = 0; i < data.Length; i++)
        {
            _ = sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string

        return sBuilder.ToString();
    }

    public static string Encrypt(string text, int shift)
    {
        var buffer = text.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            var letter = buffer[i];
            // Shift only letters, ignore other characters
            if (char.IsLetter(letter))
            {
                var offset = char.IsUpper(letter) ? 'A' : 'a';
                letter = (char)((letter + shift - offset) % 26 + offset);
            }
            buffer[i] = letter;
        }
        return new string(buffer);
    }

    public static string Decrypt(string text, int shift)
    {
        return Encrypt(text, 26 - shift); // Decrypting is just shifting in the opposite direction
    }
}
