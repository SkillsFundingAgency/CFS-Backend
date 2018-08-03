using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CalculateFunding.Services.DataImporter.Validators.Models
{
    public class DatasetWorksheetValidationModel
    {
        public ExcelPackage Data { get; set; }
    }
}
