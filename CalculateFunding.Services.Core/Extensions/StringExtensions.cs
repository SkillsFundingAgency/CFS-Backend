using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace System
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    static public class StringExtensions
    {
        public static string ConvertExpotentialNumber(this string text, string replaceWith = "0")
        {
            return Regex.Replace(text, "E[+|-](\\d)+", replaceWith);
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
                byte[] data =Convert.FromBase64String(base64String);
                return true;
            }
            catch
            {}
            return false;
        }

        public static byte[] Compress(this string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return new byte[0];
            }

            byte[] data = Encoding.UTF8.GetBytes(body);

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
    }
}
