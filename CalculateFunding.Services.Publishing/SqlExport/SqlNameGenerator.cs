using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SqlNameGenerator : ISqlNameGenerator
    {
        private static readonly Regex ConvertToSentenceCase 
            = new Regex("\\b[a-z]", RegexOptions.Compiled);
        private static readonly Regex InvalidFileNameCharacters 
            = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]", RegexOptions.Compiled);

        public string GenerateIdentifier(string value)
        {
            if (value.IsNullOrWhitespace())
            {
                return null;
            }

            string schemaObject = value;

            schemaObject = schemaObject.Replace("<", "LessThan");
            schemaObject = schemaObject.Replace(">", "GreaterThan");
            schemaObject = schemaObject.Replace("%", "Percent");
            schemaObject = schemaObject.Replace("£", "Pound");
            schemaObject = schemaObject.Replace("=", "Equals");
            schemaObject = schemaObject.Replace("+", "Plus");

            List<string> characters = new List<string>(schemaObject.Select(_ => _.ToString()));

            // Convert "my function name" to "My Function Name"
            MatchCollection matches = ConvertToSentenceCase.Matches(schemaObject);
            for (int i = 0; i < matches.Count; i++)
            {
                characters[matches[i].Index] = characters[matches[i].Index].ToUpperInvariant();
            }

            schemaObject = string.Join(string.Empty, characters);


            // File name contains invalid chars, remove them
            schemaObject = InvalidFileNameCharacters.Replace(schemaObject, string.Empty);

            // Class name doesn't begin with a letter, insert an underscore
            if (!char.IsLetter(schemaObject, 0))
            {
                schemaObject = schemaObject.Insert(0, "_");
            }


            return schemaObject.Replace(" ", String.Empty);
        }
    }
}
