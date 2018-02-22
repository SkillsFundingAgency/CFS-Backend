using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class AssignDatasourceModel
    {
        public string RelationshipId { get; set; }
        public string DatasetId { get; set; }
        public int Version { get; set; }
    }
}
