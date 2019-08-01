using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public interface IBuildCalculationNamespaces
    {
        IEnumerable<NamespaceClassDefinition> BuildNamespacesForCalculations(IEnumerable<Calculation> calculations);
    }
}