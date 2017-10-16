using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace EndToEndDemo
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
    }
}