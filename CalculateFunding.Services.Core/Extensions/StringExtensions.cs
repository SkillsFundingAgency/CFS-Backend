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
    }
}
