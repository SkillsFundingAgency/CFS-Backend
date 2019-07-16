using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace CalculateFunding.Services.Publishing.UnitTests.Validators
{
    [TestClass]
    public class PublishSpecificationValidatorTests
    {
        private PublishSpecificationValidator _validator;

        [TestInitialize]
        public void SetUp()
        {
            _validator = new PublishSpecificationValidator();
        }

        [TestMethod]
        [DynamicData(nameof(EmptyIdExamples), DynamicDataSourceType.Method)]
        public async Task FailsValidationWhenNoSpecificationIdIsSupplied(string specificationId)
        {
            var result = await _validator.ValidateAsync(specificationId);

            result.Errors
                .Count
                .Should()
                .Be(1);

            result.Errors
                .Should()
                .Contain(_ => _.ErrorMessage == "No specification Id was provided");
        }

        public static IEnumerable<object[]> EmptyIdExamples()
        {
            yield return  new object[] { "" };
            yield return new object[] { string.Empty };
        }
    }
}