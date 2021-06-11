using CalculateFunding.IntegrationTests.Common.Data;
using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Newtonsoft.Json;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Api.Calcs.IntegrationTests.Data
{
    public class TemplateMappingsContext : NoPartitionKeyCosmosBulkDataContext
    {
        private dynamic _templateMappingParameters;
        private string _now;

        public TemplateMappingsContext(IConfiguration configuration) : base(configuration,
            "calcs",
            "CalculateFunding.Api.Calcs.IntegrationTests.Resources.TemplateMappingsTemplate",
            typeof(TemplateMappingsContext).Assembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) {
            _templateMappingParameters = documentData;
            _now = now;
            return new
            {
                ID = documentData.Id,
                SPECIFICATIONID = documentData.SpecificationId,
                FUNDINGSTREAMID = documentData.FundingStreamId,
                NOW = now
            };
        }

        public TemplateMapping GetTemplate()
        {
            DocumentEntity<TemplateMapping> documentEntity = JsonConvert.DeserializeObject<DocumentEntity<TemplateMapping>>(GetFormattedDocument(_templateMappingParameters, _now));
            return documentEntity.Content;
        }
    }
}
