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
            string originalSourceCode,
            string normalisedSourceCode,
            string externalSourceId,
            FileLinePositionSpan diagnosticSpan,
            int expectedStartLine,
            int expectedStartChar,
            int expectedEndLine,
            int expectedEndChar)
        {
            DeNormaliseWhiteSpaceLinePosition whiteSpaceLinePosition = new DeNormaliseWhiteSpaceLinePosition(diagnosticSpan, 
                originalSourceCode, 
                normalisedSourceCode, 
                externalSourceId);
            
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
one two  three

two",
                 @"all
                    of
                     this 
                         should be 
                             ignored
                 #ExternalSource(""4321|AnythingElse"", 1)
   onetwo three
   two
                 #End ExternalSource",
                 "4321",
                 NewLinePositionSpan(_ => _.WithStartLineNumber(0)
                     .WithStartChar(10)
                     .WithEndLineNumber(1)
                     .WithEndChar(3)),
                 2,
                 10,
                 4,
                 1
             };
              yield return new object []
              {
                  @"one
two
three bar

four",
                  @"
                  #ExternalSource(""XXX|AnythingElse"", 1)
                  one
                  four
                  #End ExternalSource
                  all
                     of
                      this 
                          should be 
                              ignored
                  #ExternalSource(""1234|AnythingElse"", 1)
                  one
                  two
                  threebar
                  four
                  #End ExternalSource",
                  "1234",
                  NewLinePositionSpan(_ => _.WithStartLineNumber(2)
                      .WithStartChar(18)
                      .WithEndLineNumber(3)
                      .WithEndChar(22)),
                  3,
                  1,
                  5,
                  4
              };
            yield return new object []
            {
@"one
two

three
four bar",
                @"all
                   of
                    this 
                        should be 
                            ignored
                #ExternalSource(""1122|AnythingElse"", 1)
                one
                two
                three
                fourbar
                #End ExternalSource
                #ExternalSource(""XXX|AnythingElse"", 1)
                one
                four
                #End ExternalSource
                all
                   of
                    this 
                        should be 
                            ignored",
                "1122",
                NewLinePositionSpan(_ => _.WithStartLineNumber(3)
                    .WithStartChar(16)
                    .WithEndLineNumber(3)
                    .WithEndChar(23)), //goes off end of line and gets capped to line length
                5,
                1,
                5,
                7
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