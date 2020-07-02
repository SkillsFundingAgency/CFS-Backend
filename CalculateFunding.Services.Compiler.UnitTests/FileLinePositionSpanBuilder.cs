using CalculateFunding.Tests.Common.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class FileLinePositionSpanBuilder : TestEntityBuilder
    {
        private string _path;
        private int? _startLine;
        private int? _startChar;
        private int? _endLine;
        private int? _endChar;

        public FileLinePositionSpanBuilder WithStartLineNumber(int startLine)
        {
            _startLine = startLine;

            return this;
        }
        
        public FileLinePositionSpanBuilder WithStartChar(int startChar)
        {
            _startChar = startChar;

            return this;
        }

        public FileLinePositionSpanBuilder WithEndLineNumber(int endLine)
        {
            _endLine = endLine;

            return this;
        }
        
        public FileLinePositionSpanBuilder WithEndChar(int endChar)
        {
            _endChar = endChar;

            return this;
        }

        public FileLinePositionSpanBuilder WithPath(string path)
        {
            _path = path;

            return this;
        }
        
        public FileLinePositionSpan Build()
        {
            return new FileLinePositionSpan(_path ?? NewRandomString(),
                NewLinePosition(_startLine, _startChar),
                NewLinePosition(_endLine, _endChar));
        }

        private LinePosition NewLinePosition(int? lineNumber, int? charPosition) 
            => new LinePosition(lineNumber.GetValueOrDefault(NewRandomNumberBetween(0, 10)), 
                charPosition.GetValueOrDefault(NewRandomNumberBetween(0, 10)));
    }
}