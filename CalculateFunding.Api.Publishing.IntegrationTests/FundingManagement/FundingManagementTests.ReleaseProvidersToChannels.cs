using CalculateFunding.Common.ApiClient.Publishing;
using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.IntegrationTests.ReleaseManagement
{
    public partial class FundingManagementTests : IntegrationTestWithJobMonitoring
    {
        [TestMethod]
        public async Task RunsReleaseProviderVersionsJobForTheSpecificationIdProvidersAndChannels()
        {
            JobCreationResponse job = await WhenTheReleaseProviderVersionsJobIsQueued(_specificationId, NewReleaseProvidersToChannelRequest());

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace();

            // TODO: Will be added as part of adding unit tests for ReleaseProviderVersions method
            //await ThenTheJobSucceeds(job.JobId, "Expected ProcessDatasetObsoleteItemsJob to complete and succeed.");
        }

        private async Task<JobCreationResponse> WhenTheReleaseProviderVersionsJobIsQueued(
            string specificationId,
            ReleaseProvidersToChannelRequest request)
            => (await _publishing.QueueReleaseProviderVersions(specificationId, request))?.Content;

        protected static void AddPublishingApiClient(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<IPublishingApiClient, PublishingApiClient>();

        private ReleaseProvidersToChannelRequest NewReleaseProvidersToChannelRequest(Action<ReleaseProvidersToChannelRequestBuilder> setup = null)
            => BuildNewModel<ReleaseProvidersToChannelRequest, ReleaseProvidersToChannelRequestBuilder>(setup);

        private T BuildNewModel<T, TB>(Action<TB> setup) where TB : TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }
    }
}
