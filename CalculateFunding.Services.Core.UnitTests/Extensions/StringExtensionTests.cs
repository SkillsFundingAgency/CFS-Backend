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
            const string text = "mary had a little lamb who was -7.9228162514264338E+28 years old or nearly 7.9228162514264338E+31";

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
            const string text = "mary had a little lamb";

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
            const string text = null;

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
            const string text = "";

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
            const string text = "mary had a little lamb ";

            //act
            string result = text.RemoveAllSpaces();

            //Assert
            result
                .Should()
                .Be("maryhadalittlelamb");
        }

        [TestMethod]
        [DataRow("", false)]
        [DataRow(null, false)]
        [DataRow("    ", false)]
        [DataRow("blah blah blah", false)]
        [DataRow("YmxhaCBibGFoIGJsYWg=", true)]
        public void IsBase64_GivenEmptyString_ReturnsExpected(string text, bool result)
        {
            //Act
           bool expectedResult = text.IsBase64();

            //Assert
            expectedResult
                .Should()
                .Be(result);
        }

        [TestMethod]
        public void IsBase64_GivenBase64String_ReturnsTrue()
        {
            //Arrange
            const string text = "YmxhaCBibGFoIGJsYWg=";

            //Act
            bool result = text.IsBase64();

            //Assert
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Compress_GivenEmptyString_ReturnsEmptyArray()
        {
            //Arrange
            const string text = "";

            //Act
            byte[] result = text.Compress();

            //Assert
            result
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void Compress_GivenString_EnsureCompresses()
        {
            //Arrange
            const string text = "dsjhfjkhsdfskdhfksdfhskdhfksdhjkfhskdjfhjksdhfkjhsdjkfhkjsdhfkjhsdkjfhskdhfjksdhdfjkhsdkjfhsjkdh";
            byte[] stringAsBytes = Encoding.UTF8.GetBytes(text);

            //Act
            byte[] result = text.Compress();

            //Assert
            result
                .Length
                .Should()
                .BeLessThan(stringAsBytes.Length);
        }
    }
}


