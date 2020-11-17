using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.DataImporter
{
    public class DataDefinitionExcelWriter : IExcelDatasetWriter
    {
        public byte[] Write(DatasetDefinition datasetDefinition, IEnumerable<TableLoadResult> data = null)
        {
            if (datasetDefinition == null)
                throw new ArgumentNullException(nameof(datasetDefinition), "Null dataset definition was provided");

            if (datasetDefinition.TableDefinitions == null || !datasetDefinition.TableDefinitions.Any())
                return null;

            bool haveData = data != null;

            using (ExcelPackage package = new ExcelPackage())
            {
                foreach(TableDefinition tableDefinition in datasetDefinition.TableDefinitions)
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

                    if(haveData)
                    {
                        TableLoadResult tableData = data.FirstOrDefault(x => x.TableDefinition.Name == tableDefinition.Name);

                        if(tableData != null)
                        {
                            int rowNumber = 2;
                            foreach (RowLoadResult row in tableData.Rows)
                            {
                                for (int i = 1; i <= headers.Count(); i++)
                                {
                                    ExcelCellModel excelCellModel = headers.ElementAt(i - 1);
                                    object fieldValue = row.Fields.FirstOrDefault(x => x.Key == excelCellModel.Value.ToString()).Value;

                                    if(fieldValue != null)
                                    {
                                        workSheet.Cells[rowNumber, i].Value = fieldValue;
                                    }
                                }

                                rowNumber++;
                            }
                        }
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
