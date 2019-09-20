using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.Funding;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;
using ApiCalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineTotalAggregator : IFundingLineTotalAggregator
    {
        private readonly FundingGenerator _fundingGenerator;
        private readonly IMapper _mapper;

        public FundingLineTotalAggregator(IMapper mapper)
        {
            _fundingGenerator = new FundingGenerator();
            _mapper = mapper;
        }

        public GeneratorModels.FundingValue GenerateTotals(TemplateModels.TemplateMetadataContents templateMetadataContents, TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));
            Guard.ArgumentNotNull(mapping, nameof(mapping));
            Guard.ArgumentNotNull(calculationResults, nameof(calculationResults));

            templateMetadataContents.RootFundingLines = templateMetadataContents.RootFundingLines?.Select(_ => ToFundingLine(_, mapping, calculationResults));

            IEnumerable<GeneratorModels.FundingLine> fundingLines = _mapper.Map<IEnumerable<GeneratorModels.FundingLine>>(templateMetadataContents.RootFundingLines);

            return _fundingGenerator.GenerateFundingValue(fundingLines);
        }

        private Common.TemplateMetadata.Models.FundingLine ToFundingLine(Common.TemplateMetadata.Models.FundingLine fundingLine, TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            fundingLine.Calculations = fundingLine.Calculations?.Select(_ => ToCalculation(_, mapping, calculationResults));

            fundingLine.FundingLines = fundingLine.FundingLines?.Select(_ => ToFundingLine(_, mapping, calculationResults));

            return fundingLine;
        }

        private Common.TemplateMetadata.Models.Calculation ToCalculation(Common.TemplateMetadata.Models.Calculation calculation, TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            CalculationResult calculationResult = calculationResults.SingleOrDefault(calc => calc.Id == GetCalculationId(mapping, calculation.TemplateCalculationId));
            decimal? calculationResultValue = calculationResult?.Value;
            
            calculation.Value = calculation.Type == ApiCalculationType.Cash && calculationResultValue.HasValue 
                ? (object) Math.Round(calculationResultValue.Value, 2, MidpointRounding.AwayFromZero) 
                :  calculationResultValue;

            calculation.Calculations = calculation.Calculations?.Select(_ => ToCalculation(_, mapping, calculationResults));

            return calculation;
        }

        private string GetCalculationId(TemplateMapping mapping, uint templateId)
        {
            return mapping.TemplateMappingItems.SingleOrDefault(_ => _.TemplateId == templateId)?.CalculationId;
        }
    }
}
