﻿using System.Threading.Tasks;
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
    public class ApproveFundingStepDefinition
    {
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly CurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly CurrentUserStepContext _currentUserStepContext;
        private readonly IApproveService _approveService;

        public ApproveFundingStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext, 
            IApproveService approveService)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
            _approveService = approveService;
        }

        [When(@"funding is approved")]
        public async Task WhenFundingIsApproved()
        {
            Message message = new Message();

            message.UserProperties.Add("user-id", _currentUserStepContext.UserId);
            message.UserProperties.Add("user-name", _currentUserStepContext.UserName);
            message.UserProperties.Add("specification-id", _currentSpecificationStepContext.SpecificationId);
            message.UserProperties.Add("jobId", _currentJobStepContext.JobId);

            await _approveService.ApproveAllResults(message);
        }

        [When(@"partial funding is approved")]
        public async Task WhenPartialFundingIsApproved(Table table)
        {
            Message message = new Message();

            string[] providerIds = table.AsStrings();
            ApproveProvidersRequest approveProvidersRequest = new ApproveProvidersRequest { Providers = providerIds };
            string approveProvidersRequestJson = JsonExtensions.AsJson(approveProvidersRequest);

            message.UserProperties.Add("user-id", _currentUserStepContext.UserId);
            message.UserProperties.Add("user-name", _currentUserStepContext.UserName);
            message.UserProperties.Add("specification-id", _currentSpecificationStepContext.SpecificationId);
            message.UserProperties.Add("jobId", _currentJobStepContext.JobId);
            message.UserProperties.Add(JobConstants.MessagePropertyNames.ApproveProvidersRequest, approveProvidersRequestJson);

            await _approveService.ApproveBatchResults(message);
        }
    }
}
