using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Calcs.Services.Calculations
{
    [TestClass]
    public class TokenCheckerTests
    {
        [TestMethod]
        [DynamicData(nameof(InvalidTokens), DynamicDataSourceType.Method)]
        public void CheckIsToken_TokenIsInvalid_ExceptionThrown(string token, string expectedError)
        {
            //Don't know why, but NCrunch can't recognise this pattern of test. Run it manually though and you'll see it's fine. 
            //Arrange
            TokenChecker tokenChecker = new TokenChecker();

            //Act
            Action action = () => tokenChecker.CheckIsToken("The quick brown fox jumps over a lazy dog", token, 0);

            //Assert
            action.Should().Throw<ArgumentException>().WithMessage(expectedError);
        }

        private static IEnumerable<object[]> InvalidTokens()
        {
            string blank = "Token names cannot be blank";
            string invalid = "Token name '{0}' is not a valid identifier";

            yield return new object[] { string.Empty, blank };
            yield return new object[] { " ", blank };
            yield return new object[] { Environment.NewLine, blank };
            yield return new object[] { "\t", blank };

            foreach (var character in " \t.()[]{}-+=!\"':;,/|\\¬¦%&*'".ToCharArray())
            {
                string token = $"abc{character}def";
                yield return new object[] { token, string.Format(invalid, token) };
            }
        }

        [TestMethod]
        [DynamicData(nameof(InvalidTokenPositions), DynamicDataSourceType.Method)]
        public void CheckIsToken_TokenPositionIsInvalid_ExceptionThrown(string sourceCode, string token, int position, string expectedError)
        {
            //Don't know why, but NCrunch can't recognise this pattern of test. Run it manually though and you'll see it's fine. 
            //Arrange
            TokenChecker tokenChecker = new TokenChecker();

            //Act
            Action action = () => tokenChecker.CheckIsToken(sourceCode, token, position);

            //Assert
            action.Should().Throw<ArgumentException>().WithMessage(expectedError);
        }

        private static IEnumerable<object[]> InvalidTokenPositions()
        {
            string badPlace = "Supplied token position is invalid";

            yield return new object[] { "a", "a", -1, badPlace };
            yield return new object[] { "Source code", "abc", 9, badPlace };
        }

        [TestMethod]
        [DynamicData(nameof(TestReplacements), DynamicDataSourceType.Method)]
        public void CheckToken_TestReplacement_ReturnsExpected(string sourceCode, string token, int position, bool isToken)
        {
            //Don't know why, but NCrunch can't recognise this pattern of test. Run it manually though and you'll see it's fine. 
            //Arrange
            TokenChecker tokenChecker = new TokenChecker();

            //Act & Assert
            Assert.AreEqual(isToken, tokenChecker.CheckIsToken(sourceCode, token, position));
        }

        private static IEnumerable<object[]> TestReplacements()
        {
            yield return new object[] { "Lorem(\"Ipsum\")", "Lorem", 0, true };
            yield return new object[] { "Lorem(\"Ipsum\")", "Ipsum", 0, false };
            yield return new object[] { "Lorem(\"Ipsum\")", "Ipsum", 1, false };
            yield return new object[] { "Lorem(\"Ipsum\")", "orem", 1, false };
            yield return new object[] { "Lorem(Ipsum(),Dolor())", "Ipsum", 6, true };
            yield return new object[] { "Lorem(Ipsum(),Dolor())", "Dolor", 14, true };
            yield return new object[] { "Lorem(Ipsum(),Dolor)", "Dolor", 14, true };
            yield return new object[] { "Lorem.Ipsum()", "Lorem", 0, true };
            yield return new object[] { "Lorem=Ipsum.Dolor", "Dolor", 12, false };
            yield return new object[] { "LoremIpsum = Dolor", "Ipsum", 5, false };
            yield return new object[] { "Lorem=Ipsum+Dolor", "Lorem", 0, false };
            foreach (var op in new[] { "+", "-", "*", "/" })
            {
                yield return new object[] { $"Lorem=Ipsum{op}Dolor", "Ipsum", 6, true };
                yield return new object[] { $"Lorem=Ipsum{op}Dolor", "Dolor", 12, true };
            }
        }
    }
}
