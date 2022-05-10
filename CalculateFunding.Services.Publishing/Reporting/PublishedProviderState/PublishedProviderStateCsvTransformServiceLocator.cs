using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public class PublishedProviderStateCsvTransformServiceLocator : IPublishedProviderStateSummaryCsvTransformServiceLocator
    {
        private readonly IEnumerable<IPublishedProviderStateSummaryCsvTransform> _transforms;

        public PublishedProviderStateCsvTransformServiceLocator(IEnumerable<IPublishedProviderStateSummaryCsvTransform> transforms)
        {
            Guard.IsNotEmpty(transforms, nameof(transforms));

            _transforms = transforms.ToArray();
        }

        public IPublishedProviderStateSummaryCsvTransform GetService(string jobDefinitionName)
        {
            return _transforms.SingleOrDefault(_ => _.IsForJobDefinition(jobDefinitionName)) ?? throw new ArgumentOutOfRangeException();
        }
    }
}
