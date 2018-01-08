using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Compiler.CSharp;
using CalculateFunding.Services.Compiler.VisualBasic;

namespace CalculateFunding.Services.Compiler
{
    public class SourceFileGeneratorFactory
    {
        private readonly Dictionary<TargetLanguage, ISourceFileGenerator> _generators = new Dictionary<TargetLanguage, ISourceFileGenerator>();

        public SourceFileGeneratorFactory(CSharpSourceFileGenerator cSharpGenerator, VisualBasicSourceFileGenerator visualBasicGenerator)
        {
            _generators.Add(TargetLanguage.CSharp, cSharpGenerator);
            _generators.Add(TargetLanguage.VisualBasic, visualBasicGenerator);
        }

        public ISourceFileGenerator GetCompiler(TargetLanguage targetLanguage)
        {

            if (_generators.TryGetValue(targetLanguage, out var generator))
            {
                return generator;
            }
            
            throw new NotImplementedException($"No supported project file included (must be one of {string.Join(",", _generators.Keys.Select(x => x.ToString()))})");
        }
    }
}