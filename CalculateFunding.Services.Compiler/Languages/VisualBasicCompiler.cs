using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Serilog;

namespace CalculateFunding.Services.Compiler.Languages
{
    public class VisualBasicCompiler : RoslynCompiler
    {

        public VisualBasicCompiler(ILogger logger) : base(logger)
        {
        }

        protected override EmitResult Compile(MetadataReference[] references, MemoryStream ms, List<SourceFile> sourceFiles)
        {
            VisualBasicCompilationOptions options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            IEnumerable<SyntaxTree> syntaxTrees = sourceFiles.Where(x => x.FileName.EndsWith(".vb"))
                .Select(x => SyntaxFactory.ParseSyntaxTree(x.SourceCode));

            VisualBasicCompilation compilation = VisualBasicCompilation.Create("implementation.dll")
                .WithOptions(options)
                .AddSyntaxTrees(syntaxTrees)
                .AddReferences(references);

            return compilation.Emit(ms);
        }

        protected override IDictionary<string, string> GetCalculationFunctions(IEnumerable<SourceFile> sourceFiles)
        {
            return GetCalculationFunctionsFromVb(sourceFiles);
        }

        private IDictionary<string, string> GetCalculationFunctionsFromVb(IEnumerable<SourceFile> sourceFiles)
        {
            SourceFile sourceFile = sourceFiles.FirstOrDefault(m => m.FileName == "Calculations.vb");

            if (sourceFile == null)
            {
                throw new Exception("Missing calculations class file");
            }

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceFile.SourceCode);

            Dictionary<string, string> functions = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach(ClassBlockSyntax classDefinition in GetDescendants<ClassBlockSyntax>(syntaxTree))
            {
                string className = classDefinition.ClassStatement.Identifier.Text;

                if (className == "CalculationContext")
                {
                    continue;
                }

                string namespaceName = className == "AdditionalCalculations" ? "Calculations" : className.Replace("Calculations", "");

                foreach (LambdaExpressionSyntax func in  GetDescendants<LambdaExpressionSyntax>(classDefinition))
                {
                    if (!(func.Parent is AssignmentStatementSyntax)) continue;
                    
                    AssignmentStatementSyntax assignmentStatementSyntax = (AssignmentStatementSyntax) func.Parent;

                    IdentifierNameSyntax identifierNameSyntax =
                        (IdentifierNameSyntax) assignmentStatementSyntax.Left;

                    string funcName = identifierNameSyntax.Identifier.Text;

                    string body = func.ToFullString();

                    MatchCollection matches = Regex.Matches(body, "#ExternalSource.*?\\)(.*?)#End\\sExternalSource",
                        RegexOptions.Singleline);

                    if (matches.Count > 0 && matches[0].Groups.Count > 1)
                    {
                        //we need aggregate parameters to always be fully qualified now (for calcs)
                        functions.Add($"{namespaceName}.{funcName}", matches[0].Groups[1].Value);
                    }
                }
            }

            return functions;
        }
    }
}