using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Compiler
{
    public interface ICompiler
    {
        Build GenerateCode(Implementation implementation);
        string GetIdentifier(string name);
    }
}
