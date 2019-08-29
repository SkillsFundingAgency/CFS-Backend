using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.Funding;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineTotalAggregator : IFundingLineTotalAggregator
    {
        private readonly FundingGenerator _fundingGenerator;
        private readonly IMapper _mapper;

        public FundingLineTotalAggregator(IMapper mapper)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _fundingGenerator = new FundingGenerator();
            _mapper = mapper;
        }

        public IEnumerable<Models.Publishing.FundingLine> GenerateTotals(TemplateModels.TemplateMetadataContents templateMetadataContents, TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            templateMetadataContents.RootFundingLines = templateMetadataContents.RootFundingLines?.Select(_ => ToFundingLine(_, mapping, calculationResults));

            IEnumerable<GeneratorModels.FundingLine> fundingLines = _mapper.Map<IEnumerable<GeneratorModels.FundingLine>>(templateMetadataContents.RootFundingLines);

            GeneratorModels.FundingValue fundingValue = _fundingGenerator.GenerateFundingValue(fundingLines);

            return _mapper.Map<IEnumerable<FundingLine>>(fundingValue.FundingLines.Flatten(_ => _.FundingLines) ?? new GeneratorModels.FundingLine[0]);
        }

        private Common.TemplateMetadata.Models.FundingLine ToFundingLine(Common.TemplateMetadata.Models.FundingLine fundingLine, TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            fundingLine.Calculations = fundingLine.Calculations?.Select(_ => ToCalculation(_, mapping, calculationResults));

            fundingLine.FundingLines = fundingLine.FundingLines?.Select(_ => ToFundingLine(_, mapping, calculationResults));

            return fundingLine;
        }

        private Common.TemplateMetadata.Models.Calculation ToCalculation(Common.TemplateMetadata.Models.Calculation calculation, TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            calculation.Value = calculationResults.SingleOrDefault(calc => calc.Id == GetCalculationId(mapping, calculation.TemplateCalculationId))?.Value;

            calculation.Calculations = calculation.Calculations?.Select(_ => ToCalculation(_, mapping, calculationResults));

            return calculation;
        }

        private string GetCalculationId(TemplateMapping mapping, uint templateId)
        {
            return mapping.TemplateMappingItems.SingleOrDefault(_ => _.TemplateId == templateId)?.CalculationId;
        }
    }
}
