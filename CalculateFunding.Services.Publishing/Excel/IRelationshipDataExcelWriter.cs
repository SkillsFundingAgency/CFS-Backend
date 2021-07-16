using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Excel
{
    public interface IRelationshipDataExcelWriter
    {
        byte[] WriteToExcel(string worksheetName, IEnumerable<RelationshipDataSetExcelData> data);
    }
}
