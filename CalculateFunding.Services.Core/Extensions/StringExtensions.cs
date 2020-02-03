using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Services.Core;

namespace System
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class StringExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static string ComputeSHA1Hash(this string text)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                byte[] hash = sha1.ComputeHash(text.AsUTF8Bytes());

                return string.Concat(hash.Select(_ => _.ToString("X2")));
            }
        }
        
        public static string ConvertExpotentialNumber(this string text, string replaceWith = "0")
        {
            return Regex.Replace(text, "E[+|-](\\d)+", replaceWith);
        }
        
        public static bool IsNotNullOrWhitespace(this string text)
        {
            return !text.IsNullOrWhitespace();
        }

        public static string Join(this IEnumerable<string> strings, string separator)
        {
            return string.Join(separator, strings);
        }

        public static bool IsNullOrWhitespace(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        public static string EmptyIfNull(this string text)
        {
            if(text != null)
            {
                return text;
            }

            return "";
        }

        public static string RemoveAllSpaces(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text.Trim().Replace(" ", string.Empty);
        }

        public static string RemoveAllQuotes(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text.Trim().Replace("'", string.Empty);
        }

        public static Stream ToStream(this string text)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(text);
                    writer.Flush();
                    stream.Position = 0;
                    return stream;
                }
            }
        }

        public static bool IsBase64(this string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String) || base64String.Length % 4 != 0
               || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                    return false;
            try
            {
                byte[] data = Convert.FromBase64String(base64String);
                
                return true;
            }
            catch
            { 
                return false;
            }
        }

        public static byte[] Compress(this string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return new byte[0];
            }

            byte[] data = body.AsUTF8Bytes();

            byte[] zippedBytes;

            using (MemoryStream outputStream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(outputStream, CompressionMode.Compress, false))
                {
                    gZipStream.Write(data, 0, data.Length);

                    gZipStream.Flush();

                    outputStream.Flush();
                    zippedBytes = outputStream.ToArray();
                }
            }

            return zippedBytes;
        }

        public static byte[] AsUTF8Bytes(this string text)
        {
            return Encoding.UTF8.GetBytes(text ?? string.Empty);
        }

        public static string ToCamelCase(this string identifier)
        {
            return string.IsNullOrWhiteSpace(identifier) ? null : $"{char.ToLower(identifier[0])}{identifier.Substring(1)}";
        }
    }
}
