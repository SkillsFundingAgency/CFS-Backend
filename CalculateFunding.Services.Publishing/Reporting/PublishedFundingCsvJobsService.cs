using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class PublishedFundingCsvJobsService : IPublishedFundingCsvJobsService
    {
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;
        private readonly ISpecificationService _specificationService;

        public PublishedFundingCsvJobsService(IPublishedFundingDataService publishedFundingDataService,
            IGeneratePublishedFundingCsvJobsCreationLocator generateCsvJobsLocator,
            ISpecificationService specificationService)
        {
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(generateCsvJobsLocator, nameof(generateCsvJobsLocator));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));

            _publishedFundingDataService = publishedFundingDataService;
            _generateCsvJobsLocator = generateCsvJobsLocator;
            _specificationService = specificationService;
        }

        public async Task<IEnumerable<Job>> QueueCsvJobs(GeneratePublishingCsvJobsCreationAction createActionType, string specificationId, string correlationId, Reference author)
        {
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new Exception($"Could not find specification with id '{specificationId}'");
            }

            if (!specification.IsSelectedForFunding)
            {
                throw new Exception($"Specification with id '{specificationId}' is not chosen for funding.");
            }

            return await GenerateCsvJobs(createActionType,
                    specification.Id,
                    specification.FundingPeriod.Id,
                    correlationId,
                    author,
                    createActionType == GeneratePublishingCsvJobsCreationAction.Release ? 
                        specification.FundingStreams.Select(_ => _.Id) : 
                        null);
        }

        public async Task<IEnumerable<Job>> GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction createActionType, string specificationId, string fundingPeriodId, string correlationId, Reference author, IEnumerable<string> fundingStreamIds = null)
        {
            IGeneratePublishedFundingCsvJobsCreation generateCsvJobs = _generateCsvJobsLocator
                    .GetService(createActionType);
            IEnumerable<string> fundingLineCodes = await _publishedFundingDataService.GetPublishedProviderFundingLines(specificationId);
            PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest = new PublishedFundingCsvJobsRequest
            {
                SpecificationId = specificationId,
                CorrelationId = correlationId,
                User = author,
                FundingLineCodes = fundingLineCodes,
                FundingStreamIds = fundingStreamIds ?? Array.Empty<string>(),
                FundingPeriodId = fundingPeriodId
            };
            return await generateCsvJobs.CreateJobs(publishedFundingCsvJobsRequest);
        }
    }
}
