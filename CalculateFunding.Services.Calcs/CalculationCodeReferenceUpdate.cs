using System;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationCodeReferenceUpdate : ICalculationCodeReferenceUpdate
    {
        private readonly ITokenChecker _tokenChecker;

        public CalculationCodeReferenceUpdate(ITokenChecker tokenChecker)
        {
            Guard.ArgumentNotNull(tokenChecker, nameof(tokenChecker));

            _tokenChecker = tokenChecker;
        }

        public string ReplaceSourceCodeReferences(string sourceCode, string oldCalcSourceCodeName, string newCalcSourceCodeName)
        {
            Guard.IsNullOrWhiteSpace(sourceCode, nameof(sourceCode));
            Guard.IsNullOrWhiteSpace(oldCalcSourceCodeName, nameof(oldCalcSourceCodeName));
            Guard.IsNullOrWhiteSpace(newCalcSourceCodeName, nameof(newCalcSourceCodeName));

            //NCrunch can't run the tests for this, but they run fine manually
            int position = -1;
            while (sourceCode.Substring(++position).Contains(oldCalcSourceCodeName, StringComparison.CurrentCultureIgnoreCase))
            {
                position = sourceCode.IndexOf(oldCalcSourceCodeName, position, StringComparison.CurrentCultureIgnoreCase);
                if (_tokenChecker.CheckIsToken(sourceCode, oldCalcSourceCodeName, position))
                {
                    string before = sourceCode.Substring(0, position);
                    string after = sourceCode.Substring(position + oldCalcSourceCodeName.Length);
                    sourceCode = $"{before}{newCalcSourceCodeName}{after}";
                }
            }

            return sourceCode;
        }
    }
}