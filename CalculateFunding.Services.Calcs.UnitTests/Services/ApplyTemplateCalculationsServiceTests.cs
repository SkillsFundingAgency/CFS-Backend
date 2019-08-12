using System;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class ApplyTemplateCalculationsServiceTests
    {
        private ICalculationService _calculationService;
        private IPoliciesApiClient _policies;

        private ApplyTemplateCalculationsService _service;

        [TestInitialize]
        public void SetUp()
        {
            _policies = Substitute.For<IPoliciesApiClient>();
            _calculationService = Substitute.For<ICalculationService>();

            _service = new ApplyTemplateCalculationsService(_calculationService,
                _policies,
                new ResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                Substitute.For<ILogger>());
        }

        private Calculation NewTemplateMappingCalculation(Action<TemplateMappingCalculationBuilder> setUp = null)
        {
            TemplateMappingCalculationBuilder templateMappingCalculationBuilder = new TemplateMappingCalculationBuilder();

            setUp?.Invoke(templateMappingCalculationBuilder);
            
            return templateMappingCalculationBuilder.Build();
        }

        private TemplateMapping NewTemplateMapping(Action<TemplateMappingBuilder> setUp = null)
        {
            TemplateMappingBuilder templateMappingBuilder = new TemplateMappingBuilder();
            
            setUp?.Invoke(templateMappingBuilder);

            return templateMappingBuilder.Build();
        }
        
        
        
        
        
    }

    public class TemplateMappingBuilder : TestEntityBuilder
    {
        public TemplateMapping Build()
        {
            return new TemplateMapping();
        }    
    }

    public class TemplateMappingCalculationBuilder : TestEntityBuilder
    {
        public Calculation Build()
        {
            return new Calculation();
        }
    }
    
}