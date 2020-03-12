using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class FundingLineCsvBatchProcessorServiceLocator : IFundingLineCsvBatchProcessorServiceLocator
    {
        private readonly IEnumerable<IFundingLineCsvBatchProcessor> _batchProcessors;
        public FundingLineCsvBatchProcessorServiceLocator(IEnumerable<IFundingLineCsvBatchProcessor> batchProcessors)
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