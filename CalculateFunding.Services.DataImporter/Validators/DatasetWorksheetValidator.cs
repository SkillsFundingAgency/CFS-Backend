using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentValidation;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static OfficeOpenXml.ExcelWorksheet;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class DatasetWorksheetValidator : AbstractValidator<ExcelPackage>
    {
        public DatasetWorksheetValidator()
        {
            RuleFor(model => model)
              .NotNull()
              .Custom((excel, context) => {

                    ExcelWorksheet firstWorkSheet = excel.Workbook.Worksheets[1];

                    ExcelRange firstCell = firstWorkSheet.Cells[1, 1];

                    if (firstCell.Value == null || string.IsNullOrWhiteSpace(firstCell.Value.ToString()))
                    {
                        context.AddFailure("Excel file does not contain any values");
                        return;
                    }

                    List<int> emptyColumns = EmptyHeaderColumn(firstWorkSheet);
                    if (emptyColumns.Any())
                    {
                        context.AddFailure($"Excel file contains empty columns at positions {string.Join(',', emptyColumns)}");
                        return;
                    }

                    MergeCellsCollection mergedCells = firstWorkSheet.MergedCells;

                    if (mergedCells.Count > 0)
                    {
                        string mergedCellLocations = string.Join(",", mergedCells);

                        context.AddFailure("Excel file contains merged cells");
                        return;
                    }

              });
        }

        private static List<int> EmptyHeaderColumn(ExcelWorksheet worksheet)
        {
            ExcelCellAddress start = worksheet.Dimension.Start;
            ExcelCellAddress end = worksheet.Dimension.End;
            List<int> blankColumns = new List<int>();

            for (int col = start.Column; col <= end.Column; col++)
            {
                ExcelRange val = worksheet.Cells[1, col];
                if (val.GetValue<string>() == null)
                {
                    blankColumns.Add(col);
                }
            }

            return blankColumns;
        }
    }
}
