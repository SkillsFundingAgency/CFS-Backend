using CalculateFunding.Models.Specs;

namespace Allocations.Services.Compiler
{
    public interface ICompiler
    {
        BudgetCompilerOutput Compile(Budget budget);
        string GetIdentifier(string name);
    }
}
