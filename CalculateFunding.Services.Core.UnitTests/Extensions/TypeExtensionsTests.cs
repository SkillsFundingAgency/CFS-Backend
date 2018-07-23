using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class TypeExtensionsTests
    {
        [TestMethod]
        public void FriendlyName_WhenNormalType()
        {
            // Arrange
            Type type = typeof(string);

            // Act
            string result = type.GetFriendlyName();

            // Assert
            result.Should().Be("String");
        }

        [TestMethod]
        public void FriendlyName_WhenGenericTypeWithOneParameter()
        {
            // Arrange
            Type type = typeof(List<string>);

            // Act
            string result = type.GetFriendlyName();

            // Assert
            result.Should().Be("List<String>");
        }

        [TestMethod]
        public void FriendlyName_WhenGenericTypeWithTwoParameter()
        {
            // Arrange
            Type type = typeof(Dictionary<int, string>);

            // Act
            string result = type.GetFriendlyName();

            // Assert
            result.Should().Be("Dictionary<Int32, String>");
        }
    }
}
