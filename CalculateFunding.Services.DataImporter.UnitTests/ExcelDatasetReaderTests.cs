using CalculateFunding.Models.Datasets.Schema;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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


        static TableDefinition CreateTableDefinition()
        {
            var sb = new System.Text.StringBuilder(5238);
            sb.AppendLine(@"{""id"":""1100000"",""name"":""High Needs"",""description"":""High Needs"",""fieldDefinitions"":[{""id"":""1100001"",""name"":""Rid"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Rid is the unique reference from The Store"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100002"",""name"":""Parent Rid"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The Rid of the parent provider (from The Store)"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100003"",""name"":""UPIN"",""identifierFieldType"":""UPIN"",""matchExpression"":null,""description"":""The UPIN identifier for the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100004"",""name"":""Provider Name"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The name of the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100005"",""name"":""Provider Type"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Describes the high level type of provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100006"",""name"":""Provider SubType"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Describes the sub type of the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100007"",""name"":""Date Opened"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The date the provider opened or will open"",""type"":""DateTime"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100008"",""name"":""URN"",""identifierFieldType"":""URN"",""matchExpression"":null,""description"":""The URN identifier for the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100009"",""name"":""Establishment Number"",""identifierFieldType"":""EstablishmentNumber"",""matchExpression"":null,""description"":""The estblishment number for the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100010"",""name"":""Local Authority"",""identifierFieldType"":null,""matchExpression"":null,""description"":""The local authority assosciated with the provider"",""type"":""String"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100011"",""name"":""High Needs Students 16-19"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Current year high needs students aged 16-19"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100012"",""name"":""High Needs Students 19-24"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Current year high needs students aged 19-24"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100013"",""name"":""R04 High Needs Students 16-19"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R04 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100014"",""name"":""R04 High Needs Students 19-24"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 19-24 from the ILR R04 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100015"",""name"":""R14 High Needs Students 16-19"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R14 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100016"",""name"":""R14 High Needs Students 19-24"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 19-24 from the ILR R14 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100017"",""name"":""Sp SSF High Needs"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100017"",""name"":""SSF High Needs"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100018"",""name"":""R46 High Needs Students 1619"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R46 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100019"",""name"":""R46 High Needs Students 1924"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Count of high needs students aged 16-19 from the ILR R46 collection"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100020"",""name"":""R06 High Needs Students"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null},{""id"":""1100021"",""name"":""Place Value"",""identifierFieldType"":null,""matchExpression"":null,""description"":""Description to be provided as part of the schema"",""type"":""Decimal"",""required"":false,""min"":null,""max"":null,""mustMatch"":null}]}");

            return JsonConvert.DeserializeObject<TableDefinition>(sb.ToString());
        }

        static IEnumerable<string> CreateHeaders()
        {
            var sb = new System.Text.StringBuilder(835);
            sb.AppendLine(@"[""Rid"",""Parent Rid"",""Provider Information.UPIN_9068"",""Provider Information.Provider Name_9070"",""Provider Information.Provider Type_9072"",""Provider Information.Provider Subtype_9074"",""Provider Information.Date Opened_9077"",""Provider Information.URN_9079"",""Provider Information.Establishment Number_9081"",""Provider Information.Local Authority_9426"",""Data.1718 High Needs Students 16-19_87766"",""Data.1718 High Needs Students 19-24_87768"",""Data.1516  R04 High Needs Students 16-19_89534"",""Data.1516 R04 High Needs Students 19-24_89535"",""Data.1516 R14 High Needs Students 16-19_89536"",""Data.1516 R14 High Needs Students 19-24_89537"",""Data.Maint Sp SSF High Needs_89538"",""Data.Maint SSF High Needs_89539"",""Data.1617 R46 High Needs Students 1619_90628"",""Data.1617 R46 High Needs Students 1924_90630"",""Data.1617 R06 High Needs Students_90714""]");

            return JsonConvert.DeserializeObject<List<string>>(sb.ToString());
        }
    }
}
