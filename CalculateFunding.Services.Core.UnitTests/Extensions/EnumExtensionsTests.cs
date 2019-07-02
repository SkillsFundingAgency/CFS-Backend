using System;
using System.Collections.Generic;
using System.Text;
using ComponentModel = System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class EnumExtensionsTests
    {
        [TestMethod]
        public void GetDescription_GivenEnumValue_ReturnsDescription()
        {
            //Arrange
            TestEnum testEnum = TestEnum.Green;

            //Act
            string description = testEnum.GetDescription();

            //Assert
            description
                .Should()
                .Be("green");
        }

        [TestMethod]
        public void GetDescription_GivenEnumValueThatHasNoAttribute_ReturnsEnumName()
        {
            //Arrange
            TestEnum testEnum = TestEnum.Red;

            //Act
            string description = testEnum.GetDescription();

            //Assert
            description
                .Should()
                .Be("Red");
        }

        [TestMethod]
        public void GetDescription_GivenEnumValueThatHasNoAttributeAndNameIfNullIsFalse_ReturnsEmptyString()
        {
            //Arrange
            TestEnum testEnum = TestEnum.Red;

            //Act
            string description = testEnum.GetDescription(false);

            //Assert
            description
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void GetEnumValueFromDescription_GivenDescription_ReturnsEnumValue()
        {
            //arrange
            const string green = "green";

            //Act
            TestEnum testEnum = green.GetEnumValueFromDescription<TestEnum>();

            //Assert
            testEnum
                .Should()
                .Be(TestEnum.Green);
        }

        [TestMethod]
        public void GetEnumValueFromDescription_GivenDescriptionInWrongCase_ReturnsEnumValue()
        {
            //arrange
            const string green = "Green";

            //Act
            Func<TestEnum> test = () => green.GetEnumValueFromDescription<TestEnum>();

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be($"Enum not found for description Green.");
        }


        private enum TestEnum
        {
            Red,

            [ComponentModel.Description("green")]
            Green,

            [ComponentModel.Description("blue")]
            Blue
        }
    }
}
