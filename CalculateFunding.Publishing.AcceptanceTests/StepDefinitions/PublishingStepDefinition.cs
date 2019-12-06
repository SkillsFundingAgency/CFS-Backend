using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishingStepDefinition
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly CurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly CurrentUserStepContext _currentUserStepContext;

        public PublishingStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
        }


        [Given(@"template mapping exists")]
        public void GivenTemplateMappingExists(Table table)
        {
            IEnumerable<TemplateMappingItem> templateMappingItems = table.CreateSet<TemplateMappingItem>();

            _publishFundingStepContext.TemplateMapping.TemplateMappingItems = templateMappingItems;
        }

        [Given(@"calculation meta data exists for '(.*)'")]
        public void GivenCalculationMetaDataExistsFor(string fundingStreamId, Table table)
        {
            IEnumerable<CalculationMetadata> calculationMetadata = table.CreateSet<CalculationMetadata>();

            foreach (CalculationMetadata calculation in calculationMetadata)
            {
                calculation.SpecificationId = _currentSpecificationStepContext.SpecificationId;
            }

            _publishFundingStepContext.CalculationMetadata = calculationMetadata.Select(_ =>
            {
                _.FundingStreamId = fundingStreamId;
                return _;
            });
        }

        [Given(@"calculations exists")]
        public void GivenCalculationsExists(Table table)
        {
            _publishFundingStepContext.CalculationResults = table.CreateSet<CalculationResult>();
        }

        [Given(@"the following distribution periods exist")]
        public void GivenTheFollowingDistributionPeriodsExist(Table table)
        {
            _publishFundingStepContext.DistributionPeriods = table.Rows.Select(_ => new Common.ApiClient.Profiling.Models.DistributionPeriods { DistributionPeriodCode = _[0], Value = decimal.Parse(_[1]) }) ;
        }

        [Given(@"the following profiles exist")]
        public void GivenTheFollowingProfilesExist(Table table)
        {
            _publishFundingStepContext.DistributionPeriods.ToList().ForEach(_ =>
            {
                _publishFundingStepContext.ProfilingPeriods = table.Rows.Where(row => row[0] == _.DistributionPeriodCode).Select(row => new Common.ApiClient.Profiling.Models.ProfilingPeriod { DistributionPeriod = _.DistributionPeriodCode, Type = row[1], Period = row[2], Year = Convert.ToInt16(row[3]), Occurrence = Convert.ToInt16(row[4]), Value = decimal.Parse(row[5]) });
            });
        }


        [When(@"funding is published")]
        public async Task WhenFundingIsPublished()
        {
            try
            {
                await _publishFundingStepContext.PublishFunding(
                _currentSpecificationStepContext.SpecificationId,
                _currentJobStepContext.JobId,
                _currentUserStepContext.UserId,
                _currentUserStepContext.UserName);
            }
            catch (Exception)
            {
                _publishFundingStepContext.PublishSuccessful = false;
                throw;
            }
            _publishFundingStepContext.PublishSuccessful = true;

        }

        [Then(@"publishing succeeds")]
        public void ThenPublishingSucceeds()
        {
            _publishFundingStepContext.PublishSuccessful
                .Should()
                .BeTrue();
        }
    }
}
