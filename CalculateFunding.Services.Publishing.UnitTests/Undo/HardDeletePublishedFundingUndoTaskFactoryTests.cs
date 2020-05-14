using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    [TestClass]
    public class HardDeletePublishedFundingUndoTaskFactoryTests : PublishedFundingUndoTaskFactoryTest
    {
        [TestInitialize]
        public void SetUp()
        {
            Factory = new HardDeletePublishedFundingUndoTaskFactory(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsForHardDeleteJobs(bool hardDelete)
        {
            GivenTheParametersIsHardDelete(hardDelete);

            bool isForJob = WhenIsForJobIsQueried();

            isForJob
                .Should()
                .Be(hardDelete);
        }

        [TestMethod]
        public void CreatesHardDeletePublishedProviderUndoTasks()
        {
            TheTaskIs<PublishedProviderUndoTask>(Factory.CreatePublishedProviderUndoTask(),
                (_ => _.IsHardDelete, nameof(PublishedProviderUndoTask.IsHardDelete)));    
        }
        
        [TestMethod]
        public void CreatesHardDeletePublishedProviderVersionUndoTasks()
        {
            TheTaskIs<HardDeletePublishedProviderVersionsUndoTask>(Factory.CreatePublishedProviderVersionUndoTask());    
        }
        
        [TestMethod]
        public void CreatesHardDeletePublishedFundingUndoTasks()
        {
            TheTaskIs<PublishedFundingUndoTask>(Factory.CreatePublishedFundingUndoTask(),
                (_ => _.IsHardDelete, nameof(PublishedFundingUndoTask.IsHardDelete)));    
        }
        
        [TestMethod]
        public void CreatesHardDeletePublishedFundingVersionUndoTasks()
        {
            TheTaskIs<HardDeletePublishedFundingVersionUndoTask>(Factory.CreatePublishedFundingVersionUndoTask());    
        }
    }
}