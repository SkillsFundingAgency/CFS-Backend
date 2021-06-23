using System;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    [TestClass]
    public class PublishedFundingUndoTaskTests : UndoTaskTestBase
    {
        private bool _isHardDelete;

        [TestInitialize]
        public void PublishedFundingUndoTaskBaseTestBaseSetUp()
        {
            _isHardDelete = NewRandomFlag();

            TaskContext = NewPublishedFundingUndoTaskContext(_ =>
                _.WithPublishedFundingDetails(NewUndoTaskDetails())
                    .WithPublishedFundingVersionDetails(NewUndoTaskDetails()));

            TaskDetails = TaskContext.PublishedFundingDetails;

            Task = new PublishedFundingUndoTask(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object,
                _isHardDelete);
        }

        [TestMethod]
        public async Task ExitsEarlyIfErrorWhenPagingFeed()
        {
            await WhenTheTaskIsRun();

            ThenNothingWasDeleted<PublishedFunding>();
            AndNothingWasUpdated<PublishedFunding>();
        }

        [TestMethod]
        public async Task ExitsEarlyIfFeedHasNoRecords()
        {
            GivenThePublishedFundingFeed(NewFeedIterator<PublishedFunding>());

            await WhenTheTaskIsRun();

            ThenNothingWasDeleted<PublishedFunding>();
            AndNothingWasUpdated<PublishedFunding>();
        }

        [TestMethod]
        public async Task SetsCurrentToLatestPreviousVersionAndDeletesIfInitialVersion()
        {
            PublishedFunding publishedFundingOne = NewPublishedFunding();
            PublishedFunding publishedFundingTwo = NewPublishedFunding();
            PublishedFunding publishedFundingThree = NewPublishedFunding();
            PublishedFunding publishedFundingFour = NewPublishedFunding();
            PublishedFunding publishedFundingFive = NewPublishedFunding();

            PublishedFundingVersion previousVersionTwo = NewPublishedFundingVersion();
            PublishedFundingVersion previousVersionThree = NewPublishedFundingVersion();
            PublishedFundingVersion previousVersionFive = NewPublishedFundingVersion();

            GivenThePublishedFundingFeed(NewFeedIterator(WithPages(Page(publishedFundingOne, publishedFundingTwo),
                Page(publishedFundingThree, publishedFundingFour),
                Page(publishedFundingFive))));
            AndThePreviousLatestVersion(publishedFundingTwo.Current, previousVersionTwo);
            AndThePreviousLatestVersion(publishedFundingThree.Current, previousVersionThree);
            AndThePreviousLatestVersion(publishedFundingFive.Current, previousVersionFive);

            await WhenTheTaskIsRun();

            ThenTheDocumentsWereDeleted(new[] {publishedFundingOne},
                new[] {publishedFundingOne.ParitionKey},
                _isHardDelete);
            ThenTheDocumentsWereDeleted(new[] {publishedFundingFour},
                new[] {publishedFundingFour.ParitionKey},
                _isHardDelete);
            AndTheDocumentsWereUpdated(new[] {publishedFundingTwo},
                new[] {publishedFundingTwo.ParitionKey});
            AndTheDocumentsWereUpdated(new[] {publishedFundingThree},
                new[] {publishedFundingThree.ParitionKey});
            AndTheDocumentsWereUpdated(new[] {publishedFundingFive},
                new[] {publishedFundingFive.ParitionKey});
            AndThePublishedFundingHasCurrent((publishedFundingFive, previousVersionFive),
                (publishedFundingTwo, previousVersionTwo),
                (publishedFundingThree, previousVersionThree));
        }

        protected void AndThePublishedFundingHasCurrent(params (PublishedFunding funding, PublishedFundingVersion current)[] expectedMatches)
        {
            foreach ((PublishedFunding funding, PublishedFundingVersion current) expectedMatch in expectedMatches)
            {
                expectedMatch.funding.Current
                    .Should()
                    .BeSameAs(expectedMatch.current);
            }
        }

        protected void GivenThePublishedFundingFeed(ICosmosDbFeedIterator feed)
        {
            Cosmos.Setup(_ => _.GetPublishedFunding(TaskDetails.FundingStreamId,
                    TaskDetails.FundingPeriodId,
                    TaskDetails.TimeStamp))
                .Returns(feed);
        }

        protected void AndThePreviousLatestVersion(PublishedFundingVersion current,
            PublishedFundingVersion previous)
        {
            UndoTaskDetails publishedFundingVersionDetails = TaskContext.PublishedFundingVersionDetails;

            Cosmos.Setup(_ => _.GetLatestEarlierPublishedFundingVersion(publishedFundingVersionDetails.FundingStreamId,
                    publishedFundingVersionDetails.FundingPeriodId,
                    publishedFundingVersionDetails.TimeStamp,
                    current.OrganisationGroupTypeIdentifier,
                    current.OrganisationGroupIdentifierValue,
                    current.GroupingReason))
                .ReturnsAsync(previous);
        }

        protected PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder fundingBuilder = new PublishedFundingBuilder()
                .WithCurrent(NewPublishedFundingVersion());

            setUp?.Invoke(fundingBuilder);

            return fundingBuilder.Build();
        }
    }
}