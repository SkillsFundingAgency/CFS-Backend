using System.Collections.Generic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class NamespaceBuilderResult
    {
        public ICollection<NamespaceClassDefinition> InnerClasses { get; } = new List<NamespaceClassDefinition>();

        public IEnumerable<StatementSyntax> PropertiesDefinitions { get; set; }

        public IEnumerable<StatementSyntax> EnumsDefinitions { get; set; }
    }
}