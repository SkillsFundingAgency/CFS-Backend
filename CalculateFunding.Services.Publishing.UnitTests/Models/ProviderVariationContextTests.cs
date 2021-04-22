using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Models
{
    [TestClass]
    public class ProviderVariationContextTests
    {
        private ProviderVariationContext _variationContext;
        private Mock<IApplyProviderVariations> _applyProviderVariations;
        private Mock<IPoliciesService> _policiesService;
        
        [TestInitialize]
        public void SetUp()
        {
            _policiesService = new Mock<IPoliciesService>();
            _variationContext = new ProviderVariationContext(_policiesService.Object);

            _applyProviderVariations = new Mock<IApplyProviderVariations>();
        }

        [TestMethod]
        public async Task QueuesVariationChangesToBeAppliedLater()
        {
            Mock<IVariationChange> changeOne = NewChange();
            Mock<IVariationChange> changeTwo = NewChange();
            Mock<IVariationChange> changeThree = NewChange();

            int callOrder = 0;

            // Arrange & Assert - Should invoke in the same order as supplied to QueueVariationChange
            changeThree.Setup(x => x.Apply(_applyProviderVariations.Object))
                .Callback(() => { callOrder++; callOrder.Should().Be(1); }) ;
            changeOne.Setup(x => x.Apply(_applyProviderVariations.Object))
                .Callback(() => { callOrder++; callOrder.Should().Be(2); });
            changeTwo.Setup(x => x.Apply(_applyProviderVariations.Object))
                .Callback(() => { callOrder++; callOrder.Should().Be(3); });

            _variationContext.QueueVariationChange(changeThree.Object);
            _variationContext.QueueVariationChange(changeOne.Object);
            _variationContext.QueueVariationChange(changeTwo.Object);

            await _variationContext.ApplyVariationChanges(_applyProviderVariations.Object);
        }

        private Mock<IVariationChange> NewChange() => new Mock<IVariationChange>();
    }
}