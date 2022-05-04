using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Linq;
using TemplateFundingLineType = CalculateFunding.Common.TemplateMetadata.Enums.FundingLineType;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using Serilog;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing
{
    public class InformationLinesAggregationService : IInformationLinesAggregationService
    {
        private readonly IInformationLineAggregator _informationLineAggregator;
        private readonly ILogger _logger;

        public InformationLinesAggregationService(IInformationLineAggregator informationLineAggregator, ILogger logger)
        {
            _informationLineAggregator = informationLineAggregator;
            _logger = logger;
        }

        public void AggregateFundingLines(string specificationId, string providerId, IEnumerable<FundingLine> fundingLines, IEnumerable<TemplateFundingLine> templateFundingLines)
        {
            if (templateFundingLines.AnyWithNullCheck())
            {
                foreach (TemplateFundingLine rootFundingLine in templateFundingLines.Where(_ => _.Type == TemplateFundingLineType.Information))
                {
                    IDictionary<uint, FundingLine> fundingLinesDictionary = fundingLines?.ToDictionary(_ => _.TemplateLineId);
                    if (fundingLinesDictionary != null)
                    {
                        ProfilePeriod[] profilePeriods = _informationLineAggregator.Sum(rootFundingLine, fundingLinesDictionary);

                        if (profilePeriods.AnyWithNullCheck())
                        {
                            if (fundingLinesDictionary.ContainsKey(rootFundingLine.TemplateLineId))
                            {
                                FundingLine fundingLine = fundingLinesDictionary[rootFundingLine.TemplateLineId];

                                foreach (IGrouping<string, ProfilePeriod> profilePerioGrouping in profilePeriods.GroupBy(_ => _.DistributionPeriodId))
                                {
                                    DistributionPeriod distributionPeriod = new DistributionPeriod { DistributionPeriodId = profilePerioGrouping.Key };
                                    distributionPeriod.ProfilePeriods = profilePerioGrouping;
                                    distributionPeriod.Value = profilePerioGrouping.Sum(_ => _.ProfiledValue);
                                    fundingLine.DistributionPeriods = fundingLine.DistributionPeriods.AnyWithNullCheck() ? fundingLine.DistributionPeriods.Concat(new[] { distributionPeriod }) : new[] { distributionPeriod };
                                }

                                fundingLine.Value = fundingLine.DistributionPeriods.Sum(_ => _.Value);

                                _logger.Information("Aggregation calculated for specification:{specificationId}, provider:{providerId}, funding line:{templateLineId} and value:{fundingLineJson}", specificationId, providerId, rootFundingLine.TemplateLineId, fundingLinesDictionary[rootFundingLine.TemplateLineId].AsJson());
                            }
                        }
                    }
                }
            }
        }
    }
}
