using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting.ChannelLevelPublishedGroup
{
    public class ChannelLevelPublishedGroupsCsvBatchProcessorServiceLocator : IChannelLevelPublishedGroupsCsvBatchProcessorServiceLocator
    {
        private readonly IEnumerable<IFundingLineCsvBatchProcessor> _batchProcessors;
        public ChannelLevelPublishedGroupsCsvBatchProcessorServiceLocator(IEnumerable<IFundingLineCsvBatchProcessor> batchProcessors)
        {
            Guard.IsNotEmpty(batchProcessors, nameof(batchProcessors));

            _batchProcessors = batchProcessors.ToArray();
        }

        public IFundingLineCsvBatchProcessor GetService(FundingLineCsvGeneratorJobType jobType)
        {
            return _batchProcessors.SingleOrDefault(_ => _.IsForJobType(jobType)) ?? throw new ArgumentOutOfRangeException();
        }
    }
}
