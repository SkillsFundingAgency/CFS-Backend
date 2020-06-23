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
            int expectedEndLine)
        {
            DeNormaliseWhiteSpaceLinePosition whiteSpaceLinePosition = new DeNormaliseWhiteSpaceLinePosition(diagnosticSpan, originalString);
            
            whiteSpaceLinePosition
                .StartLine
                .Should()
                .Be(expectedStartLine);
            
            whiteSpaceLinePosition
                .EndLine
                .Should()
                .Be(expectedEndLine);
        }

        private static IEnumerable<object[]> DeNormalisedLinePositionExamples()
        {
            yield return new object []
            {
                @"
                one

                two",
                NewLinePositionSpan(_ => _.WithStartLineNumber(0)
                    .WithEndLineNumber(1)),
                2,
                4
            };
            yield return new object []
            {
                @"one
                two
                three

                four",
                NewLinePositionSpan(_ => _.WithStartLineNumber(0)
                    .WithEndLineNumber(0)),
                1,
                1
            };
            yield return new object []
            {
                @"one
                two

                three
                four",
                NewLinePositionSpan(_ => _.WithStartLineNumber(2)
                    .WithEndLineNumber(2)),
                4,
                4
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