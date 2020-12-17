using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationCodeReferenceUpdate : ICalculationCodeReferenceUpdate
    {
        public string ReplaceSourceCodeReferences(Calculation calculation, string oldCalcSourceCodeName, string newCalcSourceCodeName)
        {
            Guard.ArgumentNotNull(calculation, nameof(calculation));
            Guard.ArgumentNotNull(calculation.Current, nameof(calculation.Current));
            Guard.IsNullOrWhiteSpace (oldCalcSourceCodeName, nameof(oldCalcSourceCodeName));
            Guard.IsNullOrWhiteSpace(newCalcSourceCodeName, nameof(newCalcSourceCodeName));

            string sourceCode = calculation.Current.SourceCode;
            string calculationNamespace = calculation.Namespace;

            SyntaxTree calculationSyntaxTree = VisualBasicSyntaxTree.ParseText(sourceCode);
            SyntaxNode root = calculationSyntaxTree.GetRoot();
            
            MemberAccessExpressionSyntax[] invocationsToReplace = root
                .DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(_ => IsForOldCalculationName(_, calculationNamespace, oldCalcSourceCodeName))
                .ToArray();

            foreach (MemberAccessExpressionSyntax invocation in invocationsToReplace)
            {
                string originalSpan = invocation.GetText().ToString();
                string replacementSpan = originalSpan.Replace(oldCalcSourceCodeName, newCalcSourceCodeName, StringComparison.InvariantCultureIgnoreCase);
                
                SyntaxTree replacementNodeTree = VisualBasicSyntaxTree.ParseText(replacementSpan);
                MemberAccessExpressionSyntax replacementInvocation = replacementNodeTree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MemberAccessExpressionSyntax>()
                    .Single();
                
                root = root.ReplaceNode(invocation, replacementInvocation);
            }

            return root.ToString();
        }

        private bool IsForOldCalculationName(MemberAccessExpressionSyntax statementSyntax,
            string calculationNamespace,
            string calculationName)
        {
            string text = statementSyntax.GetText().ToString();

            return text.Contains(calculationNamespace, StringComparison.CurrentCultureIgnoreCase) &&
                   text.Contains(calculationName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}