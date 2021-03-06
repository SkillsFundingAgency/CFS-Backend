﻿using System;
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
        public enum Source
        {
            Two = 1,
            One = 2
        }

        public enum Target
        {
            One = 0,
            Two = 1,
            Three = 2,
        }

        [TestMethod]
        [DataRow("One", Target.One)]
        [DataRow("Two", Target.Two)]
        [DataRow("Three", Target.Three)]
        public void AsEnumParsesLiteralIntoSuppliedEnumType(string literal, Target expectedEnumValue)
        {
            literal
                .AsEnum<Target>()
                .Should()
                .Be(expectedEnumValue);
        }

        [TestMethod]
        [DataRow(Source.One, Target.One)]
        [DataRow(Source.Two, Target.Two)]
        public void AsParsesEnumValueNames(Source source, Target expectedTarget)
        {
            source
                .AsMatchingEnum<Target>()
                .Should()
                .Be(expectedTarget);
        }
        
        [TestMethod]
        [DataRow(TestEnum.Green, "green")]
        [DataRow(TestEnum.Red, "Red")]
        public void GetDescription_GivenEnumValue_ReturnsDescription(TestEnum testEnum, string expectedDescription)
        {
            //Act
            string description = testEnum.GetDescription();

            //Assert
            description
                .Should()
                .Be(expectedDescription);
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
            //Arrange
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
            //Arrange
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

        public enum TestEnum
        {
            Red,

            [ComponentModel.Description("green")]
            Green,

            [ComponentModel.Description("blue")]
            Blue
        }
    }
}
