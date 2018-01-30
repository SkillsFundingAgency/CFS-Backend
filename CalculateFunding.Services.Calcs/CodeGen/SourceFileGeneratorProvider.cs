using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.CSharp;
using System;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using Serilog;

namespace CalculateFunding.Services.Calcs.CodeGen
{
    public class SourceFileGeneratorProvider : ISourceFileGeneratorProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public SourceFileGeneratorProvider(IServiceProvider serviceProvider, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
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
            {
                _logger.Error("An invalid language type was provided");

                throw new NotSupportedException("Target language not supported");
            }

            _logger.Information($"Generating {targetLanguage.ToString()} source files");

            return generator;
        }
    }

}
