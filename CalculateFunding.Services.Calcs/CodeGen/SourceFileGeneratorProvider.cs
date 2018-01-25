using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.CSharp;
using System;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Services.CodeGeneration.VisualBasic;

namespace CalculateFunding.Services.Calcs.CodeGen
{
    public class SourceFileGeneratorProvider : ISourceFileGeneratorProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public SourceFileGeneratorProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISourceFileGenerator CreateSourceFileGenerator(TargetLanguage targetLanguage)
        {
            ISourceFileGenerator generator = null;
            switch (targetLanguage)
            {
                case TargetLanguage.CSharp:
                    generator = _serviceProvider.GetService<CSharpSourceFileGenerator>();
                    break;
                case TargetLanguage.VisualBasic:
                    generator = _serviceProvider.GetService<VisualBasicSourceFileGenerator>();
                    break;
            }

            //Shouldnt ever happen but if a new language is added but no generator
            if (generator == null)
                throw new NotSupportedException("Target language not supported");

            return generator;
        }
    }

}
