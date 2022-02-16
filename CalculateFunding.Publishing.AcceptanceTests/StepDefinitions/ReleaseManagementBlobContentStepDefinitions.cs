using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    public class ReleaseManagementBlobContentStepDefinitions : StepDefinitionBase
    {
        private readonly IReleaseManagementBlobStepContext _blobContext;

        public ReleaseManagementBlobContentStepDefinitions(IReleaseManagementBlobStepContext releaseManagementBlobStepContext)
        {
            _blobContext = releaseManagementBlobStepContext;
        }

        [Then(@"there is content blob created for the funding group with ID '([^']*)' in the channel '([^']*)'")]
        public void ThenThereIsContentBlobCreatedForTheFundingGroupWithIDInTheChannel(string fundingId, string channelCode)
        {
            string filename = $"{channelCode}/{fundingId}.json";

            bool blobExists = _blobContext.FundingGroupsClient.ContainsBlob(filename);

            blobExists.Should().BeTrue($"funding groups repository should contain file '{filename}'");
        }

        [Then(@"there are '([^']*)' files contained in the funding groups blob storage")]
        public void ThenThereAreFilesContainedInTheFundingGroupsBlobStorage(int expectedTotalFiles)
        {
            _blobContext.FundingGroupsClient.GetFiles().Count.Should().Be(expectedTotalFiles);
        }

        [Then(@"there are '([^']*)' files contained in the published providers blob storage")]
        public void ThenThereAreFilesContainedInThePublishedProvidersBlobStorage(int expectedTotalFiles)
        {
            _blobContext.PublishedProvidersClient.GetFiles().Count.Should().Be(expectedTotalFiles);
        }

        [Then(@"there are '([^']*)' files contained in the released providers blob storage")]
        public void ThenThereAreFilesContainedInTheReleasedProvidersBlobStorage(int expectedTotalFiles)
        {
            _blobContext.ReleasedProvidersClient.GetFiles().Count.Should().Be(expectedTotalFiles);
        }

        [Then(@"there is content blob created for the released published provider with ID '([^']*)'")]
        public void ThenThereIsContentBlobCreatedForTheReleaseProviderWithIDInTheChannel(string fundingId)
        {
            string filename = $"{fundingId}.json";

            bool blobExists = _blobContext.PublishedProvidersClient.ContainsBlob(filename);

            blobExists.Should().BeTrue($"Release provider blob repository should contain file '{filename}'");
        }

        [Then(@"there is content blob created for the released provider with ID '([^']*)' in channel '([^']*)'")]
        public void ThenThereIsContentBlobCreatedForTheReleasedProviderWithIDInChannel(string fundingId, string channelCode)
        {
            string filename = $"{channelCode}/{fundingId}.json";

            bool blobExists = _blobContext.ReleasedProvidersClient.ContainsBlob(filename);

            blobExists.Should().BeTrue($"funding groups repository should contain file '{filename}'");
        }
    }
}
