using CalculateFunding.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class Dataset : VersionContainer<DatasetVersion>
    {
        public Reference Definition { get; set; }

        public string Description { get; set; }
    }
}
