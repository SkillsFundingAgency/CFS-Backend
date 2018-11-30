using System.ComponentModel;
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

            return text.Trim().Replace(" ", "");
        }
    }
}
