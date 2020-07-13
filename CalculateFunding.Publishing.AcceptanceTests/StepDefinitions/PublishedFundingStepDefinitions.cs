using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using CalculateFunding.Publishing.AcceptanceTests.Properties;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishedFundingStepDefinitions
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly IPublishedFundingRepositoryStepContext _publishedFundingRepositoryStepContext;
        private readonly IPublishedFundingResultStepContext _publishedFundingResultStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly ICurrentCorrelationStepContext _currentCorrelationStepContext;

        public PublishedFundingStepDefinitions(IPublishFundingStepContext publishFundingStepContext,
            IPublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext,
            IPublishedFundingResultStepContext publishedFundingResultStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            ICurrentCorrelationStepContext currentCorrelationStepContext)
        {
            Guard.ArgumentNotNull(publishedFundingRepositoryStepContext, nameof(publishedFundingRepositoryStepContext));
            Guard.ArgumentNotNull(publishedFundingResultStepContext, nameof(publishedFundingResultStepContext));
            Guard.ArgumentNotNull(currentSpecificationStepContext, nameof(currentSpecificationStepContext));

            _publishFundingStepContext = publishFundingStepContext;
            _publishedFundingRepositoryStepContext = publishedFundingRepositoryStepContext;
            _publishedFundingResultStepContext = publishedFundingResultStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentCorrelationStepContext = currentCorrelationStepContext;
        }

        [Then(@"the following published funding is produced")]
        public async Task ThenTheFollowingPublishedFundingIsProduced(Table table)
        {
            PublishedFundingLookupModel lookupModel = table.CreateInstance<PublishedFundingLookupModel>();

            _publishedFundingRepositoryStepContext.Repo.Should().NotBeNull();
            _publishedFundingResultStepContext.Should().NotBeNull();

            string fundingId = $"funding-{lookupModel.FundingStreamId}-{lookupModel.FundingPeriodId}-{lookupModel.GroupingReason}-{lookupModel.OrganisationGroupTypeCode}-{lookupModel.OrganisationGroupIdentifierValue}";

            PublishedFunding publishedFunding = await _publishedFundingRepositoryStepContext.Repo
                .GetPublishedFundingById(fundingId, "partitionNotUesd");

            publishedFunding
                .Should()
                .NotBeNull("Published funding not found for ID '{0}'", fundingId);

            _publishedFundingResultStepContext.CurrentPublishedFunding = publishedFunding;
        }

        [Then(@"the published funding contains the following published provider ids")]
        public void ThenThePublishedFundingContainsTheFollowingPublishedProviderIds(Table table)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            List<string> expectedPublishedProviderIds = new List<string>();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                expectedPublishedProviderIds.Add(table.Rows[i][0]);
            }

            publishedFunding
                .Current
                .ProviderFundings
                .Should()
                .BeEquivalentTo(expectedPublishedProviderIds);
        }

        [Then(@"the following published provider ids are upserted")]
        public async Task ThenTheFollowingPublishedProviderIdsAreUpserted(Table table)
        {
            IEnumerable<PublishedProvider> publishedProviders = await _publishedFundingRepositoryStepContext.Repo
                .GetLatestPublishedProvidersBySpecification(_currentSpecificationStepContext.SpecificationId);

            List<(string, PublishedProviderStatus)> expectedPublishedProviderIds = new List<(string, PublishedProviderStatus)>();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                expectedPublishedProviderIds.Add((table.Rows[i][0], table.Rows[i][1].AsEnum<PublishedProviderStatus>()));
            }

            (string Id, PublishedProviderStatus Status)[] actualProviderIdsAndStatus = publishedProviders
                .Select(_ => (_.Id, _.Current.Status))
                .OrderBy(_ => _.Id)
                .ToArray();

            actualProviderIdsAndStatus
                .Should()
                .BeEquivalentTo(expectedPublishedProviderIds);

            publishedProviders
                .Where(_ => _.Current.Status == PublishedProviderStatus.Approved || _.Current.Status == PublishedProviderStatus.Updated || _.Current.Status == PublishedProviderStatus.Draft)
                .Select(_ => (_.Released))
                .First()
                .Should()
                .BeNull();

            CalculationInMemoryRepository calculationsInMemoryRepository = _publishFundingStepContext.CalculationsInMemoryRepository;

            IDictionary<string, IEnumerable<CalculationResult>> providerCalculationResults = calculationsInMemoryRepository.ProviderResults;

            //TODO: remove the deep chaining
            publishedProviders
                .ToList().ForEach(_ =>
                {
                    _.Current.Calculations.Select(calc => new CalculationResult
                    {
                        Id = _publishFundingStepContext.CalculationsInMemoryClient.Mapping.TemplateMappingItems.FirstOrDefault(cm => cm.TemplateId == calc.TemplateCalculationId)?.CalculationId,
                        Value = decimal.Parse(calc.Value.ToString())
                    })
                    .Should()
                    .BeEquivalentTo(providerCalculationResults.ContainsKey(_.Current.ProviderId) ?
                        providerCalculationResults[_.Current.ProviderId] :
                        calculationsInMemoryRepository.Results);

                    // Already approved one's will not be re-approved / saved, so jobid or correleationid will not be populated for them
                    if (_.Current.JobId != null)
                    {
                        _.Current.JobId.Should().Be(_currentJobStepContext.JobId);
                        _.Current.CorrelationId.Should().Be(_currentCorrelationStepContext.CorrelationId);
                    }
                });
            //TODO: replace the branching in an assertion with a different step definition
        }

        [Then(@"the following released published provider ids are upserted")]
        public async Task ThenTheFollowingReleasedPublishedProviderIdsAreUpserted(Table table)
        {
            IEnumerable<PublishedProvider> publishedProviders = await _publishedFundingRepositoryStepContext.Repo
                 .GetLatestPublishedProvidersBySpecification(_currentSpecificationStepContext.SpecificationId);

            List<(string, PublishedProviderStatus)> expectedPublishedProviderIds = new List<(string, PublishedProviderStatus)>();

            foreach (var row in table.Rows)
            {
                expectedPublishedProviderIds.Add((row[0], row[1].AsEnum<PublishedProviderStatus>()));
            }

            publishedProviders
                .Select(_ => (_.Id, _.Current.Status))
                .OrderBy(_ => _.Id)
                .Should()
                .BeEquivalentTo(expectedPublishedProviderIds);

            publishedProviders
                .Where(_ => _.Current.Status == PublishedProviderStatus.Released)
                .Select(_ => (_.Released))
                .First()
                .Should()
                .NotBeNull();

        }


        [Then(@"the following funding lines are set against provider with id '(.*)'")]
        public async Task ThenTheFollowingFundingLinesAreSetAgainstProviderWithId(string providerId, Table table)
        {
            IEnumerable<PublishedProvider> publishedProviders = await _publishedFundingRepositoryStepContext.Repo
                .GetLatestPublishedProvidersBySpecification(_currentSpecificationStepContext.SpecificationId);

            IEnumerable<(string FundingLineCode, decimal? Value)> expectedFundingLines = table.Rows.Select(_ => (_[0], (decimal?)decimal.Parse(_[1])));

            IEnumerable<(string FundingLineCode, decimal? Value)> actualFundingLines
                = publishedProviders.FirstOrDefault(_ => _.Current.ProviderId == providerId)?.Current.FundingLines.Select(_ => (_.FundingLineCode, _.Value));

            actualFundingLines
                .Should()
                .NotBeNullOrEmpty();

            actualFundingLines
            .Should()
            .BeEquivalentTo(expectedFundingLines);
        }

        [Then(@"the total funding is '(.*)'")]
        public void ThenTheTotalFundingIs(decimal expectedTotalFunding)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            publishedFunding
                .Current
                .TotalFunding
                .Should()
                .Be(expectedTotalFunding);
        }

        [Then(@"the published funding contains a distribution period in funding line '(.*)' with id of '(.*)' has the value of '(.*)'")]
        public void ThenTheDistributionPeriodInFundingLineWithIdOfHasTheValueOf(string fundingLineCode, string distributionPeriodId, decimal expectedValue)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            var fundingLine = publishedFunding.Current.FundingLines.SingleOrDefault(c => c.FundingLineCode == fundingLineCode);

            fundingLine
                .Should()
                .NotBeNull("funding line not found");

            var distributionPeriod = fundingLine.DistributionPeriods.SingleOrDefault(c => c.DistributionPeriodId == distributionPeriodId);
            distributionPeriod
                .Should()
                .NotBeNull("distribution period could not be found");

            distributionPeriod
                .Value
                .Should()
                .Be(expectedValue);
        }

        [Then(@"the published funding contains a distribution period in funding line '(.*)' with id of '(.*)' has the following profiles")]
        public void ThenThePublishedFundingContainsADistributionPeriodInFundingLineWithIdOfHasTheFollowingProfiles(string fundingLineCode, string distributionPeriodId, Table table)
        {

            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            var fundingLine = publishedFunding.Current.FundingLines.SingleOrDefault(c => c.FundingLineCode == fundingLineCode);

            fundingLine
                .Should()
                .NotBeNull("funding line not found");

            var distributionPeriod = fundingLine.DistributionPeriods.SingleOrDefault(c => c.DistributionPeriodId == distributionPeriodId);
            distributionPeriod
                .Should()
                .NotBeNull("distribution period could not be found");

            IEnumerable<ProfilePeriod> periods = table.CreateSet<ProfilePeriod>();

            distributionPeriod
                .ProfilePeriods
                .Should()
                .BeEquivalentTo(periods);
        }

        [Then(@"the published funding contains a calculations in published provider with following calculation results")]
        public void ThenThePublishedFundingContainsACalculationsInPublishedProviderWithFollowingCalculationResults(IEnumerable<CalculationResult> calculationResult)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            IEnumerable<FundingCalculation> calculations = publishedFunding.Current.Calculations;

            calculations
                .Should()
                .NotBeNull("Calculations not found");

            foreach (CalculationResult cals in calculationResult)
            {
                FundingCalculation calValue = calculations.FirstOrDefault(c => c.TemplateCalculationId.ToString() == cals.Id);

                calValue
                    .Should()
                    .NotBeNull("Calculation value could not be found");

                calValue.Value
                    .Should()
                    .Be(cals.Value);
            }
        }

        [Then(@"the published funding document produced is saved to blob storage for following file name")]
        public void ThenThePublishedFundingDocumentProducedIsSavedToBlobStorageForFollowingFileName(Table table)
        {
            var publishedFunding = _publishedFundingRepositoryStepContext.BlobRepo.GetFiles();

            _publishedFundingRepositoryStepContext.BlobRepo.Should().NotBeNull();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                string fileName = table.Rows[i][0];

                string expected = GetResourceContent(fileName);

                publishedFunding.TryGetValue(fileName, out string actual);
                actual.Should()
                        .Equals(expected);
            }
        }

        [Then(@"the published funding document produced has following metadata")]
        public void ThenThePublishedFundingDocumentHasMetadata(Table table)
        {
            _publishedFundingRepositoryStepContext.BlobRepo.Should().NotBeNull();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                string fileName = table.Rows[i][0];
                string metadataKey = table.Rows[i][1];
                string metadataValue = table.Rows[i][2];

                ICloudBlob cloudBlob = _publishedFundingRepositoryStepContext.BlobRepo.GetBlockBlobReference(fileName);

                cloudBlob.Metadata.TryGetValue(metadataKey, out string actual);
                actual.Should().Equals(metadataValue);
            }
        }

        private static string GetResourceContent(string fileName)
        {
            Guard.IsNullOrWhiteSpace(fileName, nameof(fileName));

            string resourceName = $"CalculateFunding.Publishing.AcceptanceTests.Resources.PublishedFunding.{fileName}";

            string result = typeof(PublishedFundingStepDefinitions)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException($"Unable to find resource for filename '{fileName}'");
            }

            return result;
        }
    }
}
