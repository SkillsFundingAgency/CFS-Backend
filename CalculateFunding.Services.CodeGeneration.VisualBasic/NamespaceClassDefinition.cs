using CalculateFunding.Models.Publishing;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Net.NetworkInformation;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class NamespaceClassDefinition
    {
        public NamespaceClassDefinition(string @namespace,
            ClassBlockSyntax classBlockSyntax,
            string variable = null,
            string suffix = "Calculations")
        {
            Variable = (variable == null ? @namespace : $"{@namespace}.{variable}");
            Namespace = @namespace;
            Suffix = suffix;
            ClassBlockSyntax = classBlockSyntax;
        }

        private string Suffix { get; }

        public string Variable { get; }

        public string Namespace { get; }
        
        public ClassBlockSyntax ClassBlockSyntax { get; }

        public string ClassName => Namespace == Suffix ? $"Additional{Suffix}" : $"{Namespace}{Suffix}";
    }
}