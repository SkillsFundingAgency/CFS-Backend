using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedSpecificationItemBuilder : TestEntityBuilder
    {
        private string _sourceCodeName;
        private FieldType? _fieldType;
        private uint? _templateId;

        public PublishedSpecificationItemBuilder WithName(string name)
        {
            _sourceCodeName = name;

            return this;
        }

        public PublishedSpecificationItemBuilder WithFieldType(FieldType? fieldType)
        {
            _fieldType = fieldType;

            return this;
        }

        public PublishedSpecificationItemBuilder WithSourceCodeName(string sourceCodeName)
        {
            _sourceCodeName = sourceCodeName;

            return this;
        }

        public PublishedSpecificationItemBuilder WithTemplateId(uint? templateId)
        {
            _templateId = templateId;

            return this;
        }

        public PublishedSpecificationItem Build()
        {
            return new PublishedSpecificationItem
            {
                FieldType = _fieldType ?? NewRandomEnum<FieldType>(),
                Name = _sourceCodeName ?? NewRandomString(),
                SourceCodeName = _sourceCodeName ?? NewRandomString(),
                TemplateId = _templateId ?? NewRandomUint()
            };
        }
    }
}
