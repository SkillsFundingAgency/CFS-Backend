using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
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
        public void ReplaceSourceCodeReferences_RunsAsExpected(Calculation input,
            string oldName,
            string newName,
            string expectedOutput,
            IEnumerable<TokenCheckerCall> tokenCheckerCalls)
        {
            //Arrange
            ITokenChecker tokenChecker = Substitute.For<ITokenChecker>();
            tokenChecker
                .CheckIsToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
                .Returns(x => x.ArgAt<int>(3) % 2 == 0
                    ? x.ArgAt<string>(2).Length
                    : (int?)null);

            CalculationCodeReferenceUpdate calculationCodeReferenceUpdate = new CalculationCodeReferenceUpdate(tokenChecker);

            //Act
            string result = calculationCodeReferenceUpdate.ReplaceSourceCodeReferences(input, oldName, newName);

            //Assert
            Assert.AreEqual(expectedOutput, result);

            tokenChecker
                .Received(tokenCheckerCalls.Count())
                .CheckIsToken(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());

            foreach (TokenCheckerCall call in tokenCheckerCalls)
            {
                tokenChecker
                    .Received(1)
                    .CheckIsToken(call.SourceCode, input.Namespace, oldName, call.Position);
            }
        }

        private static IEnumerable<object[]> CodeTestCases()
        {
            yield return new object[]
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        SourceCode = "Hello, world!", Namespace = CalculationNamespace.Additional
                    },
                },
                "X", "Y", "Hello, world!", new TokenCheckerCall[] { }
            };

            Calculation original = new Calculation
            {
                Current = new CalculationVersion
                {
                    SourceCode = @"The Dairymaid she curtsied,
            And went and told
            The Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                    Namespace = CalculationNamespace.Additional
                }
            };
            yield return new object[] {
                original,
                "The",
                "A",
                @"Calculations.A Dairymaid she curtsied,
            And went and told
            The Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                new[] {
                    new TokenCheckerCall{ SourceCode = original.Current.SourceCode, Position = 0},
                    new TokenCheckerCall{ SourceCode = @"Calculations.A Dairymaid she curtsied,
            And went and told
            The Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                        Position = 83},
                    new TokenCheckerCall{ SourceCode = @"Calculations.A Dairymaid she curtsied,
            And went and told
            The Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                        Position = 123},
                    new TokenCheckerCall{ SourceCode = @"Calculations.A Dairymaid she curtsied,
            And went and told
            The Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                        Position = 151},
                }
            };

            original = new Calculation
            {
                Current = new CalculationVersion
                {
                    SourceCode = @"The Dairymaid she curtsied,
            And went and told
            The Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                    Namespace = CalculationNamespace.Template
                },
                FundingStreamId = "Cromulent"
            };
            yield return new object[] {
                original,
                "The",
                "A",
                @"Cromulent.A Dairymaid she curtsied,
            And went and told
            Cromulent.A Alderney:
            Don’t forget Cromulent.A butter for
            Cromulent.A Royal slice of bread",
                new[] {
                    new TokenCheckerCall{ SourceCode = original.Current.SourceCode, Position = 0},
                    new TokenCheckerCall{ SourceCode = @"Cromulent.A Dairymaid she curtsied,
            And went and told
            The Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                        Position = 80},
                    new TokenCheckerCall{ SourceCode = @"Cromulent.A Dairymaid she curtsied,
            And went and told
            Cromulent.A Alderney:
            Don’t forget the butter for
            The Royal slice of bread",
                        Position = 128},
                    new TokenCheckerCall{ SourceCode = @"Cromulent.A Dairymaid she curtsied,
            And went and told
            Cromulent.A Alderney:
            Don’t forget Cromulent.A butter for
            The Royal slice of bread",
                        Position = 164},
                }
            };
        }
    }
}
