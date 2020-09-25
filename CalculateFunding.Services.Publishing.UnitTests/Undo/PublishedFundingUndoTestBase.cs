using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;
using Moq;
using Moq.Language;
using ModelsGroupingReason = CalculateFunding.Models.Publishing.GroupingReason;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public abstract class PublishedFundingUndoTestBase
    {
        protected PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);
            
            return providerVersionBuilder.Build();
        }

        protected PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder providerVersionBuilder = new PublishedFundingVersionBuilder()
                .WithFundingPeriod(NewFundingPeriod())
                .WithOrganisationGroupIdentifierValue(NewRandomString())
                .WithOrganisationGroupTypeIdentifier(NewRandomOrganisationGroupTypeIdentifier())
                .WithGroupReason(NewRandomGroupingReason());

            setUp?.Invoke(providerVersionBuilder);
            
            return providerVersionBuilder.Build();
        }

        protected PublishedFundingPeriod NewFundingPeriod(Action<PublishedFundingPeriodBuilder> setUp = null)
        {
            PublishedFundingPeriodBuilder fundingPeriodBuilder = new PublishedFundingPeriodBuilder();

            setUp?.Invoke(fundingPeriodBuilder);
            
            return fundingPeriodBuilder.Build();
        }

        protected UndoTaskDetails NewUndoTaskDetails(Action<UndoTaskDetailsBuilder> setUp = null)
        {
            UndoTaskDetailsBuilder detailsBuilder = new UndoTaskDetailsBuilder();

            setUp?.Invoke(detailsBuilder);
            
            return detailsBuilder.Build();
        }

        protected IEnumerable<TDocument>[] WithPages<TDocument>(params IEnumerable<TDocument>[] pages)
            where TDocument: IIdentifiable => pages;
        
        protected IEnumerable<TDocument> Page<TDocument>(params TDocument[] items) 
            where TDocument : IIdentifiable => items;

        protected ICosmosDbFeedIterator<TDocument> NewFeedIterator<TDocument>(params IEnumerable<TDocument>[] pages)
            where TDocument : IIdentifiable
        {
            Mock<ICosmosDbFeedIterator<TDocument>> feedIterator = new Mock<ICosmosDbFeedIterator<TDocument>>();

            ISetupSequentialResult<bool> hasResultsSequence = feedIterator.SetupSequence(_ => _.HasMoreResults);
            ISetupSequentialResult<Task<IEnumerable<TDocument>>> pagesSequence = feedIterator.SetupSequence(_ => 
                _.ReadNext(It.IsAny<CancellationToken>()));

            foreach (IEnumerable<TDocument> page in pages)
            {
                hasResultsSequence.Returns(true);
                pagesSequence.ReturnsAsync(page);
            }

            hasResultsSequence.Returns(false);

            return feedIterator.Object;
        }

        protected string NewRandomString() => new RandomString();
        
        protected int NewRandomInteger() => new RandomNumberBetween(1, 99);
        
        protected int NewRandomTimeStamp() => new RandomNumberBetween(10000, int.MaxValue);
        
        protected ModelsGroupingReason NewRandomGroupingReason() => new RandomEnum<ModelsGroupingReason>();
        
        protected OrganisationGroupTypeIdentifier NewRandomOrganisationGroupTypeIdentifier() => new RandomEnum<OrganisationGroupTypeIdentifier>();
        
        protected bool NewRandomFlag() => new RandomBoolean();
    }
}