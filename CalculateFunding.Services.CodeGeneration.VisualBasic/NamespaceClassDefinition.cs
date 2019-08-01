using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class NamespaceClassDefinition
    {
        public NamespaceClassDefinition(string @namespace,
            ClassBlockSyntax classBlockSyntax)
        {
            Namespace = @namespace;
            ClassBlockSyntax = classBlockSyntax;
        }

        public string Namespace { get; }
        
        public ClassBlockSyntax ClassBlockSyntax { get; }

        public string ClassName => Namespace == "Calculations" ? "AdditionalCalculations" : $"{Namespace}Calculations";
    }
}