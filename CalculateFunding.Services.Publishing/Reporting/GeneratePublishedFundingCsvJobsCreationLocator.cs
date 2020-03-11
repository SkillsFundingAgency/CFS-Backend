using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GeneratePublishedFundingCsvJobsCreationLocator : IGeneratePublishedFundingCsvJobsCreationLocator
    {
        private readonly IEnumerable<IGeneratePublishedFundingCsvJobsCreation> _creations;
        public GeneratePublishedFundingCsvJobsCreationLocator(IEnumerable<IGeneratePublishedFundingCsvJobsCreation> creations)
        {
            Guard.ArgumentNotNull(creations, nameof(creations));

            _creations = creations;
        }

        public IGeneratePublishedFundingCsvJobsCreation GetService(GeneratePublishingCsvJobsCreationAction action)
        {
            return _creations.SingleOrDefault(_ => _.IsForAction(action)) ?? throw new ArgumentOutOfRangeException();

        }
    }
}
