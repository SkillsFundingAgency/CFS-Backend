using CalculateFunding.Tests.Common.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CalculateFunding.Services.Compiler.UnitTests
{
    public class FileLinePositionSpanBuilder : TestEntityBuilder
    {
        private string _path;
        private int? _startLine;
        private int? _endLine;

        public FileLinePositionSpanBuilder WithStartLineNumber(int startLine)
        {
            _startLine = startLine;

            return this;
        }

        public FileLinePositionSpanBuilder WithEndLineNumber(int endLine)
        {
            _endLine = endLine;

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
                NewLinePosition(_startLine),
                NewLinePosition(_endLine));
        }

        private LinePosition NewLinePosition(int? lineNumber) => new LinePosition(lineNumber.GetValueOrDefault(NewRandomNumberBetween(0, 10)), 1);
    }
}