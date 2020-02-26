using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class FundingLineCsvTransformServiceLocator : IFundingLineCsvTransformServiceLocator
    {
        private readonly IEnumerable<IFundingLineCsvTransform> _transforms;

        public FundingLineCsvTransformServiceLocator(IEnumerable<IFundingLineCsvTransform> transforms)
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