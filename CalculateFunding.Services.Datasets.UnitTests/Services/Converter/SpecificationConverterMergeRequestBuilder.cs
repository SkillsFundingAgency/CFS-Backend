using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    public class SpecificationConverterMergeRequestBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private Reference _author;

        public SpecificationConverterMergeRequestBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }
        
        public SpecificationConverterMergeRequestBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }
        
        
        public SpecificationConverterMergeRequest Build()
        {
            return new SpecificationConverterMergeRequest
            {
                SpecificationId = _specificationId,
                Author = _author
            };
        }
    }
}