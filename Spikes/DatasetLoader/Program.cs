using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Academies.AY1718;
using AutoMapper;

namespace DatasetLoader
{
    public class SourceColumnAttribute : Attribute
    {
        public string ColumnName { get; }

        public SourceColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
    public class AptSourceRecord
    {
        [SourceColumn("Provider Information.URN_9079")]
        public string URN { get; set; }
        [SourceColumn("Provider Information.UPIN_9068")]
        public string UPIN { get; set; }
        [SourceColumn("Provider Information.Provider Name_9070")]
        public string ProviderName { get; set; }
        [SourceColumn("Provider Information.Date Opened_9077")]
        public DateTimeOffset DateOpened { get; set; }
        [SourceColumn("Provider Information.Local Authority_9426")]
        public string LocalAuthority { get; set; }

    }

    class Program
    {
        static void Main(string[] args)
        {
            var reader = new ExcelReader.ExcelReader();

            var records =
                reader.Read<AptSourceRecord>(
                    @"C:\Users\matt\OneDrive\Documents\Visual Studio 2017\Projects\Allocations.Specs\Allocations.Specs\Datasets\Export APT.XLSX").ToArray();

            //var targets = records.Select(x => new ProviderApt
            //{
            //    ProviderInformation = new ProviderInformation
            //    {
            //        URN = x.URN,
            //        UPIN = x.UPIN,
            //        ProviderName = x.ProviderName,
            //        LocalAuthority = x.LocalAuthority,
            //        DateOpened = x.DateOpened
            //    }
            //}).ToArray();
        }
    }
}
