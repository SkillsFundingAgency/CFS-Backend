using System.Text.RegularExpressions;

namespace CalculateFunding.Generators.NavFeed
{
    public class Helpers
    {
        // Regular Expressions for processing the search text:
        private static readonly Regex _spacingCharacters = new Regex(@"[\s\u2212\u2013\u2014\u2010-]+", RegexOptions.Compiled);
        private static readonly Regex _disallowedCharacters = new Regex(@"[^\w]+", RegexOptions.Compiled);
        private static readonly Regex _multipleDashes = new Regex(@"_{2,}", RegexOptions.Compiled);

        /// <summary>
        /// Sanitise the name by removing unallowed characters, spaces etc.... Must match how the name is sanitised in the front end.
        /// </summary>
        /// <param name="originalSearchTerm">The unsanitised name of the provider/la.</param>
        /// <returns>A string in the format we require.</returns>
        public static string SanitiseName(string originalSearchTerm)
        {
            var defaultName = string.Empty;

            if (string.IsNullOrEmpty(originalSearchTerm))
            {
                return defaultName;
            }

            var cleanSearchTerm = _spacingCharacters.Replace(originalSearchTerm, "_");
            cleanSearchTerm = _disallowedCharacters.Replace(cleanSearchTerm, string.Empty);
            cleanSearchTerm = _multipleDashes.Replace(cleanSearchTerm, "_");
            cleanSearchTerm = cleanSearchTerm.Trim(new[] { '_' });

            if (cleanSearchTerm == string.Empty)
            {
                return defaultName;
            }

            return cleanSearchTerm;
        }
    }
}
