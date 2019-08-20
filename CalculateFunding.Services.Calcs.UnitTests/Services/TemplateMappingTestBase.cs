using System;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Calcs.Services
{
    public abstract class TemplateMappingTestBase
    {
        protected string NewRandomString() => new RandomString();

        protected static Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);
            
            return calculationBuilder.Build();
        }

        protected static CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationVersionBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationVersionBuilder);

            return calculationVersionBuilder.Build();
        }

        protected static TemplateCalculation NewTemplateMappingCalculation(Action<TemplateMappingCalculationBuilder> setUp = null)
        {
            TemplateMappingCalculationBuilder templateMappingCalculationBuilder = new TemplateMappingCalculationBuilder();

            setUp?.Invoke(templateMappingCalculationBuilder);

            return templateMappingCalculationBuilder.Build();
        }

        protected static TemplateMapping NewTemplateMapping(Action<TemplateMappingBuilder> setUp = null)
        {
            TemplateMappingBuilder templateMappingBuilder = new TemplateMappingBuilder();

            setUp?.Invoke(templateMappingBuilder);

            return templateMappingBuilder.Build();
        }

        protected static TemplateMappingItem NewTemplateMappingItem(Action<TemplateMappingItemBuilder> setUp = null)
        {
            TemplateMappingItemBuilder templateMappingItemBuilder = new TemplateMappingItemBuilder();

            setUp?.Invoke(templateMappingItemBuilder);
            
            return templateMappingItemBuilder.Build();
        }

        protected static TemplateMetadataContents NewTemplateMetadataContents(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder templateMetadataContentsBuilder = new TemplateMetadataContentsBuilder();
            
            setUp?.Invoke(templateMetadataContentsBuilder);

            return templateMetadataContentsBuilder.Build();
        }

        protected static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();
            
            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }
    }
}