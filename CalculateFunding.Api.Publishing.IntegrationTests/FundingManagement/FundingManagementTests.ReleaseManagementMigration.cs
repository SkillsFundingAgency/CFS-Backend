using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.IntegrationTests.ReleaseManagement
{
    public partial class FundingManagementTests
    {
        [TestMethod]
        [DataRow(false)]
        public async Task RunsQueueReleaseManagementDataMigrationJobForFundingStream(bool isSelected)
        {
            string fundingStreamId = new RandomString();
            JobCreationResponse job = await WhenQueueReleaseManagementDataMigrationJobJobIsQueued(fundingStreamId);

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace();

            await ThenTheJobSucceeds(job.JobId, "Expected QueueReleaseManagementDataMigrationJob to complete and succeed.");
        }

        private async Task<JobCreationResponse> WhenQueueReleaseManagementDataMigrationJobJobIsQueued(
            string fundingStreamId)
            => (await _publishing.QueueReleaseManagementDataMigrationJob(fundingStreamId))?.Content;

    }
}
