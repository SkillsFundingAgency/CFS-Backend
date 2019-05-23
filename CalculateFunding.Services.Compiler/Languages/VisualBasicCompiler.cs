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

        public IDictionary<string, string> GetCalculationFunctionsFromVb(IEnumerable<SourceFile> sourceFiles)
        {
            SourceFile sourceFile = sourceFiles.FirstOrDefault(m => m.FileName == "Calculations.vb");

            if (sourceFile == null)
            {
                throw new System.Exception("Missing calculations class file");
            }

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceFile.SourceCode);

            Dictionary<string, string> functions = new Dictionary<string, string>();

            foreach (LambdaExpressionSyntax func in syntaxTree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>())
            {
                if (func.Parent != null && func.Parent is AssignmentStatementSyntax)
                {
                    AssignmentStatementSyntax assignmentStatementSyntax = func.Parent as AssignmentStatementSyntax;

                    IdentifierNameSyntax identifierNameSyntax = assignmentStatementSyntax.Left as IdentifierNameSyntax;

                    string funcName = identifierNameSyntax.Identifier.Text;

                    string body = func.ToFullString();

                    MatchCollection matches = Regex.Matches(body, "#ExternalSource.*?\\)(.*?)#End\\sExternalSource", RegexOptions.Singleline);

                    if (matches.Count > 0 && matches[0].Groups.Count > 1)
                    {
                        functions.Add(funcName, matches[0].Groups[1].Value);
                    }
                }
            }

            return functions;
        }
    }
}