using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using OfficeOpenXml;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Datasets.Excel
{
    public class RelationshipDataExcelWriter : IRelationshipDataExcelWriter
    {
        private readonly ILogger _logger;

        public RelationshipDataExcelWriter(ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));

            _logger = logger;
        }
        public byte[] WriteToExcel(string worksheetName, IEnumerable<RelationshipDataSetExcelData> data)
        {
            Guard.ArgumentNotNull(worksheetName, nameof(worksheetName));

            if (!data.AnyWithNullCheck())
            {
                string message = $"RelationshipDataSet is empty to create excel.";
                _logger.Error(message);
                throw new Exception(message);
            }

            using ExcelPackage package = new ExcelPackage();
            ExcelWorksheet workSheet = package.Workbook.Worksheets.Add(worksheetName);
            List<string> headers = GetHeaders(data);

            for (int i = 1; i <= headers.Count; i++)
            {
                ExcelRange cell = workSheet.Cells[1, i];
                cell.Value = headers[i-1];
                cell.Style.Font.Bold = true;
            }

            int rowNum = 2;
            foreach (RelationshipDataSetExcelData item in data)
            {
                workSheet.Cells[rowNum, 1].Value = item.Ukprn;

                foreach (KeyValuePair<string, decimal?> fundingLineItem in item.FundingLines)
                {
                    workSheet.Cells[rowNum, headers.IndexOf(fundingLineItem.Key) + 1].Value = fundingLineItem.Value;
                }

                foreach (KeyValuePair<string, decimal?> calculationItem in item.Calculations)
                {
                    workSheet.Cells[rowNum, headers.IndexOf(calculationItem.Key) + 1].Value = calculationItem.Value;
                }

                rowNum++;
            }

            return package.GetAsByteArray();
        }

        private List<string> GetHeaders(IEnumerable<RelationshipDataSetExcelData> data)
        {
            List<string> headers = new List<string> { nameof(RelationshipDataSetExcelData.Ukprn) };
            headers.AddRange(data.SelectMany(x => x.FundingLines.Select(f => f.Key)).Distinct());
            headers.AddRange(data.SelectMany(x => x.Calculations.Select(f => f.Key)).Distinct());

            return headers;
        }
    }
}
