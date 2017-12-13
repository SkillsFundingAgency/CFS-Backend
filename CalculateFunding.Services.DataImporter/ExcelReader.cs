using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter
{
    public class ExcelReader
    {
        public IEnumerable<TTarget> Read<TTarget>(string filename, string worksheetName = null) where TTarget : new()
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Open))
            {
                ExcelPackage excel = new ExcelPackage(fileStream);
                var workSheet = excel.Workbook.Worksheets.First(x => worksheetName == null || x.Name == worksheetName);

                return workSheet.ConvertSheetToObjects<TTarget>();
            }
        }

        public IEnumerable<TTarget> Read<TTarget>(Stream stream, string worksheetName = null) where TTarget : new()
        {
            ExcelPackage excel = new ExcelPackage(stream);
            var workSheet = excel.Workbook.Worksheets.First(x => worksheetName == null || x.Name == worksheetName);

            return workSheet.ConvertSheetToObjects<TTarget>();
        }
    }
}