using System;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Services.Calcs.Interfaces;

namespace CalculateFunding.Services.Calcs
{
    public class TokenChecker : ITokenChecker
    {
        public int? CheckIsToken(string sourceCode, string tokenNamespace, string tokenName, int position)
        {
            return CheckIsToken(sourceCode,
                       tokenName,
                       false,
                       position)
                   ?? (string.IsNullOrWhiteSpace(tokenNamespace)
                        ? null
                        : CheckIsToken(sourceCode,
                           $"{tokenNamespace}.{tokenName}",
                           true,
                           position));
        }

        public int? CheckIsToken(string sourceCode, string tokenName, bool isNamespaced, int position)
        {
            CheckTokenLegal(tokenName, isNamespaced);

            if (sourceCode.IndexOf(tokenName, position) < 0) return null;

            bool result = sourceCode.Substring(position, tokenName.Length) == tokenName;
            string[] validPrecedingCharacters = { "(", ",", "=", "+", "-", "*", "/" };
            string[] validFollowingCharacters = { "(", ")", ".", ",", "+", "-", "*", "/" };

            if (result && position > 0)
            {
                if (!string.IsNullOrWhiteSpace(sourceCode.Substring(position - 1, 1)))
                {
                    string start = sourceCode.Substring(position - 1, 1);
                    if (!validPrecedingCharacters.Contains(start)) result = false;
                }
            }

            if (result && position + tokenName.Length < sourceCode.Length)
            {
                string end = sourceCode.Substring(position + tokenName.Length, 1);
                if (!string.IsNullOrWhiteSpace(end))
                {
                    if (!validFollowingCharacters.Contains(end)) result = false;
                }
            }

            return result
                ? tokenName.Length
                : (int?)null;
        }

        public void CheckTokenLegal(string tokenName, bool isNamespaced)
        {
            if (string.IsNullOrWhiteSpace(tokenName))
            {
                throw new ArgumentException("Token names cannot be blank");
            }

            if (!Regex.IsMatch(tokenName, "^[A-Za-z_]"))
            {
                throw new ArgumentException($"Token name '{tokenName}' is not a valid identifier");
            }

            string invalidCharacters = " \t()[]{}-+=!\"':;,/|\\¬¦%&*'"
                                       + Environment.NewLine
                                       + (isNamespaced ? "" : ".");
            foreach (var character in invalidCharacters.ToCharArray())
            {
                if (tokenName.Contains(character))
                {
                    throw new ArgumentException($"Token name '{tokenName}' is not a valid identifier");
                }
            }
        }
    }
}