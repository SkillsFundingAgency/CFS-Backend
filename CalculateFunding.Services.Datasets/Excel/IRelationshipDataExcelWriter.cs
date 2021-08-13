using CalculateFunding.Models.Datasets;
using System.Collections.Generic;

namespace CalculateFunding.Services.Datasets.Excel
{
    public interface IRelationshipDataExcelWriter
    {
        byte[] WriteToExcel(string worksheetName, IEnumerable<RelationshipDataSetExcelData> data);
    }
}
