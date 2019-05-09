using System;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Services.Calcs.Interfaces;

namespace CalculateFunding.Services.Calcs
{
    public class TokenChecker : ITokenChecker
    {
        //NCrunch can't see the tests for this, but they are there
        public bool CheckIsToken(string sourceCode, string tokenName, int position)
        {
            if (position < 0 || position > sourceCode.Length - tokenName.Length)
            {
                throw new ArgumentException("Supplied token position is invalid");
            }

            if (string.IsNullOrWhiteSpace(tokenName))
            {
                throw new ArgumentException("Token names cannot be blank");
            }
            if (!Regex.IsMatch(tokenName, "^[A-Za-z_]"))
            {
                throw new ArgumentException($"Token name '{tokenName}' is not a valid identifier");
            }

            string invalidCharacters = " \t.()[]{}-+=!\"':;,/|\\¬¦%&*'" + Environment.NewLine;
            foreach (var character in invalidCharacters.ToCharArray())
            {
                if (tokenName.Contains(character))
                {
                    throw new ArgumentException($"Token name '{tokenName}' is not a valid identifier");
                }
            }

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

            return result;
        }
    }
}