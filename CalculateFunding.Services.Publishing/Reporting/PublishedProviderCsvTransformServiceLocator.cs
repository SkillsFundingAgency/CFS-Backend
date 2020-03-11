using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class PublishedProviderCsvTransformServiceLocator : IPublishedProviderCsvTransformServiceLocator
    {
        private readonly IEnumerable<IPublishedProviderCsvTransform> _transforms;

        public PublishedProviderCsvTransformServiceLocator(IEnumerable<IPublishedProviderCsvTransform> transforms)
        {
            Guard.IsNotEmpty(transforms, nameof(transforms));

            _transforms = transforms.ToArray();
        }

        public IPublishedProviderCsvTransform GetService(string jobDefinitionName)
        {
            return _transforms.SingleOrDefault(_ => _.IsForJobDefinition(jobDefinitionName)) ?? throw new ArgumentOutOfRangeException();
        }
    }
}
