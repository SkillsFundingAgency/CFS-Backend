using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
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
        private readonly ILogger _logger;

        public PublishedFundingCsvJobsService(IPublishedFundingDataService publishedFundingDataService,
            IGeneratePublishedFundingCsvJobsCreationLocator generateCsvJobsLocator,
            ISpecificationService specificationService, ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(generateCsvJobsLocator, nameof(generateCsvJobsLocator));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedFundingDataService = publishedFundingDataService;
            _generateCsvJobsLocator = generateCsvJobsLocator;
            _specificationService = specificationService;
            _logger = logger;
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

            _logger.Information($"Queuing csv job create action type '{createActionType}' for specification Id '{specificationId}'. Correlation id = '{correlationId}'.");

            return await GenerateCsvJobs(createActionType,
                    specification.Id,
                    specification.FundingPeriod.Id,
                    specification.FundingStreams.Select(_ => _.Id),
                    correlationId,
                    author);
        }

        public async Task<IEnumerable<Job>> GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction createActionType, string specificationId, string fundingPeriodId, IEnumerable<string> fundingStreamIds, string correlationId, Reference author)
        {
            IGeneratePublishedFundingCsvJobsCreation generateCsvJobs = _generateCsvJobsLocator
                    .GetService(createActionType);
            IEnumerable<(string Code, string Name)> fundingLines = await _publishedFundingDataService.GetPublishedProviderFundingLines(specificationId);
            PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest = new PublishedFundingCsvJobsRequest
            {
                SpecificationId = specificationId,
                CorrelationId = correlationId,
                User = author,
                FundingLines = fundingLines,
                FundingStreamIds = fundingStreamIds ?? Array.Empty<string>(),
                FundingPeriodId = fundingPeriodId
            };
            return await generateCsvJobs.CreateJobs(publishedFundingCsvJobsRequest);
        }
    }
}
