using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Models
{
    [TestClass]
    public class ProviderVariationContextTests
    {
        private ProviderVariationContext _variationContext;
        private IApplyProviderVariations _applyProviderVariations;
        
        [TestInitialize]
        public void SetUp()
        {
            _variationContext = new ProviderVariationContext();

            _applyProviderVariations = Substitute.For<IApplyProviderVariations>();
        }

        [TestMethod]
        public async Task QueuesVariationChangesToBeAppliedLater()
        {
            IVariationChange changeOne = NewChange();
            IVariationChange changeTwo = NewChange();
            IVariationChange changeThree = NewChange();
            
            _variationContext.QueueVariationChange(changeThree);
            _variationContext.QueueVariationChange(changeOne);
            _variationContext.QueueVariationChange(changeTwo);

            await _variationContext.ApplyVariationChanges(_applyProviderVariations);
            
            Received.InOrder(async () =>
            {
                await changeThree.Apply(_applyProviderVariations);
                await changeOne.Apply(_applyProviderVariations);
                await changeTwo.Apply(_applyProviderVariations);
            });
        }

        private IVariationChange NewChange() => Substitute.For<IVariationChange>();
    }
}