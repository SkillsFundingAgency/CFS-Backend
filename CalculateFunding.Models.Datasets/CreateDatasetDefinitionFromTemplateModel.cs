using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class CreateDatasetDefinitionFromTemplateModel
    {
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public string TemplateVersion { get; set; }
        public int? Version { get; set; }
        public int DatasetDefinitionId { get; set; }
    }
}
