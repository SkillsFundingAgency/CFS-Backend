using System.Collections.Generic;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Datasets.Converter
{
    public class ConverterDataMergeLog : IIdentifiable
    {
        public string Id => JobId;

        public IEnumerable<RowCopyResult> Results { get; set; }
        
        public ConverterMergeRequest Request { get; set; }
        
        public string ParentJobId { get; set; }

        public string JobId { get; set; }
        
        public int DatasetVersionCreated { get; set; }  
    }
}