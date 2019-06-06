using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class CalculationCodeReferenceUpdateTests
    {
        public struct TokenCheckerCall
        {
            public string SourceCode;
            public int Position;
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(CodeTestCases), DynamicDataSourceType.Method)]
        public void ReplaceSourceCodeReferences_RunsAsExpected(string input,
            string oldName,
            string newName,
            string expectedOutput,
            IEnumerable<TokenCheckerCall> tokenCheckerCalls)
        {
            //Arrange
            ITokenChecker tokenChecker = Substitute.For<ITokenChecker>();
            tokenChecker
                .CheckIsToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(x => x.ArgAt<int>(2) % 2 == 0);

            CalculationCodeReferenceUpdate calculationCodeReferenceUpdate = new CalculationCodeReferenceUpdate(tokenChecker);

            //Act
            string result = calculationCodeReferenceUpdate.ReplaceSourceCodeReferences(input, oldName, newName);

            //Assert
            Assert.AreEqual(expectedOutput, result);

            tokenChecker
                .Received(tokenCheckerCalls.Count())
                .CheckIsToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());

            foreach (TokenCheckerCall call in tokenCheckerCalls)
            {
                tokenChecker
                    .Received(1)
                    .CheckIsToken(call.SourceCode, oldName, call.Position);
            }
        }

        private static IEnumerable<object[]> CodeTestCases()
        {
            yield return new object[] { "Hello, world!", "X", "Y", "Hello, world!", new TokenCheckerCall[] { } };
            yield return new object[] { "Hello, world!", "l", "m", "Hemlo, wormd!", new []
            {
                new TokenCheckerCall{ SourceCode = "Hello, world!", Position = 2},
                new TokenCheckerCall{ SourceCode = "Hemlo, world!", Position = 3},
                new TokenCheckerCall{ SourceCode = "Hemlo, world!", Position = 10}
            } };
            yield return new object[] {
                @"The Dairymaid she curtsied,
And went and told
The Alderney:
“Don’t forget the butter for
The Royal slice of bread",
                "The",
                "A",
                @"A Dairymaid she curtsied,
And went and told
A Alderney:
“Don’t forget the butter for
The Royal slice of bread",
                new[] {
                    new TokenCheckerCall{ SourceCode = @"The Dairymaid she curtsied,
And went and told
The Alderney:
“Don’t forget the butter for
The Royal slice of bread",
                        Position = 0},
                    new TokenCheckerCall{ SourceCode = @"A Dairymaid she curtsied,
And went and told
The Alderney:
“Don’t forget the butter for
The Royal slice of bread",
                        Position = 46},
                    new TokenCheckerCall{ SourceCode = @"A Dairymaid she curtsied,
And went and told
A Alderney:
“Don’t forget the butter for
The Royal slice of bread",
                        Position = 73},
                    new TokenCheckerCall{ SourceCode = @"A Dairymaid she curtsied,
And went and told
A Alderney:
“Don’t forget the butter for
The Royal slice of bread",
                        Position = 89},
                    }
            };
        }
    }
}
