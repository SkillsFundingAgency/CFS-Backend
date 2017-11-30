using System.Text.RegularExpressions;

namespace CalculateFunding.Models
{
    public static class ExtensionMethods
    {
        public static string ToSlug(this string phrase)
        {
            string str = phrase.RemoveAccent().ToLower();
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 50 ? str.Length : 50).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens   
            return str;
        }

        private static string RemoveAccent(this string txt)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
    }
}