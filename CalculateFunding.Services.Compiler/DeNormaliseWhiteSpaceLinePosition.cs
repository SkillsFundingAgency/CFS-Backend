using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CalculateFunding.Services.Compiler
{
    public readonly struct DeNormaliseWhiteSpaceLinePosition
    {
        private static readonly string[] EmptyLinesOfCode = new string[0];

        public DeNormaliseWhiteSpaceLinePosition(FileLinePositionSpan normalisedPosition,
            string originalSourceCode,
            string normalisedSourceCode,
            string externalSourceCodeId)
        {
            string[] linesOfCode = originalSourceCode?.Split(Environment.NewLine) ?? EmptyLinesOfCode;
            Dictionary<int, int> mappedLines = new Dictionary<int, int>();
            Dictionary<int, Dictionary<int, int>> linesOfMappedCharacters = new Dictionary<int, Dictionary<int, int>>();

            int whitespaceCount = 0;
            int normalisedWhiteSpaceLineCount = 0;

            foreach (string lineOfCode in linesOfCode)
            {
                if (lineOfCode.IsNullOrWhitespace())
                {
                    whitespaceCount++;

                    continue;
                }

                mappedLines.Add(normalisedWhiteSpaceLineCount++, whitespaceCount);
            }

            string[] linesOfNormaliseSourceCode = new NormalisedExternalSourceCode(externalSourceCodeId, normalisedSourceCode);

            StartLine = GetOriginalLineNumber(mappedLines,
                normalisedPosition.StartLinePosition);
            StartCharacter = GetOriginalCharacterPosition(StartLine,
                normalisedPosition.StartLinePosition,
                linesOfCode,
                linesOfNormaliseSourceCode,
                linesOfMappedCharacters);
            EndLine = GetOriginalLineNumber(mappedLines,
                normalisedPosition.EndLinePosition);
            EndCharacter = GetOriginalCharacterPosition(EndLine,
                normalisedPosition.EndLinePosition,
                linesOfCode,
                linesOfNormaliseSourceCode,
                linesOfMappedCharacters);
        }

        private static int GetOriginalLineNumber(Dictionary<int, int> mappedLines,
            LinePosition normalisedLinePosition)
            => mappedLines.TryGetValue(normalisedLinePosition.Line, out int offset) ? normalisedLinePosition.Line + offset + 1 : normalisedLinePosition.Line + 1;

        private static int GetOriginalCharacterPosition(int actualLineNumber,
            LinePosition linePosition,
            string[] linesOfOriginalCode,
            string[] linesOfNormalisedCode,
            Dictionary<int, Dictionary<int, int>> linesOfMappedCharacters)
        {
            string originalLineOfCode = linesOfOriginalCode[actualLineNumber - 1];
            string normalisedLineOfCode = linesOfNormalisedCode[linePosition.Line];

            int indent = normalisedLineOfCode.TakeWhile(char.IsWhiteSpace).Count();

            if (!linesOfMappedCharacters.TryGetValue(linePosition.Line, out Dictionary<int, int> mappedCharacters))
            {
                mappedCharacters = new Dictionary<int, int>();

                int normalisedOffset = 0;
                int originalOffset = 0;

                normalisedLineOfCode = normalisedLineOfCode.Trim();

                for (int character = 0; character < normalisedLineOfCode.Length; character++)
                {
                    // need to make sure we don't go beyond the end of the original code
                    if ((character + normalisedOffset) < originalLineOfCode.Length)
                    {
                        char originalCharacter = originalLineOfCode[character + normalisedOffset];
                        char normalisedCharacter = normalisedLineOfCode[character + originalOffset];

                        while (originalCharacter != normalisedCharacter)
                        {
                            // if there is a whitespace in the normalised code then skip it
                            if (char.IsWhiteSpace(normalisedCharacter))
                            {
                                normalisedCharacter = normalisedLineOfCode[++originalOffset + character];
                            }
                            else
                            {
                                int accessIndex = ++normalisedOffset + character;

                                if (accessIndex >= originalLineOfCode.Length)
                                {
                                    break;
                                }

                                originalCharacter = originalLineOfCode[accessIndex];
                            }
                        }

                        mappedCharacters.Add(character, normalisedOffset + character);
                    }
                }

                linesOfMappedCharacters.Add(linePosition.Line, mappedCharacters);
            }

            int characterWithIndent = linePosition.Character - indent;

            return mappedCharacters.TryGetValue(characterWithIndent, out int mappedCharacterPosition) ? mappedCharacterPosition + 1 : characterWithIndent;
        }

        public int StartLine { get; }

        public int StartCharacter { get; }

        public int EndLine { get; }

        public int EndCharacter { get; }

        public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) == true;

        public override int GetHashCode() => HashCode.Combine(StartLine, StartCharacter, EndLine, EndCharacter);

        public override string ToString() => $"StartLine[{StartLine}]-StartCharacter[{StartCharacter}]-EndLine[{EndLine}]-EndCharacter[{EndCharacter}]";

        private readonly struct NormalisedExternalSourceCode
        {
            public NormalisedExternalSourceCode(string externalSourceId,
                string normalisedSourceCode)
            {
                string externalSourceCompilerSymbol = $"#ExternalSource(\"{externalSourceId}";
                string externalSourceEnd = "#End ExternalSource";

                string[] linesOfCode = normalisedSourceCode?.Split(Environment.NewLine) ?? EmptyLinesOfCode;

                int startLine = linesOfCode.IndexOf(_ => _.Contains(externalSourceCompilerSymbol));

                if (startLine == -1)
                {
                    Value = EmptyLinesOfCode;

                    return;
                }

                int endLine = linesOfCode.Skip(startLine).IndexOf(_ => _.Contains(externalSourceEnd));

                if (endLine == -1)
                {
                    Value = EmptyLinesOfCode;

                    return;
                }

                Value = linesOfCode.Skip(startLine + 1).Take(endLine - 1).ToArray();
            }

            private string[] Value { get; }

            public static implicit operator string[](NormalisedExternalSourceCode input) => input.Value;
        }
    }
}