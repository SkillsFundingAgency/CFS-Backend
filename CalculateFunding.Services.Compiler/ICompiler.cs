using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Compiler
{
    public interface ICompiler
    {
        BudgetCompilerOutput Compile(Budget budget);
        string GetIdentifier(string name);
    }
}
