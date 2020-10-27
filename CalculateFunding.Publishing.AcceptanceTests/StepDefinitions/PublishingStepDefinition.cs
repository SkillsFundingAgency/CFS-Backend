using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Extensions;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
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
        private readonly IPublishService _publishService;

        public PublishingStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext, 
            IPublishService publishService)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
            _publishService = publishService;
        }


        [Given(@"template mapping exists")]
        public void GivenTemplateMappingExists(Table table)
        {
            IEnumerable<TemplateMappingItem> templateMappingItems = table.CreateSet<TemplateMappingItem>();

            _publishFundingStepContext.CalculationsInMemoryClient.SetInMemoryTemplateMapping(new TemplateMapping
            {
                TemplateMappingItems = templateMappingItems
            });
        }

        [Given(@"calculation meta data exists for '(.*)'")]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void GivenCalculationMetaDataExistsFor(string fundingStreamId, Table table)
        {
            IEnumerable<CalculationMetadata> calculationMetadata = table.CreateSet<CalculationMetadata>();

            foreach (CalculationMetadata calculation in calculationMetadata)
            {
                calculation.SpecificationId = _currentSpecificationStepContext.SpecificationId;
                calculation.FundingStreamId = fundingStreamId;
            }

            _publishFundingStepContext.CalculationsInMemoryClient
                .SetInMemoryCalculationMetaData(calculationMetadata);
        }

        [Given(@"calculations exists")]
        public void GivenCalculationsExists(IEnumerable<CalculationResult> calculationResults)
        {
            _publishFundingStepContext.CalculationsInMemoryRepository.SetCalculationResults(calculationResults);
        }

        [Given(@"the following calculation results also exist")]
        public void GivenCalculationsAlsoExist(IEnumerable<CalculationResult> calculationResults)
        {
            _publishFundingStepContext.CalculationsInMemoryRepository.AddCalculationResults(calculationResults);
        }

        [Given(@"the following distribution periods exist")]
        public void GivenTheFollowingDistributionPeriodsExist(Table table)
        {
            _publishFundingStepContext.ProfilingInMemoryClient.DistributionPeriods = table.Rows.Select(_ =>
                new Common.ApiClient.Profiling.Models.DistributionPeriods
                {
                    DistributionPeriodCode = _[0], 
                    Value = decimal.Parse(_[1])
                });
        }

        [Given(@"the following profiles exist")]
        public void GivenTheFollowingProfilesExist(Table table)
        {
            _publishFundingStepContext.ProfilingInMemoryClient.DistributionPeriods.ToList().ForEach(_ =>
            {
                _publishFundingStepContext.ProfilingInMemoryClient.ProfilingPeriods = table.Rows.Where(row => 
                    row[0] == _.DistributionPeriodCode)
                    .Select(row => 
                        new Common.ApiClient.Profiling.Models.ProfilingPeriod
                    {
                        DistributionPeriod = _.DistributionPeriodCode, 
                        Type = row[1], 
                        Period = row[2], 
                        Year = Convert.ToInt16(row[3]), 
                        Occurrence = Convert.ToInt16(row[4]), 
                        Value = decimal.Parse(row[5])
                    });
            });
        }


        [When(@"funding is published")]
        public async Task WhenFundingIsPublished()
        {
            Message message = new Message();

            message.UserProperties.Add("user-id", _currentUserStepContext.UserId);
            message.UserProperties.Add("user-name", _currentUserStepContext.UserName);
            message.UserProperties.Add("specification-id", _currentSpecificationStepContext.SpecificationId);
            message.UserProperties.Add("jobId", _currentJobStepContext.JobId);

            await _publishService.Run(message);
        }

        [When(@"batch funding is published")]
        public async Task WhenBatchFundingIsPublished(Table table)
        {
            Message message = new Message();

            string[] providerIds = table.AsStrings();
            PublishedProviderIdsRequest publishProvidersRequest = new PublishedProviderIdsRequest { PublishedProviderIds = providerIds };
            string publishProvidersRequestJson = JsonExtensions.AsJson(publishProvidersRequest);

            message.UserProperties.Add("user-id", _currentUserStepContext.UserId);
            message.UserProperties.Add("user-name", _currentUserStepContext.UserName);
            message.UserProperties.Add("specification-id", _currentSpecificationStepContext.SpecificationId);
            message.UserProperties.Add("jobId", _currentJobStepContext.JobId);
            message.UserProperties.Add(JobConstants.MessagePropertyNames.PublishedProviderIdsRequest, publishProvidersRequestJson);

            await _publishService.Run(message, async () =>
            {
                await _publishService.PublishProviderFundingResults(message, batched: true);
            });
        }
    }
}
