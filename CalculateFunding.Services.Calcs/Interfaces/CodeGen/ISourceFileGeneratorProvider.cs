using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CodeGeneration;

namespace CalculateFunding.Services.Calcs.Interfaces.CodeGen
{
    public interface ISourceFileGeneratorProvider
    {
        ISourceFileGenerator CreateSourceFileGenerator(TargetLanguage targetLanguage);
    }
}
