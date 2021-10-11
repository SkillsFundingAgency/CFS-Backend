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
        private const string FundingLinesNamespace = "FundingLines";

        public string ReplaceSourceCodeReferences(string sourceCode, string oldCalcSourceCodeName, string newCalcSourceCodeName, string @namespace = null)
        {
            Guard.IsNullOrWhiteSpace(sourceCode, nameof(sourceCode));
            Guard.IsNullOrWhiteSpace (oldCalcSourceCodeName, nameof(oldCalcSourceCodeName));
            Guard.IsNullOrWhiteSpace(newCalcSourceCodeName, nameof(newCalcSourceCodeName));

            SyntaxTree calculationSyntaxTree = VisualBasicSyntaxTree.ParseText(sourceCode);
            SyntaxNode root = calculationSyntaxTree.GetRoot();

            SyntaxNode[] invocationsToReplace = root
                .DescendantNodes()
                .Where(_ => (_ is MemberAccessExpressionSyntax || _ is SimpleAsClauseSyntax) && HasReference(_, @namespace, oldCalcSourceCodeName))
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
                        .First();
                }

                replacementNodes.Add(invocation, replacementInvocation);
            }

            return root.ReplaceNodes(replacementNodes.Keys, (x, y) => replacementNodes[x]).ToString();
        }

        private bool HasReference(SyntaxNode statementSyntax,
            string calculationNamespace,
            string calculationName)
        {
            IEnumerable<string> texts = statementSyntax.DescendantNodes().Select(_ => _.GetText().ToString().Trim());

            // if this is a funding line and we haven't passed the funding line namespace in then we need to filter it out
            if (calculationNamespace != FundingLinesNamespace && texts.Any(_ => _.Equals(FundingLinesNamespace, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            bool namespaceCheck = calculationNamespace != null ?
                texts.Any(_ => _.Equals(calculationNamespace, StringComparison.InvariantCultureIgnoreCase)) :
                true;

            return namespaceCheck &&
                   texts.Any(_ => _.Equals(calculationName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}