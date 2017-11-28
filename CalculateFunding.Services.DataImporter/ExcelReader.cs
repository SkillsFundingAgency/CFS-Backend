using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace Allocations.Services.DataImporter
{
    public class ExcelReader
    {
        public IEnumerable<TTarget> Read<TTarget>(string filename) where TTarget : new()
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Open))
            {
                ExcelPackage excel = new ExcelPackage(fileStream);
                var workSheet = excel.Workbook.Worksheets.First();

                return workSheet.ConvertSheetToObjects<TTarget>();
            }
        }

        public IEnumerable<TTarget> Read<TTarget>(Stream stream) where TTarget : new()
        {
            ExcelPackage excel = new ExcelPackage(stream);
            var workSheet = excel.Workbook.Worksheets.First();

            return workSheet.ConvertSheetToObjects<TTarget>();
        }
    }
}