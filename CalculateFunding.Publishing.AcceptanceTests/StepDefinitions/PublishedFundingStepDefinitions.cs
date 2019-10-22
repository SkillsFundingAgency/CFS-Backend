using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using CalculateFunding.Publishing.AcceptanceTests.Properties;
using FluentAssertions;
using Newtonsoft.Json;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishedFundingStepDefinitions
    {
        private readonly IPublishedFundingRepositoryStepContext _publishedFundingRepositoryStepContext;
        private readonly IPublishedFundingResultStepContext _publishedFundingResultStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;

        public PublishedFundingStepDefinitions(IPublishedFundingRepositoryStepContext publishedFundingRepositoryStepContext,
            IPublishedFundingResultStepContext publishedFundingResultStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext)
        {
            Guard.ArgumentNotNull(publishedFundingRepositoryStepContext, nameof(publishedFundingRepositoryStepContext));
            Guard.ArgumentNotNull(publishedFundingResultStepContext, nameof(publishedFundingResultStepContext));
            Guard.ArgumentNotNull(currentSpecificationStepContext, nameof(currentSpecificationStepContext));

            _publishedFundingRepositoryStepContext = publishedFundingRepositoryStepContext;
            _publishedFundingResultStepContext = publishedFundingResultStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
        }

        [Then(@"the following published funding is produced")]
        public async Task ThenTheFollowingPublishedFundingIsProduced(Table table)
        {
            PublishedFundingLookupModel lookupModel = table.CreateInstance<PublishedFundingLookupModel>();

            _publishedFundingRepositoryStepContext.Repo.Should().NotBeNull();

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
            var json = JsonConvert.SerializeObject(publishedFunding);

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

            publishedProviders
                .Select(_ => (_.Id, _.Current.Status))
                .OrderBy(_ => _.Id)
                .Should()
                .BeEquivalentTo(expectedPublishedProviderIds);
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
        public void ThenThePublishedFundingContainsACalculationsInPublishedProviderWithFollowingCalculationResults(Table table)
        {
            PublishedFunding publishedFunding = _publishedFundingResultStepContext.CurrentPublishedFunding;

            publishedFunding.Should()
                .NotBeNull();

            var calculations = publishedFunding.Current.Calculations;

            calculations
                .Should()
                .NotBeNull("Calculations not found");

          
            IEnumerable<CalculationResult> calculationResult = table.CreateSet<CalculationResult>();

           foreach(var cals in calculationResult)
            {
                var calValue = calculations.SingleOrDefault(c => c.TemplateCalculationId.ToString() == cals.Id);
                calValue
                    .Should()
                    .NotBeNull("Calculation value could not be found");
            }
        }

        [Then(@"the published funding document produced is saved to blob storage for following file name")]
        public void ThenThePublishedFundingDocumentProducedIsSavedToBlobStorageForFollowingFileName(Table table)
        {
            var publishedFunding = _publishedFundingRepositoryStepContext.BlobRepo.GetFiles();

            _publishedFundingRepositoryStepContext.BlobRepo.Should().NotBeNull();
            string fileName = table.Rows[0][0];


           var content = Resources.ResourceManager.GetObject(fileName, Resources.Culture);
            string expected = GetResourceContent(fileName);

            publishedFunding.TryGetValue(fileName, out string acutal);
            acutal.Should()
                    .Equals(expected);
        }

        

        private static string GetResourceContent(string fileName)
        {
            byte[] result = null;

            if (fileName == "PSG-AY-1920-Information-Provider-1000201-1_0.json")
            {
                result = Resources.PSG_AY_1920_Information_Provider_1000201_1_0;
            }
            else if (fileName == "PSG-AY-1920-Information-Provider-1000202-1_0.json")
            {
                result = Resources.PSG_AY_1920_Information_Provider_1000202_1_0;
            }
            else if (fileName == "PSG-AY-1920-Payment-AcademyTrust-8000001-1_0.json")
            {
                result = Resources.PSG_AY_1920_Payment_AcademyTrust_8000001_1_0;
            }
            else if (fileName == "PSG-AY-1920-Payment-LocalAuthority-9000000-1_0.json")
            {
                result = Resources.PSG_AY_1920_Payment_LocalAuthority_9000000_1_0;
            }

            return Encoding.UTF8.GetString(result, 0, result.Length);
        }

    }
}
