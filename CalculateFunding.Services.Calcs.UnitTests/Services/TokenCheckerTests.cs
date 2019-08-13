using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Calcs.Services.Calculations
{
    [TestClass]
    public class TokenCheckerTests
    {
#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(TestReplacements), DynamicDataSourceType.Method)]
        public void CheckToken_TestReplacement_ReturnsExpected(string calcNamespace,
            string sourceCode,
            string token,
            int position,
            int? tokenLength)
        {
            //Arrange
            TokenChecker tokenChecker = new TokenChecker();

            //Act & Assert
            Assert.AreEqual(tokenLength, tokenChecker.CheckIsToken(sourceCode, calcNamespace, token, position));
        }

        private static IEnumerable<object[]> TestReplacements()
        {
            foreach (string ns in new[] { "Cromulent", "Embiggen" })
            {
                yield return new object[] { ns, "Lorem(\"Ipsum\")", "Lorem", 0, 5 };
                yield return new object[] { ns, "Lorem(\"Ipsum\")", "Ipsum", 0, null };
                yield return new object[] { ns, "Lorem(\"Ipsum\")", "Ipsum", 1, null };
                yield return new object[] { ns, "Lorem(\"Ipsum\")", "orem", 1, null };
                yield return new object[] { ns, "Lorem(Ipsum(),Dolor())", "Ipsum", 6, 5 };
                yield return new object[] { ns, "Lorem(Ipsum(),Dolor())", "Dolor", 14, 5 };
                yield return new object[] { ns, "Lorem(Ipsum(),Dolor)", "Dolor", 14, 5 };
                yield return new object[] { ns, "Lorem.Ipsum()", "Lorem", 0, 5 };
                yield return new object[] { ns, "Lorem=Ipsum.Dolor", "Dolor", 12, null };
                yield return new object[] { ns, "LoremIpsum = Dolor", "Ipsum", 5, null };
                yield return new object[] { ns, "Lorem=Ipsum+Dolor", "Lorem", 0, null };
                foreach (var op in new[] { "+", "-", "*", "/" })
                {
                    yield return new object[] { ns, $"Lorem=Ipsum{op}Dolor", "Ipsum", 6, 5 };
                    yield return new object[] { ns, $"Lorem=Ipsum{op}Dolor", "Dolor", 12, 5 };
                }
            }
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(InvalidTokens), DynamicDataSourceType.Method)]
        public void CheckTokenLegal_TokenIsInvalid_ExceptionThrown(string token, bool isNamespaced, string expectedError)
        {
            //Arrange
            TokenChecker tokenChecker = new TokenChecker();

            //Act
            Action action = () => tokenChecker.CheckTokenLegal(token, isNamespaced);

            //Assert
            action.Should().Throw<ArgumentException>().WithMessage(expectedError);
        }

        private static IEnumerable<object[]> InvalidTokens()
        {
            string blank = "Token names cannot be blank";
            string invalid = "Token name '{0}' is not a valid identifier";

            yield return new object[] { string.Empty, false, blank };
            yield return new object[] { " ", false, blank };
            yield return new object[] { Environment.NewLine, false, blank };
            yield return new object[] { "\t", false, blank };

            foreach (var namespaced in new[] {true, false})
            {
                foreach (var character in (" \t()[]{}-+=!\"':;,/|\\¬¦%&*'" + (namespaced ? "" : ".")).ToCharArray())
                {
                    string token = (namespaced ? "a." : "") + $"abc{character}def";
                    yield return new object[] {token, namespaced, string.Format(invalid, token)};
                }
            }
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(ValidTokens), DynamicDataSourceType.Method)]
        public void CheckTokenLegal_TokenIsValid_Completes(string token, bool isNamespaced)
        {
            //Arrange
            TokenChecker tokenChecker = new TokenChecker();

            //Act 
            Action action = () => tokenChecker.CheckTokenLegal(token, isNamespaced);

            //Assert
            //Nothing to check, if it's not thrown an exception we're good
        }

        private static IEnumerable<object[]> ValidTokens()
        {
            foreach (var namespaced in new[] { true, false })
            {
                foreach (var character in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvqxyz".ToCharArray())
                {
                    string token = (namespaced ? $"a." : "") + character + Guid.NewGuid().ToString();
                    yield return new object[] { token, namespaced };
                }
            }
        }

    }
}
