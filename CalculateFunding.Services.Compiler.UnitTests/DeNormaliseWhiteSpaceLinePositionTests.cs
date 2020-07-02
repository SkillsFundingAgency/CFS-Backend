using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    [TestClass]
    public class DeNormaliseWhiteSpaceLinePositionTests
    {
        [TestMethod]
        [DynamicData(nameof(DeNormalisedLinePositionExamples), DynamicDataSourceType.Method)]
        public void MapsLinesNumbersAgainstOffsetMapFromOriginalStringWhitespaceIntact(
            string originalString,
            FileLinePositionSpan diagnosticSpan,
            int expectedStartLine,
            int expectedStartChar,
            int expectedEndLine,
            int expectedEndChar)
        {
            DeNormaliseWhiteSpaceLinePosition whiteSpaceLinePosition = new DeNormaliseWhiteSpaceLinePosition(diagnosticSpan, originalString);
            
            whiteSpaceLinePosition
                .StartLine
                .Should()
                .Be(expectedStartLine);
            
            whiteSpaceLinePosition
                .StartCharacter
                .Should()
                .Be(expectedStartChar);
            
            whiteSpaceLinePosition
                .EndLine
                .Should()
                .Be(expectedEndLine);

            whiteSpaceLinePosition
                .EndCharacter
                .Should()
                .Be(expectedEndChar);
        }

        private static IEnumerable<object[]> DeNormalisedLinePositionExamples()
        {
            yield return new object []
            {
                @"
                one

                two",
                NewLinePositionSpan(_ => _.WithStartLineNumber(0)
                    .WithStartChar(21)
                    .WithEndLineNumber(1)
                    .WithEndChar(27)),
                2,
                1,
                4,
                7
            };
            yield return new object []
            {
                @"one
                two
                three

                four",
                NewLinePositionSpan(_ => _.WithStartLineNumber(0)
                    .WithStartChar(23)
                    .WithEndLineNumber(0)
                    .WithEndChar(45)),
                1,
                3,
                1,
                25
            };
            yield return new object []
            {
                @"one
                two

                three
                four",
                NewLinePositionSpan(_ => _.WithStartLineNumber(2)
                    .WithStartChar(24)
                    .WithEndLineNumber(2)
                    .WithEndChar(28)),
                4,
                4,
                4,
                8
            };
        }

        private static FileLinePositionSpan NewLinePositionSpan(Action<FileLinePositionSpanBuilder> setUp = null)
        {
            FileLinePositionSpanBuilder spanBuilder = new FileLinePositionSpanBuilder();

            setUp?.Invoke(spanBuilder);
            
            return spanBuilder.Build();
        }
    }
}