using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.DataImporter
{
    public class DataDefinitionExcelWriter : IExcelWriter<DatasetDefinition>
    {
        public byte[] Write(DatasetDefinition data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Null dataset definition was provided");

            if (data.TableDefinitions == null || !data.TableDefinitions.Any())
                return null;

            using (ExcelPackage package = new ExcelPackage())
            {
                foreach(TableDefinition tableDefinition in data.TableDefinitions)
                {
                    int fieldCount = tableDefinition.FieldDefinitions.Count();

                    ExcelWorksheet workSheet = package.Workbook.Worksheets.Add(tableDefinition.Name);

                    IEnumerable<ExcelCellModel> headers = CreateHeaders(tableDefinition.FieldDefinitions);

                    for(int i = 1; i <= headers.Count(); i++)
                    {
                        ExcelCellModel excelCellModel = headers.ElementAt(i - 1);

                        ExcelRange cell = workSheet.Cells[1, i];

                        cell.Value = excelCellModel.Value;

                        cell.AddComment(excelCellModel.Comment, "Calculate Funding");

                        cell.Style.Font.Bold = true;
                    }

                    workSheet.Cells[workSheet.Dimension.Address].AutoFitColumns();
                }

                return package.GetAsByteArray();
            }
        }

        IEnumerable<ExcelCellModel> CreateHeaders(IEnumerable<FieldDefinition> fields)
        {
            FieldDefinition identifier = fields.FirstOrDefault(m => m.IdentifierFieldType.HasValue);

            if(identifier == null)
            {
                throw new Exception("Invalid definition, no identifier present");
            }

            List<FieldDefinition> sortedDefinitions = new List<FieldDefinition>();

            sortedDefinitions.Add(identifier);

            sortedDefinitions.AddRange(fields.Where(m => m.Id != identifier.Id));

            IList<ExcelCellModel> cellModels = new List<ExcelCellModel>();

            return sortedDefinitions.Select(m => new ExcelCellModel
            {
                Value = m.Name,
                IsRequired = m.Required,
                FieldType = m.Type.ToString(),
                Description = m.Description
            });
        }
    }
}
