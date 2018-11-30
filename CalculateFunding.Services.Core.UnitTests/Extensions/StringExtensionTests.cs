using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class StringExtensionTests
    {
        [TestMethod]
        public void RemoveExpotentialNumber_GivenTextWithExpotentialNumber_RemovesRemoveExpotentialNumber()
        {
            //arrange
            string text = "mary had a little lamb who was -7.9228162514264338E+28 years old or nearly 7.9228162514264338E+31";

            //act
            string result = text.ConvertExpotentialNumber();

            //Assert
            result
                .Should()
                .Be("mary had a little lamb who was -7.92281625142643380 years old or nearly 7.92281625142643380");
        }

        [TestMethod]
        public void RemoveExpotentialNumber_GivenTextWithNoExpotentialNumber_ReturnsText()
        {
            //arrange
            string text = "mary had a little lamb";

            //act
            string result = text.ConvertExpotentialNumber();

            //Assert
            result
                .Should()
                .Be("mary had a little lamb");
        }

        [TestMethod]
        public void RemoveAllSpaces_GivenNullText_ReturnsEmptyString()
        {
            //arrange
            string text = null;

            //act
            string result = text.RemoveAllSpaces();

            //Assert
            result
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void RemoveAllSpaces_GivenEmptyText_ReturnsEmptyString()
        {
            //arrange
            string text = "";

            //act
            string result = text.RemoveAllSpaces();

            //Assert
            result
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void RemoveAllSpaces_GivenText_ReturnsTextWithNoSpaces()
        {
            //arrange
            string text = "mary had a little lamb ";

            //act
            string result = text.RemoveAllSpaces();

            //Assert
            result
                .Should()
                .Be("maryhadalittlelamb");
        }
    }
}
