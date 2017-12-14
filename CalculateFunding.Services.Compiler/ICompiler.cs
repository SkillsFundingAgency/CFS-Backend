using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Compiler
{
    public interface ICompiler
    {
        CompilerOutput GenerateCode(Implementation implementation);
        string GetIdentifier(string name);
    }
}
