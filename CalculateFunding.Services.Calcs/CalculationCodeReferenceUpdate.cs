using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationCodeReferenceUpdate : ICalculationCodeReferenceUpdate
    {
        public string ReplaceSourceCodeReferences(string sourceCode, string oldCalcSourceCodeName, string newCalcSourceCodeName, string calculationNamespace = null)
        {
            Guard.IsNullOrWhiteSpace(sourceCode, nameof(sourceCode));
            Guard.IsNullOrWhiteSpace (oldCalcSourceCodeName, nameof(oldCalcSourceCodeName));
            Guard.IsNullOrWhiteSpace(newCalcSourceCodeName, nameof(newCalcSourceCodeName));

            SyntaxTree calculationSyntaxTree = VisualBasicSyntaxTree.ParseText(sourceCode);
            SyntaxNode root = calculationSyntaxTree.GetRoot();

            SyntaxNode[] invocationsToReplace = root
                .DescendantNodes()
                .Where(_ => (_ is MemberAccessExpressionSyntax || _ is SimpleAsClauseSyntax) && IsForOldCalculationName(_, calculationNamespace, oldCalcSourceCodeName))
                .ToArray();

            Dictionary<SyntaxNode, SyntaxNode> replacementNodes = new Dictionary<SyntaxNode, SyntaxNode>();

            foreach (SyntaxNode invocation in invocationsToReplace)
            {
                string originalSpan = invocation.GetText().ToString();
                string replacementSpan = originalSpan.Replace(oldCalcSourceCodeName, newCalcSourceCodeName, StringComparison.InvariantCultureIgnoreCase);

                SyntaxNode replacementInvocation;

                if (invocation is SimpleAsClauseSyntax)
                {
                    replacementInvocation = ((SimpleAsClauseSyntax)invocation).WithType(SyntaxFactory.ParseTypeName(replacementSpan, 3));
                }
                else
                {
                    SyntaxTree replacementNodeTree = VisualBasicSyntaxTree.ParseText(replacementSpan);
                    replacementInvocation = replacementNodeTree
                        .GetRoot()
                        .DescendantNodes()
                        .OfType<MemberAccessExpressionSyntax>()
                        .Single();
                }

                replacementNodes.Add(invocation, replacementInvocation);
            }

            return root.ReplaceNodes(replacementNodes.Keys, (x, y) => replacementNodes[x]).ToString();
        }

        private bool IsForOldCalculationName(SyntaxNode statementSyntax,
            string calculationNamespace,
            string calculationName)
        {
            string text = statementSyntax.GetText().ToString();

            return text.Contains(calculationNamespace ?? string.Empty, StringComparison.CurrentCultureIgnoreCase) &&
                   text.Contains(calculationName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}