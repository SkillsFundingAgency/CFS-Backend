using System;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Policy.Caching.Http
{
    public class TemplateMetadataContentsBuilder : TestEntityBuilder
    {
        private DateTimeOffset? _lastModified;

        public TemplateMetadataContentsBuilder WithLastModified(DateTimeOffset lastModified)
        {
            _lastModified = lastModified;

            return this;
        }
        
        public TemplateMetadataContents Build()
        {
            return new TemplateMetadataContents
            {
                LastModified = _lastModified.GetValueOrDefault(NewRandomDateTime())
            };
        }
    }
}