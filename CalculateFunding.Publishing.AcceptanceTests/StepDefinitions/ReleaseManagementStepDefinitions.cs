using CalculateFunding.Common.Models;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    public class ReleaseManagementStepDefinitions : StepDefinitionBase
    {
        private readonly ReleaseProvidersToChannelsService _releaseProvidersToChannelsService;
        private readonly IReleaseProvidersToChannelsContext _ctx;
        private readonly ICurrentSpecificationStepContext _spec;
        private readonly ICurrentJobStepContext _currentJobStepContext;

        public ReleaseManagementStepDefinitions(ReleaseProvidersToChannelsService releaseProvidersToChannelsService,
            IReleaseProvidersToChannelsContext releaseProvidersToChannelsContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext,
            ICurrentJobStepContext currentJobStepContext)
        {
            _releaseProvidersToChannelsService = releaseProvidersToChannelsService;
            _ctx = releaseProvidersToChannelsContext;
            _spec = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
        }

        [Given(@"funding is released for providers")]
        public void GivenFundingIsReleasedForProviders(Table table)
        {
            List<string> providerIds = new List<string>();

            foreach (var row in table.Rows)
            {
                providerIds.Add(row[0]);
            }

            _ctx.Request.ProviderIds = providerIds;
        }


        [Given(@"funding is released for channels")]
        public void GivenFundingIsReleasedForChannels(Table table)
        {
            List<string> channels = new List<string>();

            foreach (var headerValue in table.Header)
            {
                channels.Add(headerValue);
            }

            foreach (var row in table.Rows)
            {
                channels.Add(row[0]);
            }

            _ctx.Request.Channels = channels;
        }



        [When(@"funding is released to channels for selected providers")]
        public async Task WhenFundingIsReleasedToChannelsForSelectedProviders(Table table)
        {
            ReleaseFundingForChannelsRequest releaseFundingForChannelsRequest = table.CreateInstance<ReleaseFundingForChannelsRequest>();
            Reference author = new Reference(releaseFundingForChannelsRequest.AuthorId, releaseFundingForChannelsRequest.AuthorName);

            await _releaseProvidersToChannelsService.ReleaseProviderVersions(_spec.SpecificationId,
                 _ctx.Request,
                 _currentJobStepContext.JobId,
                 releaseFundingForChannelsRequest.CorrelationId,
                 author
                 );
        }

        [Then(@"funding is successfully released")]
        public void ThenFundingIsSuccessfullyReleased()
        {
            throw new PendingStepException();
        }

    }
}
