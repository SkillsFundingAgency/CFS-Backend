using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calcs.Interfaces.CodeGen
{
    public interface ISourceFileGeneratorProvider
    {
        ISourceFileGenerator CreateSourceFileGenerator(TargetLanguage targetLanguage);
    }
}
