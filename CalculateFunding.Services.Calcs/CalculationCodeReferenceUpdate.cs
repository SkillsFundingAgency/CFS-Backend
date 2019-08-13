using System;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
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

        public string ReplaceSourceCodeReferences(Calculation calculation, string oldCalcSourceCodeName, string newCalcSourceCodeName)
        {
            Guard.ArgumentNotNull(calculation, nameof(calculation));
            Guard.IsNullOrWhiteSpace(oldCalcSourceCodeName, nameof(oldCalcSourceCodeName));
            Guard.IsNullOrWhiteSpace(newCalcSourceCodeName, nameof(newCalcSourceCodeName));

            string sourceCode = calculation.Current.SourceCode;
            string calculationNamespace = calculation.Namespace;

            int position = -1;
            while (sourceCode.Substring(++position).Contains(oldCalcSourceCodeName, StringComparison.CurrentCultureIgnoreCase))
            {
                position = sourceCode.IndexOf(oldCalcSourceCodeName, position, StringComparison.CurrentCultureIgnoreCase);

                int? tokenLength = _tokenChecker.CheckIsToken(sourceCode, calculationNamespace, oldCalcSourceCodeName, position);

                if (tokenLength != null)
                {
                    string before = sourceCode.Substring(0, position);
                    string after = sourceCode.Substring(position + (int)tokenLength);
                    sourceCode = $"{before}{calculationNamespace}.{newCalcSourceCodeName}{after}";
                }
            }

            return sourceCode;
        }
    }
}