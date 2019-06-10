using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.DataImporter.UnitTests
{
    [TestClass]
    public class ExcelDatasetReaderTests
    {
        [TestMethod]
        public void AddtoDictionary_GivenTableDefinition_AddsHeaders()
        {
            //Arrange
            IDictionary<string, int> headers = new Dictionary<string, int>();

            string[] headerList = CreateHeaders().ToArray();

            TableDefinition tableDefinition = CreateTableDefinition();

            //Act
            for(int i = 0; i < headerList.Length; i++)
            {
                ExcelDatasetReader.AddToDictionary(headers, tableDefinition, headerList[i], i+1);
            }

            //Assert
            headers
                .Count()
                .Should()
                .Be(headerList.Count());
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DataRow(FieldType.Boolean, true, typeof(bool))]
        [DataRow(FieldType.Integer, 5, typeof(int))]
        [DataRow(FieldType.Float, 9.5, typeof(double))]
        [DataRow(FieldType.Decimal, 9.5, typeof(decimal))]
        [DataRow(FieldType.DateTime, "12/11/2019", typeof(DateTime))]
        [DataRow(FieldType.NullableOfDecimal, 9.5, typeof(decimal))]
        [DataRow(FieldType.NullableOfInteger, 5, typeof(int))]
        [DataRow(FieldType.String, "test", typeof(string))]
        public void PopulateField_GivenDefinitionAndValues_EnsuresCorrectType(FieldType fieldType, object cellValue, Type type)
        {
            //Arrange
            FieldDefinition fieldDefinition = new FieldDefinition
            {
                Name = "test-name",
                Type = fieldType
            };

            RowLoadResult rowLoadResult = new RowLoadResult
            {
                Fields = new Dictionary<string, object>()
            };

            ExcelRange excelRange = CreateExcelRange(cellValue);

            //Act
            ExcelDatasetReader.PopulateField(fieldDefinition, rowLoadResult, excelRange, true);

            //Assert
            rowLoadResult
                .Fields
                .Should()
                .HaveCount(1);

            rowLoadResult
                .Fields
                .First()
                .Value
                .GetType()
                .Should()
                .BeSameAs(type);
        }

        [TestMethod]
        [DataRow(FieldType.NullableOfDecimal, "")]
        [DataRow(FieldType.NullableOfInteger, "")]
        [DataRow(FieldType.NullableOfDecimal, null)]
        [DataRow(FieldType.NullableOfInteger, null)]
        public void PopulateField_GivenDefinitionsAndNullOrEmptyValues_EnsuresCorrectType(FieldType fieldType, object cellValue)
        {
            //Arrange
            FieldDefinition fieldDefinition = new FieldDefinition
            {
                Name = "test-name",
                Type = fieldType
            };

            RowLoadResult rowLoadResult = new RowLoadResult
            {
                Fields = new Dictionary<string, object>()
            };

            ExcelRange excelRange = CreateExcelRange(cellValue);

            //Act
            ExcelDatasetReader.PopulateField(fieldDefinition, rowLoadResult, excelRange, true);

            //Assert
            rowLoadResult
                .Fields
                .Should()
                .HaveCount(1);

            rowLoadResult
                .Fields
                .First()
                .Value
                .Should()
                .BeNull();
        }

        private static ExcelRange CreateExcelRange(object cellValue)
        {
            ExcelPackage package = new ExcelPackage();

            ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("test-ws");

            ExcelRange cell = workSheet.Cells[1, 1];

            cell.Value = cellValue;

            return cell;
        }

        private static TableDefinition CreateTableDefinition()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"{""id"":""1100000"",""name"":""High Needs"",""description"":""High Needs"",""fieldDefinitions"":[{""id"":""1100001"",""name"":""Rid"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Rid is the unique reference from The Store"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100002"",""name"":""Parent Rid"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The Rid of the parent provider (from The Store)"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100003"",""name"":""UPIN"",""identifierFieldType"":""UPIN"",""matchExpression"":null,""description"":""The UPIN identifier for the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100004"",""name"":""Provider Name"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The name of the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100005"",""name"":""Provider Type"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Describes the high level type of provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100006"",""name"":""Provider SubType"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Describes the sub type of the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100007"",""name"":""Date Opened"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The date the provider opened or will open"",""type"":""DateTime"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100008"",""name"":""URN"",""identifierFieldType"":""URN"",""matchExpression"":null,""description"":""The URN identifier for the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100009"",""name"":""Establishment Number"",""identifierFieldType"":""EstablishmentNumber"",""matchExpression"":null,""description"":""The estblishment number for the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100010"",""name"":""Local Authority"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The local authority assosciated with the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100011"",""name"":""High Needs Students 16-19"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Current year high needs students aged 16-19"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100012"",""name"":""High Needs Students 19-24"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Current year high needs students aged 19-24"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100013"",""name"":""R04 High Needs Students 16-19"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R04 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100014"",""name"":""R04 High Needs Students 19-24"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 19-24 from the ILR R04 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100015"",""name"":""R14 High Needs Students 16-19"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R14 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100016"",""name"":""R14 High Needs Students 19-24"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 19-24 from the ILR R14 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100017"",""name"":""Sp SSF High Needs"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100017"",""name"":""SSF High Needs"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100018"",""name"":""R46 High Needs Students 1619"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R46 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100019"",""name"":""R46 High Needs Students 1924"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R46 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100020"",""name"":""R06 High Needs Students"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100021"",""name"":""Place Value"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null}]}");

            return JsonConvert.DeserializeObject<TableDefinition>(sb.ToString());
        }

        private static IEnumerable<string> CreateHeaders()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"[""Rid"",""Parent Rid"",""UPIN"",""Provider Name"",""Provider Type"",""Provider Subtype"",""Date Opened"",""URN"",""Establishment Number"",""Local Authority"",""High Needs Students 16-19"",""High Needs Students 19-24"",""R04 High Needs Students 16-19"",""R04 High Needs Students 19-24"",""R14 High Needs Students 16-19"",""R14 High Needs Students 19-24"",""Sp SSF High Needs"",""SSF High Needs"",""R46 High Needs Students 1619"",""R46 High Needs Students 1924"",""R06 High Needs Students"", ""Place Value""]");

            return JsonConvert.DeserializeObject<List<string>>(sb.ToString());
        }
    }
}
