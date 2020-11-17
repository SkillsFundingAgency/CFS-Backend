using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.DataImporter
{
    public interface IExcelDatasetWriter
    {
        byte[] Write(DatasetDefinition datasetDefinition, IEnumerable<TableLoadResult> data = null);
    }
}
