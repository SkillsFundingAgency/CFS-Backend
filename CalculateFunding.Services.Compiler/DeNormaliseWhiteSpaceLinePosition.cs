using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CalculateFunding.Services.Compiler
{
    public readonly struct DeNormaliseWhiteSpaceLinePosition
    {
        private const int NormalisedIndentSpaces = 20;
        
        private static readonly string[] EmptyLinesOfCode = new string[0];

        public DeNormaliseWhiteSpaceLinePosition(FileLinePositionSpan normalisedPosition,
            string originalSourceCode)
        {
            string[] linesOfCode = originalSourceCode?.Split(Environment.NewLine) ?? EmptyLinesOfCode;
            Dictionary<int, int> mappedLines = new Dictionary<int, int>();

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

            StartLine = GetOriginalLineNumber(mappedLines, normalisedPosition.StartLinePosition);
            StartCharacter = normalisedPosition.StartLinePosition.Character - NormalisedIndentSpaces;
            EndLine = GetOriginalLineNumber(mappedLines, normalisedPosition.EndLinePosition);
            EndCharacter = normalisedPosition.EndLinePosition.Character - NormalisedIndentSpaces;
        }

        private static int GetOriginalLineNumber(Dictionary<int, int> mappedLines,
            LinePosition linePosition)
            => mappedLines.TryGetValue(linePosition.Line, out int offset) ? linePosition.Line + offset + 1 : linePosition.Line + 1;

        public int StartLine { get; }

        public int StartCharacter { get; }
        
        public int EndLine { get; }

        public int EndCharacter { get; }

        public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) == true;

        public override int GetHashCode() => HashCode.Combine(StartLine, StartCharacter, EndLine, EndCharacter);

        public override string ToString() => $"StartLine[{StartLine}]-StartCharacter[{StartCharacter}]-EndLine[{EndLine}]-EndCharacter[{EndCharacter}]";
    }
}