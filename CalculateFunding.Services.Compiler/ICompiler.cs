using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Compiler
{
    public interface ICompiler
    {
        BudgetCompilerOutput GenerateCode(Budget budget);
        string GetIdentifier(string name);
    }
}
