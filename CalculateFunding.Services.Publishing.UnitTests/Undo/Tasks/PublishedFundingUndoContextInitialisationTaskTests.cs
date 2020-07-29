using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    [TestClass]
    public class PublishedFundingUndoContextInitialisationTaskTests : UndoTaskTestBase
    {
            [TestInitialize]
            public void SetUp()
            {
                Parameters = NewPublishedFundingUndoJobParameters();
                TaskContext = NewPublishedFundingUndoTaskContext(_ => 
                    _.WithParameters(Parameters));
                
                Task = new PublishedFundingUndoContextInitialisationTask(Cosmos.Object,
                    BlobStore.Object,
                    ProducerConsumerFactory,
                    Logger,
                    JobTracker.Object);
            }

            [TestMethod]
            public async Task InitialisedCorrelationIdDetailsForAllFundingDocumentTypes()
            {
                UndoTaskDetails expectedPublishedProviderDetails = NewUndoTaskDetails();
                UndoTaskDetails expectedPublishedProviderVersionDetails = NewUndoTaskDetails();
                UndoTaskDetails expectedPublishedFundingDetails = NewUndoTaskDetails();
                UndoTaskDetails expectedPublishedFundingVersionDetails = NewUndoTaskDetails();
                
                GivenThePublishedFundingCorrelationDetails(expectedPublishedFundingDetails);
                AndThePublishedFundingVersionsCorrelationDetails(expectedPublishedFundingVersionDetails);
                AndThePublishedProviderCorrelationDetails(expectedPublishedProviderDetails);
                AndThePublishedProviderVersionsCorrelationDetails(expectedPublishedProviderVersionDetails);

                await WhenTheTaskIsRun();

                TaskContext.PublishedProviderDetails
                    .Should()
                    .BeSameAs(expectedPublishedProviderDetails);

                TaskContext.PublishedProviderVersionDetails
                    .Should()
                    .BeSameAs(expectedPublishedProviderVersionDetails);

                TaskContext.PublishedFundingDetails
                    .Should()
                    .BeSameAs(expectedPublishedFundingDetails);
                
                TaskContext.PublishedFundingVersionDetails
                    .Should()
                    .BeSameAs(expectedPublishedFundingVersionDetails);
            }

            private void GivenThePublishedFundingCorrelationDetails(UndoTaskDetails details)
            {
                Cosmos.Setup(_ => _.GetCorrelationIdDetailsForPublishedFunding(Parameters.ForCorrelationId))
                    .ReturnsAsync(details);
            }
            
            private void AndThePublishedFundingVersionsCorrelationDetails(UndoTaskDetails details)
            {
                Cosmos.Setup(_ => _.GetCorrelationIdDetailsForPublishedFundingVersions(Parameters.ForCorrelationId))
                    .ReturnsAsync(details);
            }
            private void AndThePublishedProviderCorrelationDetails(UndoTaskDetails details)
            {
                Cosmos.Setup(_ => _.GetCorrelationDetailsForPublishedProviders(Parameters.ForCorrelationId))
                    .ReturnsAsync(details);
            }
            
            private void AndThePublishedProviderVersionsCorrelationDetails(UndoTaskDetails details)
            {
                Cosmos.Setup(_ => _.GetCorrelationIdDetailsForPublishedProviderVersions(Parameters.ForCorrelationId))
                    .ReturnsAsync(details);
            }
    }
}