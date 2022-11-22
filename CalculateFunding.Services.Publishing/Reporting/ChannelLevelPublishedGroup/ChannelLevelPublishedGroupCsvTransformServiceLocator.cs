using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public class ChannelLevelPublishedGroupCsvTransformServiceLocator : IFundingLineCsvTransformServiceLocator
    {
        private readonly IEnumerable<IFundingLineCsvTransform> _transforms;

        public ChannelLevelPublishedGroupCsvTransformServiceLocator(IEnumerable<IFundingLineCsvTransform> transforms)
        {
            Guard.IsNotEmpty(transforms, nameof(transforms));

            _transforms = transforms.ToArray();
        }

        public IFundingLineCsvTransform GetService(FundingLineCsvGeneratorJobType jobType)
        {
            return _transforms.SingleOrDefault(_ => _.IsForJobType(jobType)) ?? throw new ArgumentOutOfRangeException();
        }
    }
}
