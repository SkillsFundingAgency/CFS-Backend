using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
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
                    PrerequisiteCheckerLocator.Object,
                    Logger,
                    JobTracker.Object);
            }

            [TestMethod]
            public async Task InitialisedCorrelationIdDetailsForAllFundingDocumentTypes()
            {
                UndoTaskDetails expectedPublishedProviderVersionDetails = NewUndoTaskDetails();
                
                GivenThePublishedProviderVersionsCorrelationDetails(expectedPublishedProviderVersionDetails);

                PrerequisiteCheckerLocator.Setup(_ => _.GetPreReqChecker(CalculateFunding.Models.Publishing.PrerequisiteCheckerType.UndoPublishing))
                .Returns(new Mock<IPrerequisiteChecker>().Object);

                await WhenTheTaskIsRun();

                TaskContext.UndoTaskDetails
                    .Should()
                    .BeSameAs(expectedPublishedProviderVersionDetails);
            }

            private void GivenThePublishedProviderVersionsCorrelationDetails(UndoTaskDetails details)
            {
                Cosmos.Setup(_ => _.GetCorrelationIdDetailsForPublishedProviderVersions(Parameters.ForCorrelationId))
                    .ReturnsAsync(details);
            }
    }
}